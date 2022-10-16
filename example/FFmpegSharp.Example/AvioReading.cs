using System.IO;
using FFmpeg.AutoGen;

namespace FFmpegSharp.Example
{
    internal class AvioReading : ExampleBase
    {
        public AvioReading() : this("path-to-your.mp4")
        { }

        public AvioReading(params string[] args) : base(args)
        { }

        public unsafe override void Execute()
        {
            var inputFile = args[0];

            AVFormatContext* fmt_ctx = ffmpeg.avformat_alloc_context();
            var fs = File.OpenRead(inputFile);
            var io = MediaIOContext.Link(fs, 4096);
            fmt_ctx->pb = io;
            ffmpeg.avformat_open_input(&fmt_ctx, null, null, null);
            //ffmpeg.avformat_find_stream_info(fmt_ctx, null);
            var ctx = new MediaDemuxer(new MediaFormatContext(fmt_ctx));
            ctx.DumpFormat();
            fs.Dispose();
            ctx.Dispose();
        }
    }
}
