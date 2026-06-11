using System;
using System.IO;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: decode_filter_audio.c
    /// Demux + decode an audio stream then push each decoded frame through an
    /// "aresample=8000,aformat=sample_fmts=s16:channel_layouts=mono" filter graph,
    /// writing the S16LE 8 kHz mono output to stdout (pipe to ffplay).
    /// </summary>
    public unsafe class DecodeFilterAudio : ExampleBase
    {
        private const string FilterDescr = "aresample=8000,aformat=sample_fmts=s16:channel_layouts=mono";

        public DecodeFilterAudio() { Index = 8; Enable = false; }

        public override void Execute()
        {
            var inFile  = args.Length > 0 ? args[0] : "input.mp4";
            var outFile = args.Length > 1 ? args[1] : "out_filtered_audio.s16le";

            // ── Open input ────────────────────────────────────────────────────
            using var demuxer = MediaDemuxer.Open(inFile);
            demuxer.DumpFormat();

            MediaCodec audioCodec = null;
            int audioStreamIdx = demuxer.FindBestStream(AVMediaType.AVMEDIA_TYPE_AUDIO, ref audioCodec);
            if (audioStreamIdx < 0) throw new Exception("No audio stream found");

            using var decoder = MediaDecoder.CreateDecoder(demuxer[audioStreamIdx].CodecparRef);
            // Note: AddAudioSrcFilter automatically normalises AV_CHANNEL_ORDER_UNSPEC.

            var timeBase = demuxer[audioStreamIdx].Ref.time_base;

            // ── Build filter graph using MediaFilterGraph wrapper ─────────────
            using var graph = new MediaFilterGraph();

            // abuffer source: pass the stream time_base explicitly (may differ from 1/samplerate).
            var srcCtx = graph.AddAudioSrcFilter(
                new MediaFilter("abuffer"),
                decoder.Ref.ch_layout,
                timeBase,
                decoder.Ref.sample_rate,
                decoder.Ref.sample_fmt,
                "in");

            // abuffersink: the filter chain (aresample+aformat) handles format conversion.
            var sinkCtx = graph.AddFilter(new MediaFilter("abuffersink"), (string)null, "out");

            // ParseGraph wires FilterDescr between srcCtx ("in") and sinkCtx ("out").
            graph.ParseGraph(FilterDescr, srcCtx, sinkCtx);
            graph.Initialize();

            Console.Error.WriteLine($"Play: ffplay -f s16le -ar 8000 -ac 1 {outFile}");

            // ── Decode + filter loop ──────────────────────────────────────────
            using var frame     = new MediaFrame();
            using var filtFrame = new MediaFrame();
            using var packet    = new MediaPacket();
            using var outStream = File.OpenWrite(outFile);

            foreach (var pkt in demuxer.ReadPackets(packet))
            {
                if (pkt.Ref.stream_index != audioStreamIdx) continue;
                foreach (var decoded in decoder.DecodePacket(pkt, frame))
                {
                    srcCtx.WriteFrame(decoded, MediaFilterContext.BufferSrcFlagKeepRef);
                    foreach (var filt in sinkCtx.ReadFrame(filtFrame))
                    {
                        int n = filt.Ref.nb_samples * filt.Ref.ch_layout.nb_channels;
                        outStream.Write(new ReadOnlySpan<byte>(filt.Ref.data[0], n * sizeof(short)));
                    }
                }
            }
            // EOF flush.
            srcCtx.FlushSrc();
            foreach (var filt in sinkCtx.ReadFrame(filtFrame))
            {
                int n = filt.Ref.nb_samples * filt.Ref.ch_layout.nb_channels;
                outStream.Write(new ReadOnlySpan<byte>(filt.Ref.data[0], n * sizeof(short)));
            }
        }
    }
}
