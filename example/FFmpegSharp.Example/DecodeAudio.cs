using System;
using System.IO;
using FFmpeg.AutoGen;

namespace FFmpegSharp.Example
{
    internal class DecodeAudio : ExampleBase
    {
        public DecodeAudio() : this("path-to-your.mp2", "path-to-your.raw")
        { }

        public DecodeAudio(params string[] args) : base(args)
        { }

        public override void Execute()
        {
            var input = args[0];
            var output = args[1];

            var codec = MediaCodec.FindDecoder(AVCodecID.AV_CODEC_ID_MP2);
            using (var decoder = new MediaDecoder(new MediaCodecContext(codec).Open()))
            using (var parser = new MediaCodecParserContext(codec.Id))
            using (var pkt = new MediaPacket())
            using (var decoded_frame = new MediaFrame())
            using (var inStream = File.OpenRead(input))
            using (var outStream = File.OpenWrite(output))
            {

                pkt.Dts = ffmpeg.AV_NOPTS_VALUE;
                pkt.Pts = ffmpeg.AV_NOPTS_VALUE;
                pkt.Pos = 0;
                foreach (var packet in parser.ParserPackets(decoder.Context, inStream, pkt))
                {
                    foreach (var frame in decoder.DecodePacket(packet, decoded_frame))
                    {
                        WriteToOutput(frame, decoder.Context.ChLayout.nb_channels, outStream);
                    }
                }
                // flush the decoder
                foreach (var frame in decoder.DecodePacket(null, decoded_frame))
                {
                    WriteToOutput(frame, decoder.Context.ChLayout.nb_channels, outStream);
                }
            }
        }

        private unsafe static void WriteToOutput(MediaFrame frame, int NbChannels, Stream stream)
        {
            for (int i = 0; i < frame.NbSamples; i++)
            {
                for (int ch = 0; ch < NbChannels; ch++)
                {
                    var buffer = new Span<byte>(frame.Data[(uint)ch], frame.Linesize[0]);
                    stream.Write(buffer);
                }
            }
        }
    }
}
