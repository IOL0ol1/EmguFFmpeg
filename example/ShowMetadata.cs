using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: show_metadata.c
    /// Open an input file and print all metadata key=value pairs.
    /// </summary>
    public unsafe class ShowMetadata : ExampleBase
    {
        public ShowMetadata() { Index = 1; Enable = false; }

        public override void Execute()
        {
            var inFile = args.Length > 0 ? args[0] : "input.mp4";

            using var demuxer = MediaDemuxer.Open(inFile);
            demuxer.DumpFormat();

            // Wrap the native dictionary in a managed view (leaveOpen: true — demuxer owns the pointer).
            using var meta = new MediaDictionary(demuxer.Ref.metadata, leaveOpen: true);
            foreach (var kv in meta)
                Console.WriteLine($"{kv.Key}={kv.Value}");
        }
    }
}
