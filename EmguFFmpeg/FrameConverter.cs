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

    /// <summary>
    /// 图像转码器
    /// </summary>
    public unsafe class VideoFrameConverter : FrameConverter<VideoFrame>
    {
        protected SwsContext* pSwsContext = null;
        public readonly AVPixelFormat DstFormat;
        public readonly int DstWidth;
        public readonly int DstHeight;
        private int flag;

        public static VideoFrameConverter CreateConverter(MediaCodec dstEncode, int flag = ffmpeg.SWS_BILINEAR)
        {
            if (dstEncode as MediaEncode == null || dstEncode.AVCodecContext.codec_type != AVMediaType.AVMEDIA_TYPE_VIDEO)
                throw new FFmpegException(new ArgumentException());
            return new VideoFrameConverter(dstEncode.AVCodecContext.pix_fmt, dstEncode.AVCodecContext.width, dstEncode.AVCodecContext.height, flag);
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
            this.DstWidth = dstWidth;
            this.DstHeight = dstHeight;
            this.DstFormat = dstFormat;
            this.flag = flag;
            dstFrame = new VideoFrame(dstFormat, dstWidth, dstHeight);
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
                    DstWidth, DstHeight, DstFormat, flag, null, null, null);
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
                dstFrame.Dispose();
                ffmpeg.sws_freeContext(pSwsContext);

                disposedValue = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// 音频转码器
    /// </summary>
    public unsafe class AudioFrameConverter : FrameConverter<AudioFrame>
    {
        protected SwrContext* pSwrContext = null;
        public readonly AVSampleFormat DstFormat;
        public readonly ulong DstChannelLayout;
        public int DstSampleRate;

        public static AudioFrameConverter CreateConverter(MediaCodec dstEncode)
        {
            if (dstEncode as MediaEncode == null || dstEncode.Type != AVMediaType.AVMEDIA_TYPE_AUDIO)
                throw new FFmpegException(new ArgumentException());
            return new AudioFrameConverter(dstEncode.AVCodecContext.sample_fmt, dstEncode.AVCodecContext.channel_layout, dstEncode.AVCodecContext.sample_rate);
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
            this.DstChannelLayout = dstChannelLayout;
            this.DstFormat = dstFormat;
            this.DstSampleRate = dstSampleRate;
            dstFrame = new AudioFrame();
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
                dstFrame.Dispose();
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