using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;

namespace EmguFFmpeg
{
    public abstract class FrameConverter : IDisposable
    {
        public virtual MediaFrame Convert(MediaFrame frame)
        {
            throw new FFmpegException(new NotImplementedException());
        }

        #region IDisposable Support

        protected abstract void Dispose(bool disposing);

        ~FrameConverter()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    public abstract class FrameConverter<T> : FrameConverter where T : MediaFrame
    {
        protected T dstFrame;

        public new abstract T Convert(MediaFrame frame);
    }

    public unsafe class VideoFrameConverter : FrameConverter<VideoFrame>
    {
        protected SwsContext* pSwsContext = null;
        public readonly AVPixelFormat DstFormat;
        public readonly int DstWidth;
        public readonly int DstHeight;
        public readonly int SwsFlag;

        public static VideoFrameConverter CreateConverter(MediaCodec dst, int flag = ffmpeg.SWS_BILINEAR)
        {
            if (dst.AVCodecContext.codec_type != AVMediaType.AVMEDIA_TYPE_VIDEO)
                throw new FFmpegException(new ArgumentException());
            return new VideoFrameConverter(dst.AVCodecContext.pix_fmt, dst.AVCodecContext.width, dst.AVCodecContext.height, flag);
        }

        public VideoFrameConverter(AVPixelFormat srcFormat, int srcWidth, int srcHeight, AVPixelFormat dstFormat, int dstWidth, int dstHeight, int flag = ffmpeg.SWS_BILINEAR)
            : this(dstFormat, dstWidth, dstHeight, flag)
        {
            pSwsContext = ffmpeg.sws_getContext(
                srcWidth, srcHeight, srcFormat,
                dstWidth, dstHeight, dstFormat,
                flag, null, null, null);
        }

        public VideoFrameConverter(AVPixelFormat dstFormat, int dstWidth, int dstHeight, int flag = ffmpeg.SWS_BILINEAR)
        {
            DstWidth = dstWidth;
            DstHeight = dstHeight;
            DstFormat = dstFormat;
            SwsFlag = flag;
            dstFrame = new VideoFrame(dstFormat, dstWidth, dstHeight);
        }

        public VideoFrameConverter(VideoFrame dst, int flag = ffmpeg.SWS_BILINEAR)
        {
            DstWidth = dst.AVFrame.width;
            DstHeight = dst.AVFrame.height;
            DstFormat = (AVPixelFormat)dst.AVFrame.format;
            SwsFlag = flag;
            dstFrame = dst;
        }

        public static implicit operator SwsContext*(VideoFrameConverter value)
        {
            return value.pSwsContext;
        }

        public override VideoFrame Convert(MediaFrame srcFrame)
        {
            AVFrame* src = srcFrame;
            AVFrame* dst = dstFrame;
            if (pSwsContext == null && !disposedValue)
            {
                pSwsContext = ffmpeg.sws_getContext(
                    src->width, src->height, (AVPixelFormat)src->format,
                    DstWidth, DstHeight, DstFormat, SwsFlag, null, null, null);
            }
            ffmpeg.sws_scale(pSwsContext, src->data, src->linesize, 0, src->height, dst->data, dst->linesize).ThrowExceptionIfError();
            return dstFrame as VideoFrame;
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                ffmpeg.sws_freeContext(pSwsContext);

                disposedValue = true;
            }
        }

        #endregion
    }

    public unsafe class AudioFrameConverter : FrameConverter<AudioFrame>
    {
        protected SwrContext* pSwrContext = null;
        public readonly AVSampleFormat DstFormat;
        public readonly ulong DstChannelLayout;
        public readonly int DstSampleRate;

        public static AudioFrameConverter CreateConverter(MediaCodec dst)
        {
            if (dst.Type != AVMediaType.AVMEDIA_TYPE_AUDIO)
                throw new FFmpegException(new ArgumentException());
            return new AudioFrameConverter(dst.AVCodecContext.sample_fmt, dst.AVCodecContext.channel_layout, dst.AVCodecContext.sample_rate);
        }

        public AudioFrameConverter(AVSampleFormat srcFormat, ulong srcChannelLayout, int srcSampleRate, AVSampleFormat dstFormat, ulong dstChannelLayout, int dstSampleRate)
            : this(dstFormat, dstChannelLayout, dstSampleRate)
        {
            ffmpeg.swr_alloc();
            pSwrContext = ffmpeg.swr_alloc_set_opts(null,
                (long)dstChannelLayout, dstFormat, dstSampleRate,
                (long)srcChannelLayout, srcFormat, srcSampleRate,
                0, null);
            ffmpeg.swr_init(pSwrContext);
        }

        public AudioFrameConverter(AVSampleFormat dstFormat, ulong dstChannelLayout, int dstSampleRate)
        {
            DstChannelLayout = dstChannelLayout;
            DstFormat = dstFormat;
            DstSampleRate = dstSampleRate;
            dstFrame = new AudioFrame();
        }

        public AudioFrameConverter(AudioFrame dst)
        {
            DstChannelLayout = dst.AVFrame.channel_layout;
            DstFormat = (AVSampleFormat)dst.AVFrame.format;
            DstSampleRate = dst.AVFrame.sample_rate;
            dstFrame = dst;
        }

        public static implicit operator SwrContext*(AudioFrameConverter value)
        {
            return value.pSwrContext;
        }

        /// <summary>
        /// TODO fix test
        /// </summary>
        /// <param name="srcFrame"></param>
        /// <returns></returns>
        public override AudioFrame Convert(MediaFrame srcFrame)
        {
            AVFrame* src = srcFrame;
            AVFrame* dst = dstFrame;
            if (dst->data[0] == null)
            {
                dst->format = (int)DstFormat;
                dst->channel_layout = DstChannelLayout;
                dst->sample_rate = DstSampleRate;
                dst->nb_samples = src->nb_samples; //src->linesize[0] / src->channels;
                ffmpeg.av_frame_get_buffer(dst, 0);
            }
            if (pSwrContext == null && !disposedValue)
            {
                pSwrContext = ffmpeg.swr_alloc_set_opts(null,
                    (long)DstChannelLayout, DstFormat, DstSampleRate,
                    (long)src->channel_layout, (AVSampleFormat)src->format, src->sample_rate,
                    0, null);
                ffmpeg.swr_init(pSwrContext).ThrowExceptionIfError();
            }
            ffmpeg.swr_convert(pSwrContext, dst->extended_data, dst->nb_samples, src->extended_data, src->nb_samples);
            return dstFrame;
        }

        public IEnumerator<AudioFrame> Convert(MediaFrame srcFrame, int dstSample)
        {
            throw new FFmpegException(new NotImplementedException());
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                fixed (SwrContext** ppSwrContext = &pSwrContext)
                {
                    ffmpeg.swr_free(ppSwrContext);
                }

                disposedValue = true;
            }
        }

        #endregion
    }
}