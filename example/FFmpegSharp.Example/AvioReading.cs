using System.IO;
using FFmpeg.AutoGen;

namespace FFmpegSharp.Example
{
    internal class AvioReading : ExampleBase
    {
        public AvioReading() : this($"video-input.mp4")
        { }

        public AvioReading(params string[] args) : base(args)
        { }

        public unsafe override void Execute()
        {
            var inputFile = args[0];

            var fs = File.OpenRead(inputFile);
            var ctx = MediaDemuxer.Open(null, null, beforeOpen: _ =>
            {
                ((AVFormatContext*)_)->pb = fs.CreateIOContext(4096);
            });  
            ctx.DumpFormat();
            fs.Dispose();
            ctx.Dispose();
        }
    }
}
