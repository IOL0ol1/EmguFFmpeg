using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;
using FFmpegSharp.Internal;

namespace FFmpegSharp
{
    public unsafe class MediaDecoder : MediaCodecContext
    {

        public MediaDecoder(AVCodecContext* pAVCodecContext, bool isDisposeByOwner = true)
            : base(pAVCodecContext, isDisposeByOwner)
        { }

        public MediaDecoder(MediaCodec codec = null)
                    : base(codec)
        { }

        #region Create

        public static MediaDecoder Create(MediaCodec codec, Action<MediaCodecContextBase> beforeOpenSetting = null, MediaDictionary opts = null)
        {
            var output = new MediaDecoder(codec);
            beforeOpenSetting?.Invoke(output);
            var tmp = opts ?? new MediaDictionary();
            fixed (AVDictionary** pOpts = &tmp.pDictionary)
            {
                ffmpeg.avcodec_open2(output, codec, opts == null ? null : pOpts).ThrowIfError();
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
        public static MediaDecoder CreateDecoder(AVCodecParameters codecParameters, Action<MediaCodecContextBase> action = null, MediaDictionary opts = null)
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
        /// decode packet to get frame.
        /// TODO: add SubtitleFrame support
        /// <para>
        /// <see cref="SendPacket(MediaPacket)"/> and <see cref="ReceiveFrame(MediaFrame)"/>
        /// </para>
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="inFrame"></param>
        /// <param name="swFrame">av_hwframe_transfer_data dst</param>
        /// <param name="flags">av_hwframe_transfer_data flags</param>
        /// <returns></returns>
        public IEnumerable<MediaFrame> DecodePacket(MediaPacket packet, MediaFrame inFrame = null, MediaFrame swFrame = null, int flags = 0)
        {
            var isHWDeviceCtxInit = IsHWDeviceCtxInit();
            int ret = SendPacket(packet);
            if (ret < 0 && ret != ffmpeg.AVERROR(ffmpeg.EAGAIN) && ret != ffmpeg.AVERROR_EOF)
                ret.ThrowIfError();
            MediaFrame _frame = inFrame ?? new MediaFrame();
            MediaFrame _swframe = swFrame == null ? (isHWDeviceCtxInit ? new MediaFrame() : null) : swFrame;
            try
            {
                while (true)
                {
                    ret = ReceiveFrame(_frame);
                    if (ret < 0)
                    {
                        // those two return values are special and mean there is no output
                        // frame available, but there were no errors during decoding
                        if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
                            yield break;
                        else
                            break;
                    }
                    if (isHWDeviceCtxInit)
                    {
                        _frame.CopyProps(_swframe);
                        HWFrameTransferData(_swframe, _frame, flags);
                        yield return _swframe;
                    }
                    else
                    {
                        yield return _frame;
                    }
                }
            }
            finally
            {
                if (inFrame == null) _frame.Dispose();
                if (swFrame == null) _swframe?.Dispose();
            }
        }
    }
}
