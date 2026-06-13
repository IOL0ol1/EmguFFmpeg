using System;
using System.IO;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: decode_video.c
    /// Decode an MPEG-1 video elementary stream and save each frame as a PGM (grayscale) file.
    /// </summary>
    public unsafe class DecodeVideo : ExampleBase
    {
        public DecodeVideo() { Index = 4; Enable = false; }

        public override void Execute()
        {
            var inFile      = args.Length > 0 ? args[0] : "test.mpg";
            var outFileStem = args.Length > 1 ? args[1] : "frame";

            var codec = MediaCodec.FindDecoder(AVCodecID.AV_CODEC_ID_MPEG1VIDEO);
            if (codec == null) throw new Exception("MPEG-1 decoder not found");

            using var parser  = new MediaCodecParserContext(AVCodecID.AV_CODEC_ID_MPEG1VIDEO);
            using var decoder = new MediaDecoder(codec).Open();
            using var frame   = new MediaFrame();

            using var inStream = File.OpenRead(inFile);

            foreach (var packet in parser.ParsePackets(decoder, inStream))
            {
                using (packet)
                    SaveFrames(decoder, frame, packet, outFileStem);
            }

            // Flush.
            SaveFrames(decoder, frame, null, outFileStem);
        }

        private static void SaveFrames(MediaDecoder decoder, MediaFrame frame, MediaPacket packet, string stem)
        {
            foreach (var decoded in decoder.DecodePacket(packet, frame))
            {
                Console.WriteLine($"saving frame {decoder.Ref.frame_num}");
                var filename = $"{stem}-{decoder.Ref.frame_num}";
                PgmSave(decoded.Ref.data[0], decoded.Ref.linesize[0],
                        decoded.Ref.width, decoded.Ref.height, filename);
            }
        }

        private static void PgmSave(byte* buf, int wrap, int xsize, int ysize, string filename)
        {
            using var f = File.OpenWrite(filename);
            using var w = new StreamWriter(f);
            w.WriteLine($"P5");
            w.WriteLine($"{xsize} {ysize}");
            w.WriteLine("255");
            w.Flush();
            for (int i = 0; i < ysize; i++)
                f.Write(new ReadOnlySpan<byte>(buf + i * wrap, xsize));
        }
    }
}
