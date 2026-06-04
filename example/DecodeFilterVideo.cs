using System;
using System.Threading;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: decode_filter_video.c
    /// Demux + decode a video stream then push each frame through a
    /// "scale=78:24,transpose=cclock" filter graph, displaying the result as ASCII art.
    /// </summary>
    public unsafe class DecodeFilterVideo : ExampleBase
    {
        private const string FilterDescr = "scale=78:24,transpose=cclock";

        public DecodeFilterVideo() { Index = 9; Enable = false; }

        public override void Execute()
        {
            var inFile = args.Length > 0 ? args[0] : "input.mp4";

            // ── Open input ────────────────────────────────────────────────────
            using var demuxer = MediaDemuxer.Open(inFile);
            demuxer.DumpFormat();

            MediaCodec videoCodec = null;
            int videoStreamIdx = demuxer.FindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, ref videoCodec);
            if (videoStreamIdx < 0) throw new Exception("No video stream found");

            var codecPar = *demuxer.Ref.streams[videoStreamIdx]->codecpar;
            using var decoder = MediaDecoder.CreateDecoder(codecPar);

            var timeBase = demuxer.Ref.streams[videoStreamIdx]->time_base;

            // ── Build filter graph using MediaFilterGraph wrapper ─────────────
            using var graph = new MediaFilterGraph();

            // buffer source — parameters come from the decoder context.
            var srcCtx = graph.AddVideoSrcFilter(
                new MediaFilter("buffer"),
                decoder.Ref.width, decoder.Ref.height,
                decoder.Ref.pix_fmt,
                timeBase,
                decoder.Ref.sample_aspect_ratio,
                contextName: "in");

            // buffersink constrained to gray8 (the ASCII art renderer expects 1-byte pixels).
            var sinkCtx = graph.AddVideoSinkFilter(
                new MediaFilter("buffersink"),
                new[] { AVPixelFormat.AV_PIX_FMT_GRAY8 },
                "out");

            // ParseGraph wires FilterDescr between srcCtx ("in") and sinkCtx ("out").
            graph.ParseGraph(FilterDescr, srcCtx, sinkCtx);
            graph.Initialize();

            var sinkTimeBase = sinkCtx.Ref.inputs[0]->time_base;

            // ── Decode + filter loop ──────────────────────────────────────────
            using var frame     = new MediaFrame();
            using var filtFrame = new MediaFrame();
            using var packet    = new MediaPacket();
            long lastPts = ffmpeg.AV_NOPTS_VALUE;

            foreach (var pkt in demuxer.ReadPackets(packet))
            {
                if (pkt.Ref.stream_index != videoStreamIdx) continue;
                foreach (var decoded in decoder.DecodePacket(pkt, frame))
                {
                    decoded.Ref.pts = decoded.Ref.best_effort_timestamp;
                    srcCtx.WriteFrame(decoded, MediaFilterContext.BufferSrcFlagKeepRef);
                    foreach (var filt in sinkCtx.ReadFrame(filtFrame))
                        ThrottleAndDisplay(filt, sinkTimeBase, ref lastPts);
                }
            }
            // EOF flush.
            srcCtx.FlushSrc();
            foreach (var filt in sinkCtx.ReadFrame(filtFrame))
                ThrottleAndDisplay(filt, sinkTimeBase, ref lastPts);
        }

        private static void ThrottleAndDisplay(MediaFrame frame, AVRational sinkTimeBase, ref long lastPts)
        {
            if (frame.Ref.pts != ffmpeg.AV_NOPTS_VALUE)
            {
                if (lastPts != ffmpeg.AV_NOPTS_VALUE)
                {
                    long delayUs = ffmpeg.av_rescale_q(frame.Ref.pts - lastPts,
                        sinkTimeBase, new AVRational { num = 1, den = 1_000_000 });
                    if (delayUs > 0 && delayUs < 1_000_000)
                        Thread.Sleep((int)(delayUs / 1000));
                }
                lastPts = frame.Ref.pts;
            }
            DisplayFrame(frame);
        }

        private static readonly char[] GrayChars = { ' ', '.', '-', '+', '#' };

        private static void DisplayFrame(MediaFrame frame)
        {
            // ANSI clear screen.
            Console.Write("\x1b[2J\x1b[H");
            byte* p0 = frame.Ref.data[0];
            for (int y = 0; y < frame.Ref.height; y++)
            {
                byte* p = p0 + y * frame.Ref.linesize[0];
                for (int x = 0; x < frame.Ref.width; x++)
                    Console.Write(GrayChars[p[x] / 52]);
                Console.WriteLine();
            }
        }
    }
}
