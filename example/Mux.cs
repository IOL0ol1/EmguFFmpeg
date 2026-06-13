using System;
using FFmpeg.AutoGen;
using FFmpeg.Sharp;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: mux.c
    /// Generate a synthetic audio + video stream and mux them into an output
    /// container.  Default output: output_mux.mp4 (MPEG-4).
    /// </summary>
    public unsafe class Mux : ExampleBase
    {
        private const double StreamDuration = 10.0;
        private const int FrameRate        = 25;
        private const int VideoWidth       = 352;
        private const int VideoHeight      = 288;

        public Mux() { Index = 19; Enable = false; }

        public override void Execute()
        {
            var outFile = args.Length > 0 ? args[0] : "output_mux.mp4";

            // ── Video encoder ─────────────────────────────────────────────────
            var videoCodecId = AVCodecID.AV_CODEC_ID_MPEG4;
            var videoCodec   = MediaCodec.FindEncoder(videoCodecId);
            using var videoEncoder = MediaEncoder.Video()
                .Codec(videoCodec)
                .Size(VideoWidth, VideoHeight)
                .Fps(FrameRate)
                .PixelFormat(AVPixelFormat.AV_PIX_FMT_YUV420P)
                .Bitrate(400000)
                .Flags(ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER)
                .Configure(ctx =>
                {
                    ctx.Ref.gop_size = 12;
                    if (videoCodecId == AVCodecID.AV_CODEC_ID_MPEG1VIDEO)
                        ctx.Ref.mb_decision = 2;
                })
                .Build();

            // ── Audio encoder ─────────────────────────────────────────────────
            var audioCodecId = AVCodecID.AV_CODEC_ID_MP2;
            var audioCodec   = MediaCodec.FindEncoder(audioCodecId);

            int audioSampleRate = 44100;
            foreach (var rate in audioCodec.GetSupportedSamplerates())
            {
                if (rate == 44100) { audioSampleRate = 44100; break; }
                audioSampleRate = rate; // take first supported if 44100 unavailable
            }

            var audioSampleFmt = audioCodec.GetSampleFormats()[0];
            var audioChLayout  = 2.ToDefaultChLayout();
            using var audioEncoder = MediaEncoder.Audio()
                .Codec(audioCodec)
                .SampleRate(audioSampleRate)
                .ChannelLayout(audioChLayout)
                .SampleFormat(audioSampleFmt)
                .Bitrate(64000)
                .Flags(ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER)
                .Build();

            int audioFrameSize = audioEncoder.Ref.frame_size > 0 ? audioEncoder.Ref.frame_size : 10000;

            // ── Muxer ─────────────────────────────────────────────────────────
            using var muxer = MediaMuxer.Create(outFile);
            muxer.AddStream(videoEncoder);
            muxer.AddStream(audioEncoder);
            muxer.DumpFormat();
            muxer.WriteHeader();

            using var videoFrame = MediaFrame.CreateVideoFrame(VideoWidth, VideoHeight, AVPixelFormat.AV_PIX_FMT_YUV420P);
            using var audioFrame = MediaFrame.CreateAudioFrame(audioChLayout, audioFrameSize, audioSampleFmt, audioSampleRate);
            using var tmpPacket  = new MediaPacket();

            long videoNextPts = 0, audioNextPts = 0;
            bool encodeMoreVideo = true, encodeMoreAudio = true;

            while (encodeMoreVideo || encodeMoreAudio)
            {
                bool videoTime = encodeMoreVideo &&
                    (!encodeMoreAudio || FFmpegUtil.CompareTs(videoNextPts, videoEncoder.Ref.time_base,
                        audioNextPts, audioEncoder.Ref.time_base) <= 0);

                if (videoTime)
                {
                    if (FFmpegUtil.CompareTs(videoNextPts, videoEncoder.Ref.time_base,
                            (long)(StreamDuration * ffmpeg.AV_TIME_BASE), new AVRational { num = 1, den = ffmpeg.AV_TIME_BASE }) > 0)
                    {
                        // Flush video.
                        foreach (var p in videoEncoder.EncodeFrame(null, tmpPacket))
                        {
                            p.Ref.stream_index = 0;
                            muxer.WritePacket(p);
                        }
                        encodeMoreVideo = false;
                    }
                    else
                    {
                        FillYuv(videoFrame, (int)videoNextPts);
                        videoFrame.Ref.pts = videoNextPts++;
                        foreach (var p in videoEncoder.EncodeFrame(videoFrame, tmpPacket))
                        {
                            p.Ref.stream_index = 0;
                            muxer.WritePacket(p, videoEncoder.Ref.time_base);
                        }
                    }
                }
                else
                {
                    if (FFmpegUtil.CompareTs(audioNextPts, audioEncoder.Ref.time_base,
                            (long)(StreamDuration * ffmpeg.AV_TIME_BASE), new AVRational { num = 1, den = ffmpeg.AV_TIME_BASE }) > 0)
                    {
                        foreach (var p in audioEncoder.EncodeFrame(null, tmpPacket))
                        {
                            p.Ref.stream_index = 1;
                            muxer.WritePacket(p);
                        }
                        encodeMoreAudio = false;
                    }
                    else
                    {
                        FillAudio(audioFrame, audioNextPts, audioSampleRate);
                        audioFrame.Ref.pts = audioNextPts;
                        audioNextPts += audioFrameSize;
                        foreach (var p in audioEncoder.EncodeFrame(audioFrame, tmpPacket))
                        {
                            p.Ref.stream_index = 1;
                            muxer.WritePacket(p, audioEncoder.Ref.time_base);
                        }
                    }
                }
            }

            muxer.WriteTrailer();
            Console.WriteLine($"Muxing complete: '{outFile}'");
        }

        private static void FillYuv(MediaFrame frame, int frameIndex)
        {
            int w = frame.Ref.width, h = frame.Ref.height;
            byte* y  = frame.Ref.data[0];
            byte* cb = frame.Ref.data[1];
            byte* cr = frame.Ref.data[2];
            int   ls0 = frame.Ref.linesize[0], ls1 = frame.Ref.linesize[1], ls2 = frame.Ref.linesize[2];

            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                    y[j * ls0 + i] = (byte)(i + j + frameIndex * 3);
            for (int j = 0; j < h / 2; j++)
                for (int i = 0; i < w / 2; i++)
                {
                    cb[j * ls1 + i] = (byte)(128 + j + frameIndex * 2);
                    cr[j * ls2 + i] = (byte)(64  + i + frameIndex * 5);
                }
        }

        private static double _audioT = 0, _audioTIncr, _audioTIncr2;
        private static bool _audioInit = false;

        private static void FillAudio(MediaFrame frame, long pts, int sampleRate)
        {
            if (!_audioInit)
            {
                _audioTIncr  = 2 * Math.PI * 110.0 / sampleRate;
                _audioTIncr2 = 2 * Math.PI * 110.0 / sampleRate / sampleRate;
                _audioInit   = true;
            }

            int nb_channels = frame.Ref.ch_layout.nb_channels;
            short* q = (short*)frame.Ref.data[0];
            for (int j = 0; j < frame.Ref.nb_samples; j++)
            {
                short v = (short)(Math.Sin(_audioT) * 10000);
                for (int i = 0; i < nb_channels; i++) *q++ = v;
                _audioT     += _audioTIncr;
                _audioTIncr += _audioTIncr2;
            }
        }
    }
}
