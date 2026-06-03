using System;
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

        #region Create

        public static MediaDecoder Create(MediaCodec codec, Action<MediaCodecContext> beforeOpenSetting = null, MediaDictionary opts = null)
        {
            var output = new MediaDecoder(codec);
            beforeOpenSetting?.Invoke(output);
            if (opts == null)
            {
                ffmpeg.avcodec_open2(output, codec, null).ThrowIfError();
            }
            else
            {
                fixed (AVDictionary** pOpts = &opts.pDictionary)
                    ffmpeg.avcodec_open2(output, codec, pOpts).ThrowIfError();
            }
            return output;
        }

        /// <summary>
        /// Create <see cref="AVCodecContext"/> by <see cref="AVCodecParameters"/>. 
        /// <para>
        /// <seealso cref="ffmpeg.avcodec_parameters_to_context(AVCodecContext*, AVCodecParameters*)"/>
        /// </para>
        /// </summary>
        /// <param name="codecParameters"></param>
        /// <param name="action"></param>
        /// <param name="opts"></param>
        /// <returns></returns>
        public static MediaDecoder CreateDecoder(AVCodecParameters codecParameters, Action<MediaCodecContext> action = null, MediaDictionary opts = null)
        {
            var codec = MediaCodec.FindDecoder(codecParameters.codec_id);
            AVCodecParameters* pCodecParameters = &codecParameters;
            // If codec_id is AV_CODEC_ID_NONE return null
            return codec == null
                ? null
                : Create(codec, _ =>
                {
                    ffmpeg.avcodec_parameters_to_context(_, pCodecParameters).ThrowIfError();
                    action?.Invoke(_);
                }, opts);
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
        public IEnumerable<MediaFrame> DecodePacket(MediaPacket packet, MediaFrame inFrame = null, MediaFrame swFrame = null, int flags = 0)
        {
            var isHWDeviceCtxInit = IsHWDeviceCtxInit();
            int ret = SendPacket(packet);
            // EAGAIN from send means the decoder is full and the caller is expected to drain receive first.
            // We surface this as an explicit exception so the caller doesn't silently lose packets.
            if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                throw new FFmpegException(ret,
                    "avcodec_send_packet returned EAGAIN. Drain the decoder with ReceiveFrame before sending more packets, or enumerate the previous DecodePacket result to completion before re-invoking.");
            if (ret != ffmpeg.AVERROR_EOF)
                ret.ThrowIfError();

            MediaFrame _frame = inFrame ?? new MediaFrame();
            // Auto-transfer only when both HW is active AND user gave us a destination buffer.
            // If swFrame == null and HW is active, the GPU frame is yielded directly (zero-copy path).
            bool wantTransfer = isHWDeviceCtxInit && swFrame != null;
            try
            {
                while (true)
                {
                    int rret = ReceiveFrame(_frame);
                    if (rret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || rret == ffmpeg.AVERROR_EOF)
                        yield break;
                    rret.ThrowIfError();

                    try
                    {
                        if (wantTransfer)
                        {
                            _frame.CopyProps(swFrame);
                            HWFrameTransferData(swFrame, _frame, flags);
                            yield return swFrame;
                            swFrame.Unref();
                        }
                        else
                        {
                            yield return _frame;
                        }
                    }
                    finally
                    {
                        // Unref unconditionally between iterations — ReceiveFrame's contract is that it unrefs the
                        // destination internally on entry, but doing it eagerly keeps the steady-state allocation small
                        // and is required when we're reusing the same _frame for the HW path.
                        _frame.Unref();
                    }
                }
            }
            finally
            {
                if (inFrame == null) _frame.Dispose();
            }
        }
    }
}
