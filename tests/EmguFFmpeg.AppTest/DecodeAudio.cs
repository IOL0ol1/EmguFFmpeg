using System;
using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace EmguFFmpeg.AppTest
{
    internal class DecodeAudio : ExampleBase
    {
        public DecodeAudio(string inputFile = "path-to-your.mp2", string outputFile = "path-to-your.raw")
        {
            parames["inputFile"] = inputFile;
            parames["outputFile"] = outputFile;
        }

        public override void Execute()
        {
            var input = GetParame<string>("inputFile");
            var output = GetParame<string>("outputFile");

            var codec = MediaCodec.FindDecoder(AVCodecID.AV_CODEC_ID_MP2);
            var decoder = new MediaDecoder(new MediaCodecContext(codec).Open());
            var parser = new MediaCodecParserContext(codec.Id);
            var pkt = new MediaPacket();
            var decoded_frame = new MediaFrame();
            var inStream = File.OpenRead(input);
            var outStream = File.OpenWrite(output);

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

            decoder.Dispose();
            parser.Dispose();
            pkt.Dispose();
            decoded_frame.Dispose();
            outStream.Dispose();
        }

        private unsafe void WriteToOutput(MediaFrame frame, int NbChannels, Stream stream)
        {
            for (int i = 0; i < frame.NbSamples; i++)
            {
                for (int ch = 0; ch < NbChannels; ch++)
                {
                    var buffer = new Span<byte>((void*)frame.Data[ch], frame.Linesize[0]);
                    stream.Write(buffer);
                }
            }
        }
    }
}
