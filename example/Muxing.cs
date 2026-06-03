using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    internal class Muxing : ExampleBase
    {
        public Muxing() : this($"Muxing-output.mp4")
        {
        }

        public Muxing(params string[] args) : base(args)
        { }

        private const long STREAM_DURATION = 10;

        public override unsafe void Execute()
        {
            var filename = args[0];

            bool encode_video = false, encode_audio = false;
            using (var oc = MediaMuxer.Create(filename))
            using (var vframe = new MediaFrame())
            using (var vtmpframe = new MediaFrame())
            using (var aframe = new MediaFrame())
            using (var atmpframe = new MediaFrame())
            {
                var fmt = oc.Format;
                Swscale sws = null;
                Swresample swr = null;
                try
                {
                    var ap = new Parames();
                    var vp = new Parames();

                    var encoders = new List<MediaEncoder>();
                    /* Add the audio and video streams using the default format codecs
                     * and initialize the codecs. */
                    if (fmt.Ref.audio_codec != AVCodecID.AV_CODEC_ID_NONE)
                    {
                        var encoder = AddStream(oc, MediaCodec.FindEncoder(fmt.Ref.audio_codec), AVMediaType.AVMEDIA_TYPE_AUDIO, ap);
                        encoders.Add(encoder);
                        encode_audio = true;

                        var nbsamples = (encoder.GetCodec().Ref.capabilities & ffmpeg.AV_CODEC_CAP_VARIABLE_FRAME_SIZE) != 0 ? 10000 : encoder.Ref.frame_size;

                        // src (S16 input)
                        atmpframe.Ref.ch_layout = encoder.Ref.ch_layout;
                        atmpframe.Ref.nb_samples = nbsamples;
                        atmpframe.Ref.format = (int)AVSampleFormat.AV_SAMPLE_FMT_S16;
                        atmpframe.Ref.sample_rate = encoder.Ref.sample_rate;
                        atmpframe.AllocateBuffer();

                        // dst (encoder format)
                        aframe.Ref.ch_layout = encoder.Ref.ch_layout;
                        aframe.Ref.nb_samples = nbsamples;
                        aframe.Ref.format = (int)encoder.Ref.sample_fmt;
                        aframe.Ref.sample_rate = encoder.Ref.sample_rate;
                        aframe.AllocateBuffer();

                        swr = new Swresample(encoder.Ref.ch_layout, encoder.Ref.sample_fmt, encoder.Ref.sample_rate,
                                             atmpframe.Ref.ch_layout, AVSampleFormat.AV_SAMPLE_FMT_S16, encoder.Ref.sample_rate);
                    }
                    if (fmt.Ref.video_codec != AVCodecID.AV_CODEC_ID_NONE)
                    {
                        var encoder = AddStream(oc, MediaCodec.FindEncoder(fmt.Ref.video_codec), AVMediaType.AVMEDIA_TYPE_VIDEO, vp);
                        encoders.Add(encoder);
                        encode_video = true;

                        // src (YUV420P input)
                        vtmpframe.Ref.width = encoder.Ref.width;
                        vtmpframe.Ref.height = encoder.Ref.height;
                        vtmpframe.Ref.format = (int)AVPixelFormat.AV_PIX_FMT_YUV420P;
                        vtmpframe.AllocateBuffer();

                        // dst (encoder pixel format)
                        vframe.Ref.width = encoder.Ref.width;
                        vframe.Ref.height = encoder.Ref.height;
                        vframe.Ref.format = (int)encoder.Ref.pix_fmt;
                        vframe.AllocateBuffer();

                        sws = new Swscale(encoder.Ref.width, encoder.Ref.height, AVPixelFormat.AV_PIX_FMT_YUV420P,
                                          encoder.Ref.width, encoder.Ref.height, encoder.Ref.pix_fmt);
                    }
                    oc.DumpFormat();
                    oc.WriteHeader();

                    while (encode_video || encode_audio)
                    {
                        /* select the stream to encode */
                        if (encode_video &&
                            (!encode_audio || ffmpeg.av_compare_ts(vp.nextPts, encoders[1].Ref.time_base,
                                                            ap.nextPts, encoders[0].Ref.time_base) <= 0))
                        {
                            encode_video = WriteVideoFrame(oc, sws, encoders[1], vtmpframe, vframe, vp);
                        }
                        else
                        {
                            encode_audio = WriteAudioFrame(oc, swr, encoders[0], atmpframe, aframe, ap);
                        }
                    }
                    oc.FlushCodecs(encoders);
                    oc.WriteTrailer();
                    encoders.ForEach(_ => _?.Dispose());
                }
                finally
                {
                    sws?.Dispose();
                    swr?.Dispose();
                }
            }

        }

        private static MediaEncoder AddStream(MediaMuxer oc, MediaCodec codec, AVMediaType mediaType, Parames p)
        {
            var fmt = oc.Format;
            switch (mediaType)
            {
                case AVMediaType.AVMEDIA_TYPE_AUDIO:
                    var samplefmt = codec.GetSampleFormats().Any() ? codec.GetSampleFormats().First() : AVSampleFormat.AV_SAMPLE_FMT_FLTP;
                    var bitrate = 64000;
                    var samplerate = codec.GetSupportedSamplerates().Any() ? codec.GetSupportedSamplerates().First() : 44100;
                    var chlayout = codec.GetChLayouts().Any() ? codec.GetChLayouts().First() : 2.ToDefaultChLayout();
                    var aencoder = MediaEncoder.CreateAudioEncoder(fmt, samplerate, chlayout, samplefmt, bitrate, _ => _.Ref.thread_count = 10);
                    /* copy the stream parameters to the muxer */
                    oc.AddStream(aencoder).Ref.id = (int)oc.Ref.nb_streams - 1;
                    p.tincr = 2 * Math.PI * 110.0 / aencoder.Ref.sample_rate;
                    /* increment frequency by 110 Hz per second */
                    p.tincr2 = 2 * Math.PI * 110.0 / aencoder.Ref.sample_rate / aencoder.Ref.sample_rate;
                    return aencoder;
                case AVMediaType.AVMEDIA_TYPE_VIDEO:
                    var vbitrate = 400000;
                    var width = 352;
                    var height = 288;
                    var fps = 25d;
                    var pixfmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
                    var vencoder = MediaEncoder.CreateVideoEncoder(fmt, width, height, fps, pixfmt, vbitrate, _ =>
                    {
                        _.Ref.thread_count = 10;
                        _.Ref.gop_size = 12;
                        if (_.Ref.codec_id == AVCodecID.AV_CODEC_ID_MPEG2VIDEO)
                            _.Ref.max_b_frames = 2;
                        if (_.Ref.codec_id == AVCodecID.AV_CODEC_ID_MPEG1VIDEO)
                            _.Ref.mb_decision = 2;
                    });
                    oc.AddStream(vencoder).Ref.id = (int)oc.Ref.nb_streams - 1;
                    return vencoder;
                default:
                    break;
            }
            return null;
        }

        private static MediaFrame GetVideoFrame(MediaEncoder encoder, MediaFrame src, MediaFrame dst, Swscale sws, Parames vp)
        {
            if (ffmpeg.av_compare_ts(vp.nextPts, encoder.Ref.time_base, STREAM_DURATION, 1d.ToRational()) > 0)
                return null;
            FillYuvImage(src, (int)vp.nextPts, encoder.Ref.width, encoder.Ref.height);
            MediaFrame o;
            if ((int)encoder.Ref.pix_fmt == src.Ref.format)
            {
                o = src;
            }
            else
            {
                sws.Convert(src, dst);
                o = dst;
            }
            o.Ref.pts = vp.nextPts;
            vp.nextPts += 1;
            return o;
        }

        private static unsafe MediaFrame GetAudioFrame(MediaEncoder encoder, MediaFrame frame, Parames ap)
        {
            if (ffmpeg.av_compare_ts(ap.nextPts, encoder.Ref.time_base, STREAM_DURATION, 1.ToRational()) > 0)
                return null;

            int v;
            Int16* q = (Int16*)frame.Ref.data[0];
            for (var j = 0; j < frame.Ref.nb_samples; j++)
            {
                v = (int)(Math.Sin(ap.t) * 10000);
                for (var i = 0; i < frame.Ref.ch_layout.nb_channels; i++)
                    *q++ = (Int16)v;
                ap.t += ap.tincr;
                ap.tincr += ap.tincr2;
            }

            frame.Ref.pts = ap.nextPts;
            ap.nextPts += frame.Ref.nb_samples;

            return frame;
        }

        private static bool WriteAudioFrame(MediaMuxer oc, Swresample swr, MediaEncoder encoder, MediaFrame src, MediaFrame dst, Parames ap)
        {
            var f = GetAudioFrame(encoder, src, ap);
            var ret = false;
            MediaFrame[] a;
            if (f != null && (int)encoder.Ref.sample_fmt == f.Ref.format)
            {
                a = new[] { f };
            }
            else
            {
                swr.Convert(f, dst);
                a = new[] { dst };
            }
            foreach (var item in a)
            {
                ret = WriteFrame(oc, encoder, item, 0);
            }
            return ret;
        }

        private static bool WriteVideoFrame(MediaMuxer oc, Swscale sws, MediaEncoder encoder, MediaFrame src, MediaFrame dst, Parames vp)
        {
            return WriteFrame(oc, encoder, GetVideoFrame(encoder, src, dst, sws, vp), 1);
        }

        private static bool WriteFrame(MediaMuxer oc, MediaEncoder encoder, MediaFrame frame, int streamIndex)
        {
            var ret = 0;
            foreach (var pkt in encoder.EncodeFrame(frame))
            {
                pkt.Ref.stream_index = streamIndex;
                Console.WriteLine($"pts:{pkt.Ref.pts} pts_time:{0} dst:{pkt.Ref.dts} dts_time:{0} duration:{pkt.Ref.duration} duration_time:{0} stream_index:{streamIndex}");
                ret = oc.WritePacket(pkt, encoder.Ref.time_base);
            }
            return frame == null ? false : true;
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
                        pict.Ref.data[0][y * pict.Ref.linesize[0] + x] = (byte)(x + y + i * 3);

                /* Cb and Cr */
                for (y = 0; y < height / 2; y++)
                {
                    for (x = 0; x < width / 2; x++)
                    {
                        pict.Ref.data[1][y * pict.Ref.linesize[1] + x] = (byte)(128 + y + i * 2);
                        pict.Ref.data[2][y * pict.Ref.linesize[2] + x] = (byte)(64 + x + i * 5);
                    }
                }
            }
        }

        private class Parames
        {
            public double t { get; set; }
            public double tincr { get; set; }
            public double tincr2 { get; set; }
            public long nextPts { get; set; }
        }

    }
}
