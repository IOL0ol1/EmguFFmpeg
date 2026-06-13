using System;
using System.Collections;
using System.Collections.Generic;
using FFmpeg.AutoGen;


namespace FFmpeg.Sharp
{
    public unsafe partial class MediaDecoder  : MediaCodecContext
    { 

        public MediaDecoder(AVCodecContext* pAVCodecContext, bool leaveOpen)
            : base(pAVCodecContext, leaveOpen)
        { }

        public MediaDecoder(MediaCodec codec = null)
            : base(codec)
        { }

        /// <summary>Open the decoder. See <see cref="MediaCodecContext.Open"/>.</summary>
        public new MediaDecoder Open(MediaDictionary opts = null) => (MediaDecoder)base.Open(opts);

        #region Create

        /// <summary>
        /// Create and open a <see cref="MediaDecoder"/> from <see cref="AVCodecParameters"/> (e.g. a stream's codecpar).
        /// <para>
        /// For custom before-open configuration, use the ctor + <see cref="Open"/> instead:
        /// <code>
        /// using var dec = new MediaDecoder(MediaCodec.FindDecoder(stream.CodecparRef.codec_id));
        /// dec.SetCodecParameters(ref stream.CodecparRef);
        /// dec.Ref.thread_count = 10;
        /// dec.Open();
        /// </code>
        /// </para>
        /// </summary>
        /// <param name="codecParameters">Source codec parameters.</param>
        /// <param name="opts">Codec options forwarded to <see cref="Open"/>.</param>
        /// <returns>An opened decoder, or null when codec_id is AV_CODEC_ID_NONE / no decoder is found.</returns>
        public static MediaDecoder CreateDecoder(AVCodecParameters codecParameters, MediaDictionary opts = null)
        {
            var codec = MediaCodec.FindDecoder(codecParameters.codec_id);
            // If codec_id is AV_CODEC_ID_NONE return null
            return codec == null ? null : CreateDecoder(codecParameters, codec, opts);
        }

        /// <summary>
        /// Create and open a <see cref="MediaDecoder"/> for an explicit <paramref name="codec"/> (e.g. "h264_qsv"),
        /// filling its context from <paramref name="codecParameters"/> before opening.
        /// </summary>
        /// <param name="codecParameters">Source codec parameters.</param>
        /// <param name="codec">The explicit decoder to open.</param>
        /// <param name="opts">Codec options forwarded to <see cref="Open"/>.</param>
        public static MediaDecoder CreateDecoder(AVCodecParameters codecParameters, MediaCodec codec, MediaDictionary opts = null)
        {
            if (codec == null) throw new ArgumentNullException(nameof(codec));
            var output = new MediaDecoder(codec);
            output.SetCodecParameters(ref codecParameters);
            return output.Open(opts);
        }

        #endregion

        /// <summary>
        /// <see cref="ffmpeg.avcodec_send_packet(AVCodecContext*, AVPacket*)"/>
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public int SendPacket(MediaPacket packet) => ffmpeg.avcodec_send_packet(pCodecContext, packet);

        /// <summary>
        /// <see cref="ffmpeg.avcodec_receive_frame(AVCodecContext*, AVFrame*)"/>
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public int ReceiveFrame(MediaFrame frame) => ffmpeg.avcodec_receive_frame(pCodecContext, frame);

        /// <summary>
        /// Decode <paramref name="packet"/> into one or more <see cref="MediaFrame"/>s.
        /// Pass <see langword="null"/> to flush.
        /// </summary>
        /// <param name="packet">Input packet, or <see langword="null"/> to flush the decoder.</param>
        /// <param name="inFrame">Optional reusable receive frame (avoids per-call allocation).</param>
        /// <param name="swFrame">
        /// Optional reusable SW destination frame for HW-accelerated decoding. When the codec context was
        /// configured with <c>InitHWDeviceContext</c>, frames are automatically transferred from GPU to system
        /// memory and yielded as <paramref name="swFrame"/>.
        /// Pass <see langword="null"/> to disable auto-download (zero-copy GPU frame path) when the codec is HW-backed —
        /// in that case the original GPU frame is yielded and the caller is responsible for further hwframe handling.
        /// </param>
        /// <param name="flags">Flags forwarded to <see cref="ffmpeg.av_hwframe_transfer_data"/>.</param>
        /// <returns>
        /// A single-pass receive sequence over the decoder's buffered output. <c>foreach</c> compiles against the
        /// struct enumerator (zero allocation); using it as <see cref="IEnumerable{T}"/> (LINQ etc.) boxes.
        /// The packet is sent eagerly at call time — not enumerating only delays frame retrieval, it cannot lose data.
        /// </returns>
        public Frames DecodePacket(MediaPacket packet, MediaFrame inFrame = null, MediaFrame swFrame = null, int flags = 0)
        {
            int ret = SendPacket(packet);
            // EAGAIN from send means the decoder is full and the caller is expected to drain receive first.
            // We surface this as an explicit exception so the caller doesn't silently lose packets.
            if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                throw new FFmpegException(ret,
                    "avcodec_send_packet returned EAGAIN. Drain the decoder with ReceiveFrame before sending more packets, or enumerate the previous DecodePacket result to completion before re-invoking.");
            if (ret != ffmpeg.AVERROR_EOF)
                ret.ThrowIfError();
            return new Frames(this, inFrame, swFrame, flags);
        }

        /// <summary>Single-pass receive sequence returned by <see cref="DecodePacket"/>.</summary>
        public readonly struct Frames : IEnumerable<MediaFrame>
        {
            private readonly MediaDecoder _decoder;
            private readonly MediaFrame _inFrame;
            private readonly MediaFrame _swFrame;
            private readonly int _flags;

            internal Frames(MediaDecoder decoder, MediaFrame inFrame, MediaFrame swFrame, int flags)
            {
                _decoder = decoder;
                _inFrame = inFrame;
                _swFrame = swFrame;
                _flags = flags;
            }

            public Enumerator GetEnumerator() => new Enumerator(_decoder, _inFrame, _swFrame, _flags);

            public struct Enumerator : IEnumerator<MediaFrame>
            {
                private readonly MediaDecoder _decoder;
                private readonly MediaFrame _swFrame;
                private readonly int _flags;
                private readonly bool _ownsFrame;
                private readonly bool _wantTransfer;
                private readonly int _eagain;
                private MediaFrame _frame;
                private MediaFrame _current;

                internal Enumerator(MediaDecoder decoder, MediaFrame inFrame, MediaFrame swFrame, int flags)
                {
                    _decoder = decoder;
                    _swFrame = swFrame;
                    _flags = flags;
                    _ownsFrame = inFrame == null;
                    _frame = inFrame ?? new MediaFrame();
                    // Auto-transfer only when both HW is active AND user gave us a destination buffer.
                    // If swFrame == null and HW is active, the GPU frame is yielded directly (zero-copy path).
                    _wantTransfer = decoder.HasHWDevice && swFrame != null;
                    _eagain = ffmpeg.AVERROR(ffmpeg.EAGAIN);
                    _current = null;
                }

                public MediaFrame Current => _current;

                public bool MoveNext()
                {
                    ReleaseCurrent();
                    int ret = _decoder.ReceiveFrame(_frame);
                    if (ret == _eagain || ret == ffmpeg.AVERROR_EOF) return false;
                    ret.ThrowIfError();
                    if (_wantTransfer)
                    {
                        // Transfer first, then copy props — transfer may reset destination fields,
                        // so the order matches MediaFrame.TransferToSoftware.
                        HWFrameTransferData(_swFrame, _frame, _flags);
                        _frame.CopyProps(_swFrame);
                        _current = _swFrame;
                    }
                    else
                    {
                        _current = _frame;
                    }
                    return true;
                }

                public void Dispose()
                {
                    ReleaseCurrent(); // runs on early break too, so a pending frame is always returned to the pool
                    if (_ownsFrame && _frame != null)
                    {
                        _frame.Dispose();
                        _frame = null;
                    }
                }

                // Hand the consumed frame's buffers back eagerly — ReceiveFrame unrefs the destination on entry
                // anyway, but doing it here keeps the steady-state footprint small and is required for the
                // reused swFrame on the HW path.
                private void ReleaseCurrent()
                {
                    if (_current == null) return;
                    if (_wantTransfer) _swFrame.Unref();
                    _frame.Unref();
                    _current = null;
                }

                object IEnumerator.Current => Current;
                void IEnumerator.Reset() => throw new NotSupportedException();
            }

            // Interface fallback (boxes) — keeps LINQ and IEnumerable<T> consumers working.
            IEnumerator<MediaFrame> IEnumerable<MediaFrame>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
