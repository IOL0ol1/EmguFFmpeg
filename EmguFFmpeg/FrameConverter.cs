using FFmpeg.AutoGen;

using System;
using System.Collections;
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
        protected bool isDisposing = false;

        protected T dstFrame;

        public new abstract T Convert(MediaFrame frame);
    }

    public unsafe class VideoFrameConverter : FrameConverter<VideoFrame>
    {
        private SwsContext* pSwsContext = null;
        public readonly AVPixelFormat DstFormat;
        public readonly int DstWidth;
        public readonly int DstHeight;
        public readonly int SwsFlag;

        public VideoFrameConverter(AVPixelFormat dstFormat, int dstWidth, int dstHeight, int flag = ffmpeg.SWS_BILINEAR)
        {
            DstWidth = dstWidth;
            DstHeight = dstHeight;
            DstFormat = dstFormat;
            SwsFlag = flag;
            dstFrame = new VideoFrame(DstFormat, DstWidth, DstHeight);
        }

        public VideoFrameConverter(MediaCodec dstCodec, int flag = ffmpeg.SWS_BILINEAR)
        {
            if (dstCodec.Type != AVMediaType.AVMEDIA_TYPE_VIDEO)
                throw new FFmpegException(dstCodec.Type.ToString());
            DstWidth = dstCodec.AVCodecContext.width;
            DstHeight = dstCodec.AVCodecContext.height;
            DstFormat = dstCodec.AVCodecContext.pix_fmt;
            SwsFlag = flag;
            dstFrame = new VideoFrame(DstFormat, DstWidth, DstHeight);
        }

        public VideoFrameConverter(VideoFrame dstFrame, int flag = ffmpeg.SWS_BILINEAR)
        {
            DstWidth = dstFrame.AVFrame.width;
            DstHeight = dstFrame.AVFrame.height;
            DstFormat = (AVPixelFormat)dstFrame.AVFrame.format;
            SwsFlag = flag;
            base.dstFrame = dstFrame.Clone<VideoFrame>();
        }

        public static implicit operator SwsContext*(VideoFrameConverter value)
        {
            return value.pSwsContext;
        }

        public override VideoFrame Convert(MediaFrame srcFrame)
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
            return dstFrame;
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

    public unsafe class AudioFrameConverter : FrameConverter<AudioFrame>
    {
        private SwrContext* pSwrContext = null;
        public readonly AVSampleFormat DstFormat;
        public readonly ulong DstChannelLayout;
        public readonly int DstNbSamples;
        public readonly int DstSampleRate;

        public AudioFrameConverter(AVSampleFormat dstFormat, ulong dstChannelLayout, int dstNbSamples, int dstSampleRate)
        {
            DstFormat = dstFormat;
            DstChannelLayout = dstChannelLayout;
            DstNbSamples = dstNbSamples;
            DstSampleRate = dstSampleRate;
            dstFrame = new AudioFrame(DstFormat, (AVChannelLayout)DstChannelLayout, DstNbSamples, DstSampleRate);
        }

        public AudioFrameConverter(MediaCodec dstCodec)
        {
            if (dstCodec.Type != AVMediaType.AVMEDIA_TYPE_AUDIO)
                throw new FFmpegException(dstCodec.Type.ToString());
            DstFormat = dstCodec.AVCodecContext.sample_fmt;
            DstChannelLayout = dstCodec.AVCodecContext.channel_layout;
            DstNbSamples = dstCodec.AVCodecContext.frame_size;
            DstSampleRate = dstCodec.AVCodecContext.sample_rate;
            dstFrame = new AudioFrame(DstFormat, (AVChannelLayout)DstChannelLayout, DstNbSamples, DstSampleRate);
        }

        public AudioFrameConverter(AudioFrame dstFrame)
        {
            ffmpeg.av_frame_make_writable(dstFrame).ThrowExceptionIfError();
            DstFormat = (AVSampleFormat)dstFrame.AVFrame.format;
            DstChannelLayout = dstFrame.AVFrame.channel_layout;
            DstNbSamples = dstFrame.AVFrame.nb_samples;
            DstSampleRate = dstFrame.AVFrame.sample_rate;
            base.dstFrame = dstFrame;
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

            if (pSwrContext == null && !isDisposing)
            {
                pSwrContext = ffmpeg.swr_alloc_set_opts(null,
                    (long)DstChannelLayout, DstFormat, DstSampleRate == 0 ? src->sample_rate : DstSampleRate,
                    (long)src->channel_layout, (AVSampleFormat)src->format, src->sample_rate,
                    0, null);
                ffmpeg.swr_init(pSwrContext).ThrowExceptionIfError();
            }
            ffmpeg.swr_convert(pSwrContext, dst->extended_data, dst->nb_samples, src->extended_data, src->nb_samples).ThrowExceptionIfError();
            return dstFrame;
        }

        private AudioFifo audioFifo;

        private void InitFifo()
        {
            audioFifo = new AudioFifo(DstFormat, ffmpeg.av_get_channel_layout_nb_channels(DstChannelLayout), 1);
        }

        private void InitContext(MediaFrame srcFrame)
        {
            AVFrame* src = srcFrame;
            AVFrame* dst = dstFrame;
            if (pSwrContext == null && !isDisposing)
            {
                pSwrContext = ffmpeg.swr_alloc_set_opts(null,
                    (long)DstChannelLayout, DstFormat, DstSampleRate == 0 ? src->sample_rate : DstSampleRate,
                    (long)src->channel_layout, (AVSampleFormat)src->format, src->sample_rate,
                    0, null);
                ffmpeg.swr_init(pSwrContext).ThrowExceptionIfError();
            }
        }

        private int GetOutSamples(MediaFrame srcFrame)
        {
            AVFrame* src = srcFrame;
            AVFrame* dst = dstFrame;
            if (pSwrContext == null && !isDisposing)
            {
                pSwrContext = ffmpeg.swr_alloc_set_opts(null,
                    (long)DstChannelLayout, DstFormat, DstSampleRate == 0 ? src->sample_rate : DstSampleRate,
                    (long)src->channel_layout, (AVSampleFormat)src->format, src->sample_rate,
                    0, null);
                ffmpeg.swr_init(pSwrContext).ThrowExceptionIfError();
            }
            return ffmpeg.swr_get_out_samples(pSwrContext, src->nb_samples);
        }

        private int Convert2(MediaFrame srcFrame)
        {
            AVFrame* src = srcFrame;
            AVFrame* dst = dstFrame;
            if (srcFrame != null)
                return ffmpeg.swr_convert(pSwrContext, dst->extended_data, dst->nb_samples, src->extended_data, src->nb_samples).ThrowExceptionIfError();
            else
                return ffmpeg.swr_convert(pSwrContext, dst->extended_data, dst->nb_samples, null, 0).ThrowExceptionIfError();
        }

        private long outSamplesCount = 0;

        public IEnumerable<AudioFrame> Convert3(MediaFrame srcFrame)
        {
            outSamplesCount += GetOutSamples(srcFrame);
            for (int i = 0; i * DstNbSamples <= outSamplesCount; i++)
            {
                int out_samples = Convert2(i == 0 ? srcFrame : null);
                outSamplesCount -= out_samples;
                yield return dstFrame;
            }
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                isDisposing = true;
                fixed (SwrContext** ppSwrContext = &pSwrContext)
                {
                    ffmpeg.swr_free(ppSwrContext);
                }

                disposedValue = true;
            }
        }

        #endregion
    }

    public unsafe class AudioFifo : IDisposable
    {
        private AVAudioFifo* pAudioFifo;

        public AudioFifo(AVSampleFormat format, int channels, int nbSamples)
        {
            pAudioFifo = ffmpeg.av_audio_fifo_alloc(format, channels, 1);
            if (pAudioFifo == null)
                throw new FFmpegException(new NullReferenceException());
        }

        public int Size => ffmpeg.av_audio_fifo_size(pAudioFifo);

        public int Space => ffmpeg.av_audio_fifo_space(pAudioFifo);

        public int Peek(void** data, int nbSamples)
        {
            return ffmpeg.av_audio_fifo_peek(pAudioFifo, data, nbSamples).ThrowExceptionIfError();
        }

        public int PeekAt(void** data, int nbSamples, int Offset)
        {
            return ffmpeg.av_audio_fifo_peek_at(pAudioFifo, data, nbSamples, Offset).ThrowExceptionIfError();
        }

        public void Realloc(int nbSamples)
        {
            ffmpeg.av_audio_fifo_realloc(pAudioFifo, nbSamples).ThrowExceptionIfError();
        }

        public int Write(void** data, int nbSamples)
        {
            return ffmpeg.av_audio_fifo_write(pAudioFifo, data, nbSamples).ThrowExceptionIfError();
        }

        /// <summary>
        /// <see cref="Write(void**, int)"/> and auto <see cref="Realloc(int)"/> if <see cref="Space"/> less than <paramref name="nbSamples"/>
        /// </summary>
        /// <param name="data"></param>
        /// <param name="nbSamples"></param>
        /// <exception cref="FFmpegException"/>
        public int Add(void** data, int nbSamples)
        {
            if (Space < nbSamples)
                ffmpeg.av_audio_fifo_realloc(pAudioFifo, Size + nbSamples).ThrowExceptionIfError();
            return ffmpeg.av_audio_fifo_write(pAudioFifo, data, nbSamples).ThrowExceptionIfError();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <param name="nbSamples"></param>
        /// <exception cref="FFmpegException"/>
        /// <returns></returns>
        public int Read(void** data, int nbSamples)
        {
            return ffmpeg.av_audio_fifo_read(pAudioFifo, data, nbSamples).ThrowExceptionIfError();
        }

        /// <summary>
        /// Removes the data without reading it.
        /// </summary>
        /// <param name="nbSamples">number of samples to drain</param>
        /// <exception cref="FFmpegException"/>
        public void Drain(int nbSamples)
        {
            ffmpeg.av_audio_fifo_drain(pAudioFifo, nbSamples).ThrowExceptionIfError();
        }

        public void Clear()
        {
            ffmpeg.av_audio_fifo_reset(pAudioFifo);
        }

        #region IDisposable Support

        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                ffmpeg.av_audio_fifo_free(pAudioFifo);
                disposedValue = true;
            }
        }

        ~AudioFifo()
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
}