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
        /// <param name="findStreamInfo">See <see cref="Open(string, MediaInputFormat, MediaDictionary, Action{MediaFormatContext}, bool)"/>.</param>
        public static MediaDemuxer Open(Stream stream, MediaInputFormat iformat = null, MediaDictionary options = null, bool leaveOpen = true, bool findStreamInfo = true)
        {
            var ioContext = (stream as MediaIOContext) ?? new MediaIOContext(stream, 32768, leaveOpen);
            var output = Open(null, iformat, options, fc =>
            {
                AVFormatContext* f = fc;
                f->pb = ioContext;
            }, findStreamInfo);
            output._ioContext = ioContext;
            return output;
        }

        /// <summary>
        /// Open a demuxer from a path or URL.
        /// </summary>
        /// <param name="url">Path or URL.</param>
        /// <param name="iformat">Optional input format hint.</param>
        /// <param name="options">Optional dictionary of demuxer options.</param>
        /// <param name="beforeOpen">Configuration hook invoked before <c>avformat_open_input</c>.</param>
        /// <param name="findStreamInfo">
        /// When <see langword="true"/> (default), probe the input via <see cref="FindStreamInfo"/> —
        /// this can pre-read megabytes of data. Pass <see langword="false"/> for known-format/low-latency
        /// inputs to skip probing; stream codec parameters may then be incomplete until you call
        /// <see cref="FindStreamInfo"/> yourself or fill the decoder parameters by hand.
        /// </param>
        public static MediaDemuxer Open(string url, MediaInputFormat iformat = null, MediaDictionary options = null, Action<MediaFormatContext> beforeOpen = null, bool findStreamInfo = true)
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
            if (findStreamInfo)
                output.FindStreamInfo(options);
            return output;
        }

        /// <summary>
        /// Wrap an existing <see cref="AVFormatContext"/> pointer.
        /// </summary>
        /// <param name="pAVFormatContext">Native format context (must be opened for input).</param>
        /// <param name="leaveOpen">
        /// When <see langword="true"/>, this wrapper does NOT close/free the context on dispose; the caller retains ownership.
        /// When <see langword="false"/>, the wrapper takes ownership.
        /// </param>
        public MediaDemuxer(AVFormatContext* pAVFormatContext, bool leaveOpen)
            : base(pAVFormatContext, leaveOpen)
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
                timestamp = timestamp.Rescale(ffmpeg.av_get_time_base_q(), pFormatContext->streams[streamIndex]->time_base);
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
                    int ret = ReadPacket(packet);
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
                    int ret = ReadPacket(scratch);
                    if (ret == ffmpeg.AVERROR_EOF)
                        yield break;
                    if (ret < 0)
                        ret.ThrowIfError();
                    yield return MoveToOwned(scratch);
                }
            }
        }

        // Ownership transfer via av_packet_move_ref instead of Clone+Unref: no side-data deep copy
        // (av_packet_ref would duplicate it just for the source's unref to free the original), no buffer
        // refcount churn, and no failure path. scratch is left blank, ready for the next av_read_frame.
        // (Also keeps the pointer code out of the iterator body.)
        private static MediaPacket MoveToOwned(MediaPacket scratch)
        {
            var owned = new MediaPacket();
            ffmpeg.av_packet_move_ref(owned, scratch);
            return owned;
        }

        public int ReadPacket(MediaPacket packet)
        {
            return ffmpeg.av_read_frame(pFormatContext, packet);
        }

        // Per-packet hot path: read codec_type without allocating a MediaStream wrapper
        // (also keeps the pointer code out of the iterator body).
        private AVMediaType GetStreamCodecType(int streamIndex)
            => pFormatContext->streams[streamIndex]->codecpar->codec_type;

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
