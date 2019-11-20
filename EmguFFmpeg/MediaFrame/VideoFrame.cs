using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    public unsafe class VideoFrame : MediaFrame
    {
        public static VideoFrame CreateFrameByCodec(MediaCodec codec)
        {
            if (codec.Type != AVMediaType.AVMEDIA_TYPE_VIDEO)
                throw new FFmpegException(FFmpegException.CodecTypeError);
            return new VideoFrame(codec.AVCodecContext.pix_fmt, codec.AVCodecContext.width, codec.AVCodecContext.height);
        }

        public VideoFrame() : base()
        { }

        public VideoFrame(AVPixelFormat format, int width, int height, int align = 0) : base()
        {
            AllocBuffer(format, width, height, align);
        }

        private void AllocBuffer(AVPixelFormat format, int width, int height, int align = 0)
        {
            if (ffmpeg.av_frame_is_writable(pFrame) != 0)
                return;
            pFrame->format = (int)format;
            pFrame->width = width;
            pFrame->height = height;
            ffmpeg.av_frame_get_buffer(pFrame, align);
        }

        public void Init(AVPixelFormat format, int width, int height, int align = 0)
        {
            Clear();
            AllocBuffer(format, width, height, align);
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