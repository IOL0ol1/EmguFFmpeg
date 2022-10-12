using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFmpeg.AutoGen;

namespace EmguFFmpeg.AppTest
{
    internal class AvioReading : ExampleBase
    {
        public AvioReading(string inputFile = "path-to-your.mp4")
        {
            parames["inputFile"] = inputFile;
        }

        public unsafe override void Execute()
        {
            var inputFile = GetParame<string>("inputFile");

            AVFormatContext* fmt_ctx = ffmpeg.avformat_alloc_context();
            var fs = File.OpenRead(inputFile);
            var io = MediaIOContext.Link(fs, 4096);
            fmt_ctx->pb = io;
            var ret = ffmpeg.avformat_open_input(&fmt_ctx, null, null, null);
            var ctx = new MediaDemuxer(fmt_ctx);
            ctx.DumpFormat();
            fs.Dispose();
            ctx.Dispose();
        }
    }
}
