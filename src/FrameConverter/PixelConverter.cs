using FFmpeg.AutoGen;

using System.Collections.Generic;

namespace EmguFFmpeg
{
    /// <summary>
    /// <see cref="SwsContext"/> wapper
    /// </summary>
    public unsafe class PixelConverter : FrameConverter<VideoFrame>
    {
        private SwsContext* pSwsContext = null;
        public readonly AVPixelFormat DstFormat;
        public readonly int DstWidth;
        public readonly int DstHeight;
        public readonly int SwsFlag;

        /// <summary>
        /// create video frame converter by dst output parames
        /// </summary>
        /// <param name="dstFormat"></param>
        /// <param name="dstWidth"></param>
        /// <param name="dstHeight"></param>
        /// <param name="flag"></param>
        public PixelConverter(AVPixelFormat dstFormat, int dstWidth, int dstHeight, int flag = ffmpeg.SWS_BILINEAR)
        {
            DstWidth = dstWidth;
            DstHeight = dstHeight;
            DstFormat = dstFormat;
            SwsFlag = flag;
            dstFrame = new VideoFrame(DstWidth, DstHeight, DstFormat);
        }

        /// <summary>
        /// create video frame converter by dst codec
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
            dstFrame = new VideoFrame(DstWidth, DstHeight, DstFormat);
        }

        /// <summary>
        /// create video fram converter by dst frame
        /// </summary>
        /// <param name="dstFrame"></param>
        /// <param name="flag"></param>
        public PixelConverter(VideoFrame dstFrame, int flag = ffmpeg.SWS_BILINEAR)
        {
            ffmpeg.av_frame_make_writable(dstFrame).ThrowExceptionIfError();
            DstWidth = dstFrame.AVFrame.width;
            DstHeight = dstFrame.AVFrame.height;
            DstFormat = (AVPixelFormat)dstFrame.AVFrame.format;
            SwsFlag = flag;
            base.dstFrame = dstFrame;
        }


        /// <summary>
        /// Convert <paramref name="srcframe"/>
        /// <para>
        /// Video conversion can be made without the use of IEnumerable,
        /// here In order to be consistent with the <see cref="SampleConverter"/> interface.
        /// </para>
        /// </summary>
        /// <param name="srcframe"></param>
        /// <returns></returns>
        public override IEnumerable<VideoFrame> Convert(MediaFrame srcframe)
        {
            yield return ConvertFrame(srcframe);
        }

        public VideoFrame ConvertFrame(MediaFrame srcFrame)
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

        public static implicit operator SwsContext*(PixelConverter value)
        {
            return value.pSwsContext;
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                isDisposing = true;
                ffmpeg.sws_freeContext(pSwsContext);
                disposedValue = true;
            }
        }

        #endregion
    }
}
