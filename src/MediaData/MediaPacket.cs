using System;
using FFmpeg.AutoGen;


namespace FFmpeg.Sharp
{
    public unsafe partial class MediaPacket : IDisposable, ICloneable
    {
        /// <summary>
        /// Wrap an existing <see cref="AVPacket"/> pointer.
        /// </summary>
        /// <param name="pAVPacket">Native packet pointer (must be non-null).</param>
        /// <param name="leaveOpen">
        /// When <see langword="true"/>, the wrapper does NOT call <see cref="ffmpeg.av_packet_free(AVPacket**)"/> on dispose.
        /// When <see langword="false"/>, the wrapper takes ownership and will free the packet.
        /// </param>
        public MediaPacket(AVPacket* pAVPacket, bool leaveOpen)
            : this(pAVPacket)
        {
            disposedValue = leaveOpen;
        }


        /// <summary>
        /// Allocate a new empty packet via <see cref="ffmpeg.av_packet_alloc()"/>.
        /// The wrapper owns the native packet and will free it on dispose.
        /// </summary>
        public MediaPacket()
            : this(ffmpeg.av_packet_alloc(), false)
        { }

        /// <summary>
        /// Allocate a new <see cref="MediaPacket"/> and copy data from <paramref name="source"/> into it.
        /// </summary>
        public static MediaPacket FromBuffer(ReadOnlySpan<byte> source)
        {
            var pkt = new MediaPacket();
            if (source.Length == 0) return pkt;
            ffmpeg.av_new_packet(pkt, source.Length).ThrowIfError();
            fixed (byte* src = source)
            {
                Buffer.MemoryCopy(src, pkt.Ref.data, pkt.Ref.size, source.Length);
            }
            return pkt;
        }

        /// <summary>
        /// Read-only view of the packet's compressed payload.
        /// </summary>
        public ReadOnlySpan<byte> Data => pPacket->data == null
            ? ReadOnlySpan<byte>.Empty
            : new ReadOnlySpan<byte>(pPacket->data, pPacket->size);

        /// <summary>
        /// Writable view of the packet's compressed payload (use after <see cref="ffmpeg.av_packet_make_writable"/>).
        /// </summary>
        public Span<byte> AsSpan() => pPacket->data == null
            ? Span<byte>.Empty
            : new Span<byte>(pPacket->data, pPacket->size);

        /// <summary>
        /// <see cref="ffmpeg.av_packet_unref(AVPacket*)"/>
        /// </summary>
        public void Unref()
        {
            ffmpeg.av_packet_unref(pPacket);
        }

        /// <summary>
        /// Convert the packet's pts/dts/duration from one time base to another via
        /// <see cref="ffmpeg.av_packet_rescale_ts(AVPacket*, AVRational, AVRational)"/>
        /// (AV_NOPTS_VALUE timestamps are left untouched).
        /// </summary>
        public void RescaleTs(AVRational srcTimeBase, AVRational dstTimeBase)
        {
            ffmpeg.av_packet_rescale_ts(pPacket, srcTimeBase, dstTimeBase);
        }

        /// <summary>
        /// Deep copy via <see cref="ffmpeg.av_packet_clone(AVPacket*)"/>. The returned packet is owned by the caller and must be disposed.
        /// </summary>
        /// <exception cref="FFmpegException"/>
        public MediaPacket Clone()
        {
            return new MediaPacket(ffmpeg.av_packet_clone(this), leaveOpen: false);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        #region IDisposable Support

        // Default `false` (owned). The (AVPacket*, bool leaveOpen) ctor flips to `true` for borrowed pointers.
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                fixed (AVPacket** ppPacket = &pPacket)
                {
                    ffmpeg.av_packet_free(ppPacket);
                }

                disposedValue = true;
            }
        }

        ~MediaPacket()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}
