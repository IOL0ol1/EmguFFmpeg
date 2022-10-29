using System;
using System.Collections.Generic;
using System.Linq;
using FFmpeg.AutoGen;

namespace FFmpegSharp.Example
{
    internal class Muxing : ExampleBase
    {
        public Muxing() : this($"Muxing-output.mp4")
        {
            Index = -999999;
        }

        public Muxing(params string[] args) : base(args)
        { }

        private const long STREAM_DURATION = 3;

        public override void Execute()
        {
            var filename = args[0];

            using (var oc = MediaMuxer.Create(filename, OutFormat.GuessFormat(null, filename, null)))
            using (var sws = new PixelConverter())
            using (var swr = new SampleConverter())  /* create resampler context */
            {
                oc.MaxStreams = int.MaxValue; // can ignore
                var fmt = oc.Format;
                var encoders = new List<MediaEncoder>();

                var ap = new AudioParames();
                var vp = new VideoParames();

                /* Add the audio and video streams using the default format codecs
                 * and initialize the codecs. */
                if (fmt.AudioCodec != AVCodecID.AV_CODEC_ID_NONE)
                {
                    var codec = MediaCodec.FindDecoder(fmt.AudioCodec);
                    var bitrate = 64000;
                    var samplefmt = codec.GetSampelFmts().Any() ? codec.GetSampelFmts().First() : AVSampleFormat.AV_SAMPLE_FMT_FLTP;
                    var samplerate = codec.GetSupportedSamplerates().Any() ? codec.GetSupportedSamplerates().First() : 44100;
                    var encoder = MediaEncoder.CreateAudioEncoder(fmt, samplerate, 2.ToDefaultChLayout(), samplefmt, bitrate,_=>_.ThreadCount = 10);
                    encoders.Add(encoder);
                    ap.tincr = 2 * Math.PI * 110.0 / encoder.SampleRate;
                    /* increment frequency by 110 Hz per second */
                    ap.tincr2 = 2 * Math.PI * 110.0 / encoder.SampleRate / encoder.SampleRate;
                    /* copy the stream parameters to the muxer */
                    oc.AddStream(encoder);
                    /* set resampler context options */
                    swr.SetOpts(encoder.ChLayout, encoder.SampleRate, encoder.SampleFmt);
                }
                if (fmt.VideoCodec != AVCodecID.AV_CODEC_ID_NONE)
                {
                    var codec = MediaCodec.FindDecoder(fmt.VideoCodec);
                    var vbitrate = 400000;
                    var width = 352;
                    var height = 288;
                    var fps = 25;
                    var pixfmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
                    var encoder = MediaEncoder.CreateVideoEncoder(fmt, width, height, fps, pixfmt, vbitrate, _ =>
                    {
                        _.ThreadCount = 10;
                        _.GopSize = 12;
                        if (_.CodecId == AVCodecID.AV_CODEC_ID_MPEG2VIDEO)
                            _.MaxBFrames = 2;
                        if (_.CodecId == AVCodecID.AV_CODEC_ID_MPEG1VIDEO)
                            _.MbDecision = 2;
                    });
                    encoders.Add(encoder);
                    sws.SetOpts(encoder.Width, encoder.Height, AVPixelFormat.AV_PIX_FMT_YUV420P);
                    oc.AddStream(encoder);
                }
                oc.DumpFormat();
                oc.WriteHeader();

                using (var frame = new MediaFrame())
                using (var tmpframe = new MediaFrame())
                {
                    var encodeVideo = 1;
                    var encodeAudio = 1;
                    while (encodeVideo != 0 || encodeAudio != 0)
                    {
                        /* select the stream to encode */
                        if (encodeVideo != 0 &&
                            (encodeAudio == 0 || ffmpeg.av_compare_ts(vp.nextPts, encoders[1].TimeBase,
                                                            ap.nextPts, encoders[0].TimeBase) <= 0))
                        {
                            encodeVideo = 1 - WriteVideoFrame(oc, sws, encoders[1], frame, tmpframe, vp);
                        }
                        else
                        {
                            encodeAudio = 1 - WriteAudioFrame(oc, swr, encoders[0], frame, tmpframe, ap);
                        }
                    }
                }
                oc.FlushCodecs(encoders);
                oc.WriteTrailer();
                encoders.ForEach(_ => _?.Dispose());
            }

        }

        private static MediaFrame GetVideoFrame(MediaEncoder encoder, MediaFrame src, MediaFrame dst, PixelConverter sws, VideoParames vp)
        {
            if (ffmpeg.av_compare_ts(vp.nextPts, encoder.TimeBase, STREAM_DURATION, 1d.ToRational()) > 0)
                return null;
            src.Width = encoder.Width;
            src.Height = encoder.Height;
            src.Format = (int)encoder.PixFmt;
            src.AllocateBuffer();
            FillYuvImage(src, (int)vp.nextPts, encoder.Width, encoder.Height);
            var o = sws.Convert(src, dst).First();
            o.Pts = vp.nextPts;
            vp.nextPts += 1;
            return o;
        }

        private static unsafe MediaFrame GetAudioFrame(MediaEncoder encoder, MediaFrame frame, AudioParames ap)
        {
            if (ffmpeg.av_compare_ts(ap.nextPts, encoder.TimeBase, STREAM_DURATION, 1d.ToRational()) > 0)
                return null;
            frame.ChLayout = encoder.ChLayout;
            frame.NbSamples = encoder.FrameSize;
            frame.Format = (int)AVSampleFormat.AV_SAMPLE_FMT_S16;
            frame.SampleRate = encoder.SampleRate;
            frame.AllocateBuffer();
            int v;
            Int16* q = (Int16*)frame.Data[0];
            for (var j = 0; j < frame.NbSamples; j++)
            {
                v = (int)(Math.Sin(ap.t) * 10000);
                for (var i = 0; i < encoder.ChLayout.nb_channels; i++)
                    *q++ = (Int16)v;
                ap.t += ap.tincr;
                ap.tincr += ap.tincr2;
            }

            frame.Pts = ap.nextPts;
            ap.nextPts += frame.NbSamples;

            return frame;
        }

        private static int WriteAudioFrame(MediaMuxer oc, SampleConverter swr, MediaEncoder encoder, MediaFrame src, MediaFrame dst, AudioParames ap)
        {
            var f = GetAudioFrame(encoder, src, ap);
            var ret = 0;
            foreach (var item in swr.Convert(f, dst))
            {
                ret = WriteFrame(oc, encoder, item, 0);
            }
            return ret;
        }

        private static int WriteVideoFrame(MediaMuxer oc, PixelConverter sws, MediaEncoder encoder, MediaFrame src, MediaFrame dst, VideoParames vp)
        {
            return WriteFrame(oc, encoder, GetVideoFrame(encoder, src, dst, sws, vp), 1);
        }

        private static int WriteFrame(MediaMuxer oc, MediaEncoder encoder, MediaFrame frame, int streamIndex)
        {
            var ret = 0;
            foreach (var packet in encoder.EncodeFrame(frame))
            {
                packet.StreamIndex = streamIndex;
                ret = oc.WritePacket(packet, encoder.TimeBase);
            }
            return ret == ffmpeg.AVERROR_EOF ? 1 : 0;
        }


        /// <summary>
        /// Prepare a dummy image. 
        /// </summary>
        /// <param name="pict"></param>
        /// <param name="frame_index"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private unsafe static void FillYuvImage(MediaFrame pict, int frame_index, int width, int height)
        {
            int x, y, i;

            i = frame_index;


            unchecked
            {
                /* Y */
                for (y = 0; y < height; y++)
                    for (x = 0; x < width; x++)
                        pict.Data[0][y * pict.Linesize[0] + x] = (byte)(x + y + i * 3);

                /* Cb and Cr */
                for (y = 0; y < height / 2; y++)
                {
                    for (x = 0; x < width / 2; x++)
                    {
                        pict.Data[1][y * pict.Linesize[1] + x] = (byte)(128 + y + i * 2);
                        pict.Data[2][y * pict.Linesize[2] + x] = (byte)(64 + x + i * 5);
                    }
                }
            }
        }

        private class AudioParames
        {
            public double t { get; set; }
            public double tincr { get; set; }
            public double tincr2 { get; set; }
            public long nextPts { get; set; }
        }

        private class VideoParames
        {
            public long nextPts { get; set; }
        }
    }
}
