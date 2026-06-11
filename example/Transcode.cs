using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: transcode.c
    /// Full transcode pipeline: demux → decode → passthrough filter → re-encode → mux.
    /// Uses "null" for video and "anull" for audio as the filter descriptors
    /// (identity / passthrough), re-encoding each stream to the same codec.
    /// </summary>
    public unsafe class Transcode : ExampleBase
    {
        public Transcode() { Index = 18; Enable = false; }

        public override void Execute()
        {
            var inFile  = args.Length > 0 ? args[0] : "input.mp4";
            var outFile = args.Length > 1 ? args[1] : "out_transcode.mp4";

            // ── Open demuxer ──────────────────────────────────────────────────
            using var demuxer = MediaDemuxer.Open(inFile);
            demuxer.DumpFormat();

            int nb = (int)demuxer.Ref.nb_streams;

            // Per-stream decoders.
            var decoders = new MediaDecoder[nb];
            for (int i = 0; i < nb; i++)
            {
                var inStream = demuxer[i];
                if (inStream.CodecparRef.codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO ||
                    inStream.CodecparRef.codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                {
                    decoders[i] = MediaDecoder.CreateDecoder(inStream.CodecparRef, ctx =>
                    {
                        if (inStream.CodecparRef.codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                            ctx.Ref.framerate = demuxer.GuessFrameRate(inStream);
                    });
                }
            }

            // ── Open muxer + encoders ─────────────────────────────────────────
            using var muxer  = MediaMuxer.Create(outFile);
            var encoders = new MediaEncoder[nb];

            for (int i = 0; i < nb; i++)
            {
                var inStream = demuxer[i];
                var dec      = decoders[i];

                if (dec == null)
                {
                    // Remux subtitle/data streams unchanged.
                    muxer.AddStream(inStream.CodecparRef);
                    continue;
                }

                var encCodec = MediaCodec.FindEncoder(dec.Ref.codec_id);
                if (encCodec == null) throw new Exception($"No encoder found for codec {dec.Ref.codec_id}");

                encoders[i] = MediaEncoder.Create(encCodec, ctx =>
                {
                    if (dec.Ref.codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                    {
                        ctx.Ref.height              = dec.Ref.height;
                        ctx.Ref.width               = dec.Ref.width;
                        ctx.Ref.sample_aspect_ratio = dec.Ref.sample_aspect_ratio;
                        ctx.Ref.pix_fmt             = dec.Ref.pix_fmt;
                        ctx.Ref.time_base           = dec.Ref.framerate.ToInvert();
                    }
                    else
                    {
                        ctx.Ref.sample_rate = dec.Ref.sample_rate;
                        ctx.Ref.ch_layout   = dec.Ref.ch_layout;
                        ctx.Ref.sample_fmt  = dec.Ref.sample_fmt;
                        ctx.Ref.time_base   = new AVRational { num = 1, den = dec.Ref.sample_rate };
                    }
                    if ((muxer.Ref.oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
                        ctx.Ref.flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
                });

                muxer.AddStream(encoders[i]);
            }

            muxer.DumpFormat();
            muxer.WriteHeader();

            // ── Build per-stream filter graphs using MediaFilterGraph wrapper ─
            var filtSrcCtxs  = new MediaFilterContext[nb];
            var filtSinkCtxs = new MediaFilterContext[nb];
            var filtGraphs   = new MediaFilterGraph[nb];

            for (int i = 0; i < nb; i++)
            {
                var dec      = decoders[i];
                var enc      = encoders[i];
                if (dec == null || enc == null) continue;

                var graph      = new MediaFilterGraph();
                filtGraphs[i]  = graph;
                var inStream   = demuxer[i];
                string filterSpec;

                if (dec.Ref.codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    filterSpec = "null";

                    filtSrcCtxs[i] = graph.AddVideoSrcFilter(
                        new MediaFilter("buffer"),
                        dec.Ref.width, dec.Ref.height,
                        dec.Ref.pix_fmt,
                        inStream.Ref.time_base,
                        dec.Ref.sample_aspect_ratio,
                        contextName: "in");

                    filtSinkCtxs[i] = graph.AddVideoSinkFilter(
                        new MediaFilter("buffersink"),
                        new[] { enc.Ref.pix_fmt },
                        "out");
                }
                else
                {
                    filterSpec = "anull";

                    // AddAudioSrcFilter now accepts an explicit timeBase and auto-normalizes UNSPEC layouts.
                    filtSrcCtxs[i] = graph.AddAudioSrcFilter(
                        new MediaFilter("abuffer"),
                        dec.Ref.ch_layout,
                        inStream.Ref.time_base,
                        dec.Ref.sample_rate,
                        dec.Ref.sample_fmt,
                        "in");

                    // AddAudioSinkFilter(AVChannelLayout[]) overload uses string-based ch_layouts option.
                    filtSinkCtxs[i] = graph.AddAudioSinkFilter(
                        new MediaFilter("abuffersink"),
                        new[] { enc.Ref.sample_fmt },
                        new[] { enc.Ref.sample_rate },
                        new[] { enc.Ref.ch_layout },
                        "out");
                }

                graph.ParseGraph(filterSpec, filtSrcCtxs[i], filtSinkCtxs[i]);
                graph.Initialize();

                // Set encoder frame size on sink (must be done after Initialize).
                if (dec.Ref.codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO && enc.Ref.frame_size > 0)
                    filtSinkCtxs[i].BufferSinkSetFrameSize((uint)enc.Ref.frame_size);
            }

            // ── Main transcode loop ───────────────────────────────────────────
            using var decFrame  = new MediaFrame();
            using var filtFrame = new MediaFrame();
            using var encPkt    = new MediaPacket();
            using var pkt       = new MediaPacket();

            foreach (var p in demuxer.ReadPackets(pkt))
            {
                int si = p.Ref.stream_index;
                if (si < nb && decoders[si] != null)
                {
                    decoders[si].SendPacket(p).ThrowIfError();
                    DrainDecodeAndEncode(si, decoders[si], encoders[si],
                        filtSrcCtxs[si], filtSinkCtxs[si],
                        decFrame, filtFrame, encPkt, muxer);
                }
            }
            // Flush all decoders.
            for (int i = 0; i < nb; i++)
            {
                if (decoders[i] == null || encoders[i] == null) continue;
                decoders[i].SendPacket(null).ThrowIfError();
                DrainDecodeAndEncode(i, decoders[i], encoders[i],
                    filtSrcCtxs[i], filtSinkCtxs[i],
                    decFrame, filtFrame, encPkt, muxer);
                // Flush filter graph.
                filtSrcCtxs[i].FlushSrc();
                DrainFilterAndEncode(i, encoders[i], filtSinkCtxs[i], filtFrame, encPkt, muxer);
                // Flush encoder.
                foreach (var ep in encoders[i].EncodeFrame(null, encPkt))
                {
                    ep.Ref.stream_index = i;
                    muxer.WritePacket(ep, encoders[i]); // auto-rescales encoder.time_base → stream time_base
                }
            }

            muxer.WriteTrailer();

            // Cleanup.
            for (int i = 0; i < nb; i++)
            {
                filtGraphs[i]?.Dispose();
                decoders[i]?.Dispose();
                encoders[i]?.Dispose();
            }

            Console.WriteLine($"Transcode complete: '{inFile}' → '{outFile}'");
        }

        private static void DrainDecodeAndEncode(int si, MediaDecoder decoder, MediaEncoder encoder,
            MediaFilterContext srcCtx, MediaFilterContext sinkCtx,
            MediaFrame decFrame, MediaFrame filtFrame, MediaPacket encPkt, MediaMuxer muxer)
        {
            int ret;
            while ((ret = decoder.ReceiveFrame(decFrame)) >= 0)
            {
                decFrame.Ref.pts = decFrame.Ref.best_effort_timestamp;
                srcCtx.WriteFrame(decFrame, MediaFilterContext.BufferSrcFlagKeepRef);
                DrainFilterAndEncode(si, encoder, sinkCtx, filtFrame, encPkt, muxer);
                decFrame.Unref();
            }
        }

        private static void DrainFilterAndEncode(int si, MediaEncoder encoder,
            MediaFilterContext sinkCtx, MediaFrame filtFrame, MediaPacket encPkt, MediaMuxer muxer)
        {
            foreach (var filt in sinkCtx.ReadFrame(filtFrame))
            {
                foreach (var p in encoder.EncodeFrame(filt, encPkt))
                {
                    p.Ref.stream_index = si;
                    muxer.WritePacket(p, encoder); // auto-rescales encoder.time_base → stream time_base
                }
            }
        }
    }
}
