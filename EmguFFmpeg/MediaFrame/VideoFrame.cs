using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    public unsafe class VideoFrame : MediaFrame
    {
        /// <summary>
        /// create video frame by codec's parames
        /// </summary>
        /// <param name="codec"></param>
        /// <returns></returns>
        public static VideoFrame CreateFrameByCodec(MediaCodec codec)
        {
            if (codec.Type != AVMediaType.AVMEDIA_TYPE_VIDEO)
                throw new FFmpegException(FFmpegException.CodecTypeError);
            return new VideoFrame(codec.AVCodecContext.width, codec.AVCodecContext.height, codec.AVCodecContext.pix_fmt);
        }

        public VideoFrame() : base()
        { }

        public VideoFrame(int width, int height, AVPixelFormat format, int align = 0) : base()
        {
            AllocBuffer(width, height, format, align);
        }

        private void AllocBuffer(int width, int height, AVPixelFormat format, int align = 0)
        {
            if (ffmpeg.av_frame_is_writable(pFrame) != 0)
                return;
            pFrame->format = (int)format;
            pFrame->width = width;
            pFrame->height = height;
            ffmpeg.av_frame_get_buffer(pFrame, align);
        }

        public void Init(int width, int height, AVPixelFormat format, int align = 0)
        {
            Clear();
            AllocBuffer(width, height, format, align);
        }

        public override MediaFrame Copy()
        {
            VideoFrame dstFrame = new VideoFrame();
            AVFrame* dst = dstFrame;
            dst->format = pFrame->format;
            dst->width = pFrame->width;
            dst->height = pFrame->height;
            if (ffmpeg.av_frame_is_writable(pFrame) != 0)
            {
                ffmpeg.av_frame_get_buffer(dst, 0).ThrowExceptionIfError();
                ffmpeg.av_frame_copy(dst, pFrame).ThrowExceptionIfError();
            }
            ffmpeg.av_frame_copy_props(dst, pFrame).ThrowExceptionIfError();
            return dstFrame;
        }
    }
}
