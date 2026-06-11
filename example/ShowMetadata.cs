using System;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: show_metadata.c
    /// Open an input file and print all metadata key=value pairs.
    /// </summary>
    public class ShowMetadata : ExampleBase
    {
        public ShowMetadata() { Index = 1; Enable = false; }

        public override void Execute()
        {
            var inFile = args.Length > 0 ? args[0] : "input.mp4";

            using var demuxer = MediaDemuxer.Open(inFile);
            demuxer.DumpFormat();

            // Borrowed managed view over the native dictionary (demuxer owns the pointer).
            var meta = demuxer.Metadata;
            if (meta != null)
                foreach (var kv in meta)
                    Console.WriteLine($"{kv.Key}={kv.Value}");
        }
    }
}
