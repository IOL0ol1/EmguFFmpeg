using System;
using System.IO;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: resample_audio.c
    /// Generate a stereo 48 kHz DBL sine wave signal and resample it to
    /// 5.1 surround S16LE at 44100 Hz using libswresample, writing the
    /// raw output to a file.
    /// </summary>
    public unsafe class ResampleAudio : ExampleBase
    {
        public ResampleAudio() { Index = 13; Enable = false; }

        public override void Execute()
        {
            var outFile = args.Length > 0 ? args[0] : "out_resampled.s16le";

            const int    srcRate      = 48000;
            const int    dstRate      = 44100;
            const int    srcNbSamples = 1024;
            var          srcChLayout  = 2.ToDefaultChLayout();
            var          dstChLayout  = 3.ToDefaultChLayout();  // 3.0 (FL+FR+FC)
            const AVSampleFormat srcFmt = AVSampleFormat.AV_SAMPLE_FMT_DBL;
            const AVSampleFormat dstFmt = AVSampleFormat.AV_SAMPLE_FMT_S16;

            int srcNbChannels = srcChLayout.nb_channels;
            int dstNbChannels = dstChLayout.nb_channels;

            // Create Swresample wrapper — constructor calls swr_alloc_set_opts2 + swr_init.
            using var resampler = new Swresample(dstChLayout, dstFmt, dstRate, srcChLayout, srcFmt, srcRate);

            // Pre-allocate source frame (reused each iteration; data overwritten in-place).
            using var srcFrame = MediaFrame.CreateAudioFrame(srcNbChannels, srcNbSamples, srcFmt, srcRate);
            // Destination frame: swr_convert_frame (inside Swresample.Convert) manages buffer allocation.
            using var dstFrame = new MediaFrame();

            using var outStream = File.OpenWrite(outFile);
            double t = 0.0;

            do
            {
                // Fill synthetic 440 Hz sine wave.
                FillSamples((double*)srcFrame.Ref.extended_data[0], srcNbSamples, srcNbChannels, srcRate, ref t);

                // Convert — swr_convert_frame handles delay-aware output sizing and buffer allocation.
                resampler.Convert(srcFrame, dstFrame);

                if (dstFrame.Ref.nb_samples > 0)
                {
                    int byteCount = dstFrame.Ref.nb_samples * dstNbChannels * sizeof(short);
                    Console.WriteLine($"t:{t:F4} in:{srcNbSamples} out:{dstFrame.Ref.nb_samples}");
                    outStream.Write(new ReadOnlySpan<byte>(dstFrame.Ref.data[0], byteCount));
                }

                dstFrame.Unref();  // release buffers so the next Convert call can (re)allocate them
            } while (t < 10.0);

            // Print playback command.
            var outLayout = resampler.OutputChannelLayout;
            Console.Error.WriteLine("Resampling succeeded. Play with:");
            Console.Error.WriteLine($"ffplay -f s16le -channel_layout {outLayout.Describe()} -channels {dstNbChannels} -ar {dstRate} {outFile}");
        }

        private static void FillSamples(double* dst, int nbSamples, int nbChannels, int sampleRate, ref double t)
        {
            double tincr = 1.0 / sampleRate;
            const double c = 2 * Math.PI * 440.0;
            double* dstp = dst;
            for (int i = 0; i < nbSamples; i++)
            {
                *dstp = Math.Sin(c * t);
                for (int j = 1; j < nbChannels; j++)
                    dstp[j] = dstp[0];
                dstp += nbChannels;
                t += tincr;
            }
        }
    }
}
