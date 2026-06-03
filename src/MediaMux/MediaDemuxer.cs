using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using FFmpeg.AutoGen;


namespace FFmpeg.Sharp
{
    public unsafe class MediaDemuxer : MediaFormatContext, IReadOnlyList<MediaStream>
    {
        protected MediaIOContext _ioContext;

        /// <summary>
        /// Get <see cref="AVInputFormat"/>
        /// </summary>
        public MediaInputFormat Format => new MediaInputFormat(pFormatContext->iformat);

        public string Url => ((IntPtr)pFormatContext->url).PtrToStringUTF8();

        /// <summary>
        /// Open a demuxer from a managed <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">Source stream.</param>
        /// <param name="iformat">Optional input format hint.</param>
        /// <param name="options">Optional dictionary of muxer options.</param>
        /// <param name="leaveOpen">
        /// When <see langword="true"/> (default), the underlying <paramref name="stream"/> is NOT disposed when this demuxer is disposed.
        /// </param>
        public static MediaDemuxer Open(Stream stream, MediaInputFormat iformat = null, MediaDictionary options = null, bool leaveOpen = true)
        {
            var ioContext = (stream as MediaIOContext) ?? new MediaIOContext(stream, 32768, leaveOpen);
            var output = Open(null, iformat, options, fc =>
            {
                AVFormatContext* f = fc;
                f->pb = ioContext;
            });
            output._ioContext = ioContext;
            return output;
        }

        /// <summary>
        /// Open a demuxer from a path or URL.
        /// </summary>
        public static MediaDemuxer Open(string url, MediaInputFormat iformat = null, MediaDictionary options = null, Action<MediaFormatContext> beforeOpen = null)
        {
            var output = new MediaDemuxer();
            beforeOpen?.Invoke(output);
            int ret;
            if (options == null)
            {
                fixed (AVFormatContext** ps = &output.pFormatContext)
                    ret = ffmpeg.avformat_open_input(ps, url, iformat, null);
            }
            else
            {
                fixed (AVFormatContext** ps = &output.pFormatContext)
                fixed (AVDictionary** pOptions = &options.pDictionary)
                    ret = ffmpeg.avformat_open_input(ps, url, iformat, pOptions);
            }
            ret.ThrowIfError();
            output.FindStreamInfo(options);
            return output;
        }

        public MediaDemuxer(AVFormatContext* pAVCodecContext, bool isDisposeByOwner = true)
            : base(pAVCodecContext, isDisposeByOwner)
        { }

        public MediaDemuxer()
            : base()
        { }

        public int FindStreamInfo(MediaDictionary options)
        {
            if (options == null)
                return ffmpeg.avformat_find_stream_info(pFormatContext, null).ThrowIfError();
            fixed (AVDictionary** pOptions = &options.pDictionary)
                return ffmpeg.avformat_find_stream_info(pFormatContext, pOptions).ThrowIfError();
        }

        /// <summary>
        /// Find the "best" stream in the file. The best stream is determined according to various heuristics.
        /// Always rewrites <paramref name="codec"/> with the chosen default decoder (or null on failure).
        /// </summary>
        public int FindBestStream(AVMediaType type, ref MediaCodec codec, int wantedStreamNb = -1, int relatedStream = -1, int flags = 0)
        {
            AVCodec* pCodec = codec;
            var ret = ffmpeg.av_find_best_stream(pFormatContext, type, wantedStreamNb, relatedStream, &pCodec, flags).ThrowIfError();
            codec = pCodec == null ? null : new MediaCodec(pCodec);
            return ret;
        }

        /// <summary>
        /// Print detailed information about every input stream.
        /// </summary>
        public void DumpFormat()
        {
            for (int i = 0; i < pFormatContext->nb_streams; i++)
            {
                ffmpeg.av_dump_format(pFormatContext, i, ((IntPtr)pFormatContext->url).PtrToStringUTF8(), 0);
            }
        }

        /// <summary>
        /// Seek timestamp base <see cref="ffmpeg.AV_TIME_BASE"/>. It's more precise than <see cref="Seek(TimeSpan, int)"/>.
        /// </summary>
        public int Seek(long timestamp, int streamIndex = -1)
        {
            if (streamIndex >= 0)
                timestamp = ffmpeg.av_rescale_q(timestamp, ffmpeg.av_get_time_base_q(), pFormatContext->streams[streamIndex]->time_base);
            return ffmpeg.avformat_seek_file(pFormatContext, streamIndex, long.MinValue, timestamp, timestamp, 0).ThrowIfError();
        }

        /// <summary>
        /// <see cref="Seek(long, int)"/>
        /// </summary>
        public int Seek(TimeSpan time, int streamIndex = -1)
        {
            return Seek((long)(time.TotalSeconds * ffmpeg.AV_TIME_BASE), streamIndex);
        }

        #region IReadOnlyList<MediaStream>

        public int Count => (int)pFormatContext->nb_streams;

        public MediaStream this[int index] => new MediaStream(pFormatContext->streams[index]);

        public IEnumerator<MediaStream> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion IReadOnlyList<MediaStream>

        #region ReadPackets

        /// <summary>
        /// Yields demuxed packets one at a time.
        /// <para>
        /// <b>Lifetime:</b> the yielded <see cref="MediaPacket"/> is the SAME instance every iteration. Its data is
        /// automatically unrefed before the next MoveNext, so do NOT enqueue, capture, or LINQ-buffer it.
        /// Call <see cref="MediaPacket.Clone"/> if you need to outlive the next iteration, or use
        /// <see cref="ReadPacketsCloned"/> which clones for you.
        /// </para>
        /// <para>
        /// Returns cleanly on EOF; only real errors (e.g. corrupt streams, broken IO) throw.
        /// </para>
        /// </summary>
        public IEnumerable<MediaPacket> ReadPackets(MediaPacket inPacket = null)
        {
            MediaPacket packet = inPacket ?? new MediaPacket();
            try
            {
                while (true)
                {
                    int ret = ReadPacketSafe(packet);
                    if (ret == ffmpeg.AVERROR_EOF)
                        yield break;
                    if (ret < 0)
                        ret.ThrowIfError();
                    try { yield return packet; }
                    finally { packet.Unref(); }
                }
            }
            finally { if (inPacket == null) packet.Dispose(); }
        }

        /// <summary>
        /// Like <see cref="ReadPackets"/> but every yielded packet is an independent owned clone — safe to enqueue or
        /// pass to another thread. The caller is responsible for disposing each clone.
        /// </summary>
        public IEnumerable<MediaPacket> ReadPacketsCloned()
        {
            using (var scratch = new MediaPacket())
            {
                while (true)
                {
                    int ret = ReadPacketSafe(scratch);
                    if (ret == ffmpeg.AVERROR_EOF)
                        yield break;
                    if (ret < 0)
                        ret.ThrowIfError();
                    var owned = scratch.Clone();
                    scratch.Unref();
                    yield return owned;
                }
            }
        }

        /// <summary>
        /// One-shot helper: route demuxed packets through the supplied decoders and yield decoded frames.
        /// Decoders are keyed by stream index. Streams without a decoder mapping are skipped.
        /// The decoders are flushed automatically on EOF.
        /// </summary>
        /// <param name="decoders">Map of stream_index → decoder.</param>
        /// <param name="filterMediaType">If non-null, only stream indices whose codecpar matches this media type are decoded.</param>
        public IEnumerable<(int streamIndex, MediaFrame frame)> ReadFrames(IDictionary<int, MediaDecoder> decoders, AVMediaType? filterMediaType = null)
        {
            if (decoders == null) throw new ArgumentNullException(nameof(decoders));
            using (var pkt = new MediaPacket())
            {
                while (true)
                {
                    int ret = ReadPacketSafe(pkt);
                    if (ret == ffmpeg.AVERROR_EOF) break;
                    if (ret < 0) ret.ThrowIfError();
                    try
                    {
                        int idx = pkt.Ref.stream_index;
                        if (!decoders.TryGetValue(idx, out var dec) || dec == null) continue;
                        if (filterMediaType.HasValue && this[idx].CodecparRef.codec_type != filterMediaType.Value) continue;
                        foreach (var frame in dec.DecodePacket(pkt))
                            yield return (idx, frame);
                    }
                    finally { pkt.Unref(); }
                }
                // Flush each decoder.
                foreach (var kv in decoders)
                {
                    if (kv.Value == null) continue;
                    foreach (var frame in kv.Value.DecodePacket(null))
                        yield return (kv.Key, frame);
                }
            }
        }

        protected int ReadPacketSafe(MediaPacket packet)
        {
            return ffmpeg.av_read_frame(pFormatContext, packet);
        }

        #endregion ReadPackets

        #region IDisposable
        private bool disposedValue;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (pFormatContext != null)
                {
                    if (_ioContext != null)
                    {
                        AVIOContext* pb = _ioContext;
                        _ioContext.Dispose();
                        if (pb == pFormatContext->pb)
                            pFormatContext->pb = null;
                    }
                    base.Dispose(disposing);
                    pFormatContext = null;
                }
                disposedValue = true;
            }
        }

        #endregion IDisposable
    }
}
