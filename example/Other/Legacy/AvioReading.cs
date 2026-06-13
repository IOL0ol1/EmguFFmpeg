using System.IO;

namespace FFmpeg.Sharp.Example.Legacy
{
    internal class AvioReading : ExampleBase
    {
        public AvioReading() : this($"video-input.mp4")
        { Index = 40; Enable = false; }

        public AvioReading(params string[] args) : base(args)
        { }

        public override void Execute()
        {
            var inputFile = args[0];

            // 8.1.0: MediaDemuxer.Open(Stream) no longer closes the stream by default (leaveOpen: true),
            // so the caller owns it — keep it alive for the demuxer's lifetime and dispose it afterwards.
            using var fs = File.OpenRead(inputFile);
            using var ctx = MediaDemuxer.Open(fs);
            ctx.DumpFormat();
        }
    }
}
