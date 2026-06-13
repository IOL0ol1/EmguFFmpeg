using System;
using System.IO;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example.Legacy
{
    internal class DecodeVideo : ExampleBase
    {
        public DecodeVideo() : this($"EncodeVideo-output.h264", $"{nameof(DecodeVideo)}-output.raw")
        { Index = 42; Enable = false; }

        public DecodeVideo(params string[] args) : base(args)
        { }

        public override void Execute()
        {
            var filename = args[0];
            var outfilename = args[1];

            var codec = MediaCodec.FindDecoder(AVCodecID.AV_CODEC_ID_H264);
            using (var f = File.OpenRead(filename))
            using (var of = File.Create(outfilename))
            using (var parser = new MediaCodecParserContext(codec.Ref.id))
            using (var c = new MediaDecoder(codec))
            using (var frame = new MediaFrame())
            {
                /* For some codecs, such as msmpeg4 and mpeg4, width and height
                   MUST be initialized there because this information is not
                   available in the bitstream. */
                c.Ref.height = 288;
                c.Ref.width = 352;
                c.Open();

                foreach (var oPacket in parser.ParsePackets(c, f))
                {
                    foreach (var oFrame in c.DecodePacket(oPacket, frame))
                    {
                        PgmSave(oFrame, of);
                    }
                }

                /* flush the decoder */
                foreach (var oFrame in c.DecodePacket(null, frame))
                {
                    PgmSave(oFrame, of);
                }
            }
        }

        private unsafe static void PgmSave(MediaFrame frame, Stream stream)
        {
            var wrap = frame.Ref.linesize[0];
            var xsize = frame.Ref.width;
            var ysize = frame.Ref.height;
            for (int i = 0; i < ysize; i++)
            {
                stream.Write(new ReadOnlySpan<byte>(frame.Ref.data[0] + i * wrap, xsize));
            }
        }
    }
}
