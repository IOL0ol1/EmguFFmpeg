using System;
using System.IO;
using FFmpeg.AutoGen;
using FFmpeg.Sharp;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: encode_audio.c
    /// Generate a synthetic 440 Hz sine wave and encode it to an MP2 file.
    /// </summary>
    public unsafe class EncodeAudio : ExampleBase
    {
        public EncodeAudio() { Index = 5; Enable = false; }

        public override void Execute()
        {
            var outFile = args.Length > 0 ? args[0] : "out.mp2";

            var codec = MediaCodec.FindEncoder(AVCodecID.AV_CODEC_ID_MP2);
            if (codec == null) throw new Exception("MP2 encoder not found");

            // Pick the best sample rate (closest to 44100).
            int sampleRate = 44100;
            var supportedRates = codec.GetSupportedSamplerates();
            foreach (var r in supportedRates)
            {
                if (Math.Abs(44100 - r) < Math.Abs(44100 - sampleRate))
                    sampleRate = r;
            }

            // Pick the channel layout with the most channels.
            AVChannelLayout chLayout = 2.ToDefaultChLayout();
            var layouts = codec.GetChLayouts();
            int bestNb = 0;
            foreach (var cl in layouts)
            {
                var c = cl;
                if (c.nb_channels > bestNb) { bestNb = c.nb_channels; chLayout = c; }
            }

            using var encoder = MediaEncoder.CreateAudioEncoder(
                codec, sampleRate, chLayout,
                AVSampleFormat.AV_SAMPLE_FMT_S16, 64000);

            int frameSize = encoder.Ref.frame_size;
            using var frame = MediaFrame.CreateAudioFrame(chLayout, frameSize,
                AVSampleFormat.AV_SAMPLE_FMT_S16, sampleRate);

            using var packet = new MediaPacket();
            using var outStream = File.OpenWrite(outFile);

            float t = 0f;
            float tincr = 2f * MathF.PI * 440f / sampleRate;

            for (int i = 0; i < 200; i++)
            {
                frame.MakeWritable();
                var samples = (short*)frame.Ref.data[0];

                for (int j = 0; j < frameSize; j++)
                {
                    short s = (short)(MathF.Sin(t) * 10000f);
                    for (int ch = 0; ch < encoder.Ref.ch_layout.nb_channels; ch++)
                        samples[encoder.Ref.ch_layout.nb_channels * j + ch] = s;
                    t += tincr;
                }

                frame.Ref.pts = (long)i * frameSize;
                WriteEncodedPackets(encoder, frame, packet, outStream);
            }

            // Flush.
            WriteEncodedPackets(encoder, null, packet, outStream);
        }

        private static void WriteEncodedPackets(MediaEncoder encoder, MediaFrame frame, MediaPacket packet, Stream outStream)
        {
            foreach (var pkt in encoder.EncodeFrame(frame, packet))
            {
                outStream.Write(new ReadOnlySpan<byte>(pkt.Ref.data, pkt.Ref.size));
            }
        }
    }
}
