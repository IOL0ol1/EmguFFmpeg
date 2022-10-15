using System;
using System.IO;
using FFmpeg.AutoGen;

namespace FFmpegSharp.Example
{
    internal class DecodeVideo : ExampleBase
    {
        public DecodeVideo() : this("path-to-your.h264", "path-to-your.mp4")
        { }

        public DecodeVideo(params string[] args) : base(args)
        { }

        public override void Execute()
        {
            var filename = args[0];
            var outfilename = args[1];

            var codec = MediaCodec.FindDecoder(AVCodecID.AV_CODEC_ID_MPEG1VIDEO);
            using (var f = File.OpenRead(filename))
            using (var of = File.Create(outfilename))
            using (var pkt = new MediaPacket())
            using (var parser = new MediaCodecParserContext(codec.Id))
            using (var c = new MediaDecoder(MediaCodecContext.Create(_ =>
             {
                 /* For some codecs, such as msmpeg4 and mpeg4, width and height
                    MUST be initialized there because this information is not
                    available in the bitstream. */
             }, codec)))
            using (var frame = new MediaFrame())
            {
                foreach (var oPacket in parser.ParserPackets(c.Context, f, pkt))
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
            var wrap = frame.Linesize[0];
            var xsize = frame.Width;
            var ysize = frame.Height;
            for (int i = 0; i < ysize; i++)
            {
                stream.Write(new ReadOnlySpan<byte>(frame.Data[0] + i * wrap, xsize));
            }
        }
    }
}
