using FFmpeg.AutoGen;

using System.Collections.Generic;

namespace EmguFFmpeg
{
    /// <summary>
    /// <see cref="SwsContext"/> wapper
    /// </summary>
    public class PixelConverter : FrameConverter<VideoFrame>
    {
        private unsafe SwsContext* pSwsContext = null;
        public readonly AVPixelFormat DstFormat;
        public readonly int DstWidth;
        public readonly int DstHeight;
        public readonly int SwsFlag;

        public PixelConverter(AVPixelFormat dstFormat, int dstWidth, int dstHeight, int flag = ffmpeg.SWS_BILINEAR)
        {
            DstWidth = dstWidth;
            DstHeight = dstHeight;
            DstFormat = dstFormat;
            SwsFlag = flag;
            dstFrame = new VideoFrame(DstFormat, DstWidth, DstHeight);
        }

        /// <summary>
        /// create video frame conbverter by dst codec
        /// </summary>
        /// <param name="dstCodec"></param>
        /// <param name="flag"></param>
        public PixelConverter(MediaCodec dstCodec, int flag = ffmpeg.SWS_BILINEAR)
        {
            if (dstCodec.Type != AVMediaType.AVMEDIA_TYPE_VIDEO)
                throw new FFmpegException(FFmpegException.CodecTypeError);
            DstWidth = dstCodec.AVCodecContext.width;
            DstHeight = dstCodec.AVCodecContext.height;
            DstFormat = dstCodec.AVCodecContext.pix_fmt;
            SwsFlag = flag;
            dstFrame = new VideoFrame(DstFormat, DstWidth, DstHeight);
        }

        public PixelConverter(VideoFrame dstFrame, int flag = ffmpeg.SWS_BILINEAR)
        {
            unsafe
            {
                ffmpeg.av_frame_make_writable(dstFrame).ThrowExceptionIfError();
                DstWidth = dstFrame.AVFrame.width;
                DstHeight = dstFrame.AVFrame.height;
                DstFormat = (AVPixelFormat)dstFrame.AVFrame.format;
                SwsFlag = flag;
                base.dstFrame = dstFrame;
            }
        }

        public unsafe static implicit operator SwsContext*(PixelConverter value)
        {
            return value.pSwsContext;
        }

        public override IEnumerable<VideoFrame> Convert(MediaFrame frame)
        {
            yield return ConvertFrame(frame);
        }

        public VideoFrame ConvertFrame(MediaFrame srcFrame)
        {
            unsafe
            {
                AVFrame* src = srcFrame;
                AVFrame* dst = dstFrame;
                if (pSwsContext == null && !isDisposing)
                {
                    pSwsContext = ffmpeg.sws_getContext(
                        src->width, src->height, (AVPixelFormat)src->format,
                        DstWidth, DstHeight, DstFormat, SwsFlag, null, null, null);
                }
                ffmpeg.sws_scale(pSwsContext, src->data, src->linesize, 0, src->height, dst->data, dst->linesize).ThrowExceptionIfError();
                return dstFrame as VideoFrame;
            }
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                isDisposing = true;
                unsafe
                {
                    ffmpeg.sws_freeContext(pSwsContext);
                }

                disposedValue = true;
            }
        }

        #endregion
    }
}