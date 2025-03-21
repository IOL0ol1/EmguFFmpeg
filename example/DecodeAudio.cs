using System;
using System.IO;
using FFmpeg.AutoGen.Abstractions;

namespace FFmpegSharp.Example
{
    internal class DecodeAudio : ExampleBase
    {
        public DecodeAudio() : this($"EncodeAudio-output.mp2", $"{nameof(DecodeAudio)}-output.raw")
        { }

        public DecodeAudio(params string[] args) : base(args)
        {
            Index = 13;
        }

        public override void Execute()
        {
            var input = args[0];
            var output = args[1];

            var codec = MediaCodec.FindDecoder(AVCodecID.AV_CODEC_ID_MP2);
            using (var decoder = MediaDecoder.Create(codec))
            using (var parser = new MediaCodecParserContext(codec.Ref.id))
            using (var pkt = new MediaPacket())
            using (var decoded_frame = new MediaFrame())
            using (var inStream = File.OpenRead(input))
            using (var outStream = File.OpenWrite(output))
            {
                pkt.Ref.dts = ffmpeg.AV_NOPTS_VALUE;
                pkt.Ref.pts = ffmpeg.AV_NOPTS_VALUE;
                pkt.Ref.pos = 0;
                foreach (var packet in parser.ParserPackets(decoder, inStream, pkt))
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

        private unsafe static void WriteToOutput(MediaFrame frame, int NbChannels, Stream stream)
        {
            for (int i = 0; i < frame.Ref.nb_samples; i++)
            {
                for (int ch = 0; ch < NbChannels; ch++)
                {
                    var buffer = new Span<byte>(frame.Ref.data[(uint)ch], frame.Ref.linesize[0]);
                    stream.Write(buffer);
                }
            }
        }
    }
}
