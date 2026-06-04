using System;
using System.IO;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: avio_read_callback.c
    /// Demonstrate reading media from an in-memory buffer via a custom AVIOContext read callback.
    /// The entire input file is read into a managed byte array, then an AVIOContext is created
    /// with a read callback that serves bytes from that buffer — no disk access after the initial read.
    /// </summary>
    public unsafe class AvioReadCallback : ExampleBase
    {
        public AvioReadCallback() { Index = 2; Enable = false; }

        public override void Execute()
        {
            var inFile = args.Length > 0 ? args[0] : "input.mp4";

            // Slurp the whole file into a managed byte array.
            var buffer = File.ReadAllBytes(inFile);

            // Open a MemoryStream backed by that buffer.
            // MediaDemuxer.Open(Stream) creates an AVIOContext with read/seek callbacks internally.
            using var ms = new MemoryStream(buffer);
            using var demuxer = MediaDemuxer.Open(ms);

            demuxer.DumpFormat();

            Console.WriteLine($"Number of streams: {demuxer.Ref.nb_streams}");
            for (int i = 0; i < (int)demuxer.Ref.nb_streams; i++)
            {
                var st = demuxer.Ref.streams[i];
                Console.WriteLine($"  Stream #{i}: codec_type={st->codecpar->codec_type}, codec_id={st->codecpar->codec_id}");
            }
        }
    }
}
