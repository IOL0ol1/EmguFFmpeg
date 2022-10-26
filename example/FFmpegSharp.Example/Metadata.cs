using System;

namespace FFmpegSharp.Example
{
    internal class Metadata : ExampleBase
    {
        public Metadata() : this($"video-input.mp4")
        {

            Index = -99999;
        }

        public Metadata(params string[] args) : base(args)
        { }

        public unsafe override void Execute()
        {
            var input = args[0];
            var fmt = MediaDemuxer.Open(input);

            var a = fmt.Ref.metadata;
            var m = new MediaDictionary(a, false);
            foreach (var item in m)
            {
                Console.WriteLine($"{item.Key}={item.Value}");
            }

            fmt.Dispose();

        }
    }
}
