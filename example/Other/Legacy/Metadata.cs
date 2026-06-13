using System;

namespace FFmpeg.Sharp.Example.Legacy
{
    internal class Metadata : ExampleBase
    {
        public Metadata() : this($"video-input.mp4")
        { Index = 47; Enable = false; }

        public Metadata(params string[] args) : base(args)
        { }

        public override void Execute()
        {
            var input = args[0];

            using var fmt = MediaDemuxer.Open(input);

            // Borrowed managed view over the native dictionary (demuxer owns the pointer).
            var meta = fmt.Metadata;
            if (meta != null)
                foreach (var item in meta)
                    Console.WriteLine($"{item.Key}={item.Value}");
        }
    }
}
