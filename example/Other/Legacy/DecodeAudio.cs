using System;
using System.IO;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example.Legacy
{
    internal class DecodeAudio : ExampleBase
    {
        public DecodeAudio() : this($"EncodeAudio-output.mp2", $"{nameof(DecodeAudio)}-output.raw")
        { Index = 41; Enable = false; }

        public DecodeAudio(params string[] args) : base(args)
        { }

        public override void Execute()
        {
            var input = args[0];
            var output = args[1];

            var codec = MediaCodec.FindDecoder(AVCodecID.AV_CODEC_ID_MP2);
            using (var decoder = new MediaDecoder(codec).Open())
            using (var parser = new MediaCodecParserContext(codec.Ref.id))
            using (var decoded_frame = new MediaFrame())
            using (var inStream = File.OpenRead(input))
            using (var outStream = File.OpenWrite(output))
            {
                // ParsePackets owns its own MediaPacket internally — the yielded packet's data is
                // valid only until the next MoveNext, which is fine since we consume it inline.
                foreach (var packet in parser.ParsePackets(decoder, inStream))
                {
                    foreach (var frame in decoder.DecodePacket(packet, decoded_frame))
                    {
                        WriteToOutput(frame, decoder.Ref.ch_layout.nb_channels, outStream);
                    }
                }
                // flush the decoder
                foreach (var frame in decoder.DecodePacket(null, decoded_frame))
                {
                    WriteToOutput(frame, decoder.Ref.ch_layout.nb_channels, outStream);
                }
            }
        }

        private unsafe static void WriteToOutput(MediaFrame frame, int nbChannels, Stream stream)
        {
            // Interleave the channels sample by sample, writing bytes-per-sample at a time (mirrors decode_audio.c).
            int dataSize = ffmpeg.av_get_bytes_per_sample((AVSampleFormat)frame.Ref.format);
            for (int i = 0; i < frame.Ref.nb_samples; i++)
            {
                for (int ch = 0; ch < nbChannels; ch++)
                {
                    stream.Write(new ReadOnlySpan<byte>(frame.Ref.data[(uint)ch] + i * dataSize, dataSize));
                }
            }
        }
    }
}
