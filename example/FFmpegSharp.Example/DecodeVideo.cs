using System;
using System.IO;
using FFmpeg.AutoGen;

namespace FFmpegSharp.Example
{
    internal class DecodeVideo : ExampleBase
    {
        public DecodeVideo() : this($"EncodeVideo-output.h264", $"{nameof(DecodeVideo)}-output.mp4")
        { }

        public DecodeVideo(params string[] args) : base(args)
        {
            Index = 12;
            Enable = false;
        }

        public override void Execute()
        {
            var filename = args[0];
            var outfilename = args[1];

            var codec = MediaCodec.FindDecoder(AVCodecID.AV_CODEC_ID_H264);
            using (var f = File.OpenRead(filename))
            using (var of = File.Create(outfilename))
            using (var pkt = new MediaPacket() { Dts = ffmpeg.AV_NOPTS_VALUE, Pts = ffmpeg.AV_NOPTS_VALUE, Pos = 0 })
            using (var parser = new MediaCodecParserContext(codec.Id))
            using (var c = new MediaDecoder(MediaCodecContext.Create(codec, _ =>
             {
                 /* For some codecs, such as msmpeg4 and mpeg4, width and height
                    MUST be initialized there because this information is not
                    available in the bitstream. */
                 _.Height = 288;
                 _.Width = 352;
             })))
            using (var frame = new MediaFrame())
            {
                foreach (var oPacket in parser.ParserPackets(c, f, pkt))
                {
                    foreach (var oFrame in c.DecodePacket(oPacket, frame))
                    {
                        PgmSave(oFrame, of);
                    }
                    pkt.Dts = ffmpeg.AV_NOPTS_VALUE;
                    pkt.Pts = ffmpeg.AV_NOPTS_VALUE;
                    pkt.Pos = 0;
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
