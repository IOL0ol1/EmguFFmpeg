using System;
using System.Globalization;
using System.Security.Cryptography;
using FFmpeg.AutoGen;
using FFmpeg.Sharp;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: filter_audio.c
    /// Generate a synthetic multichannel 48 kHz FLTP audio signal, run it through an
    /// abuffer → volume(0.90) → aformat(s16,44100,stereo) → abuffersink chain, and
    /// print the MD5 checksum of each output plane.
    /// </summary>
    public unsafe class FilterAudio : ExampleBase
    {
        private const int InputSamplerate = 48000;
        private const AVSampleFormat InputFormat = AVSampleFormat.AV_SAMPLE_FMT_FLTP;
        private const int FrameSize = 1024;
        private const double VolumeVal = 0.90;

        public FilterAudio() { Index = 10; Enable = false; }

        public override void Execute()
        {
            float duration = args.Length > 0 ? float.Parse(args[0]) : 5f;
            int nbFrames = (int)(duration * InputSamplerate / FrameSize);
            if (nbFrames <= 0) throw new ArgumentException("Invalid duration");

            // 5.0 surround: FL+FR+FC+SL+SR (AV_CH_LAYOUT_5POINT0 = SURROUND|SIDE_LEFT|SIDE_RIGHT)
            var inputChLayout = (ffmpeg.AV_CH_LAYOUT_SURROUND | ffmpeg.AV_CH_SIDE_LEFT | ffmpeg.AV_CH_SIDE_RIGHT).ToChLayout();

            // ── Build filter graph using MediaFilterGraph wrapper ─────────────
            using var graph = new MediaFilterGraph();

            var abufferCtx = graph.AddAudioSrcFilter(
                new MediaFilter("abuffer"),
                inputChLayout,
                InputSamplerate,
                InputFormat,
                "src");

            var volumeCtx = graph.AddFilter(
                new MediaFilter("volume"),
                $"volume={VolumeVal.ToString("F2", CultureInfo.InvariantCulture)}",
                "volume");

            var aformatCtx = graph.AddFilter(
                new MediaFilter("aformat"),
                $"sample_fmts={AVSampleFormat.AV_SAMPLE_FMT_S16.GetName()}:sample_rates=44100:channel_layouts=stereo",
                "aformat");

            var abuffersinkCtx = graph.AddFilter(new MediaFilter("abuffersink"), (string)null, "sink");

            // Link the chain: abuffer → volume → aformat → abuffersink.
            abufferCtx.LinkTo(0, volumeCtx).LinkTo(0, aformatCtx).LinkTo(0, abuffersinkCtx);
            graph.Initialize();

            // ── Main loop ─────────────────────────────────────────────────────
            using var filtFrame = new MediaFrame();

            for (int i = 0; i < nbFrames; i++)
            {
                // Allocate and fill synthetic sine wave frame.
                using var frame = MediaFrame.CreateAudioFrame(inputChLayout, FrameSize, InputFormat, InputSamplerate);
                frame.Ref.pts = (long)i * FrameSize;

                for (int ch = 0; ch < 5; ch++)
                {
                    float* data = (float*)frame.Ref.extended_data[ch];
                    for (int j = 0; j < FrameSize; j++)
                        data[j] = MathF.Sin(2f * MathF.PI * (i + j) * (ch + 1) / FrameSize);
                }

                abufferCtx.WriteFrame(frame, MediaFilterContext.BufferSrcFlagKeepRef);

                foreach (var filt in abuffersinkCtx.ReadFrame(filtFrame))
                    ProcessOutput(filt);
            }
        }

        private static void ProcessOutput(MediaFrame frame)
        {
            bool planar    = ((AVSampleFormat)frame.Ref.format).IsPlanar();
            int  channels  = frame.Ref.ch_layout.nb_channels;
            int  planes    = planar ? channels : 1;
            int  bps       = ((AVSampleFormat)frame.Ref.format).GetBytesPerSample();
            int  planeSize = bps * frame.Ref.nb_samples * (planar ? 1 : channels);

            for (int i = 0; i < planes; i++)
            {
                var data = new ReadOnlySpan<byte>(frame.Ref.extended_data[i], planeSize);
                var checksum = MD5.HashData(data);
                Console.Write($"plane {i}: 0x");
                foreach (var b in checksum) Console.Write($"{b:X2}");
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
}
