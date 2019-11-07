using FFmpeg.AutoGen;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace EmguFFmpeg
{
    public abstract class FrameConverter : IDisposable
    {
        public virtual IEnumerable<MediaFrame> Convert(MediaFrame frame)
        {
            throw new FFmpegException(FFmpegMessage.NotImplemented);
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

        public new abstract IEnumerable<T> Convert(MediaFrame frame);
    }

    public unsafe class PixelConverter : FrameConverter<VideoFrame>
    {
        private SwsContext* pSwsContext = null;
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

        public PixelConverter(MediaCodec dstCodec, int flag = ffmpeg.SWS_BILINEAR)
        {
            if (dstCodec.Type != AVMediaType.AVMEDIA_TYPE_VIDEO)
                throw new FFmpegException(FFmpegMessage.CodecTypeError);
            DstWidth = dstCodec.AVCodecContext.width;
            DstHeight = dstCodec.AVCodecContext.height;
            DstFormat = dstCodec.AVCodecContext.pix_fmt;
            SwsFlag = flag;
            dstFrame = new VideoFrame(DstFormat, DstWidth, DstHeight);
        }

        public PixelConverter(VideoFrame dstFrame, int flag = ffmpeg.SWS_BILINEAR)
        {
            DstWidth = dstFrame.AVFrame.width;
            DstHeight = dstFrame.AVFrame.height;
            DstFormat = (AVPixelFormat)dstFrame.AVFrame.format;
            SwsFlag = flag;
            base.dstFrame = dstFrame;
        }

        public static implicit operator SwsContext*(PixelConverter value)
        {
            return value.pSwsContext;
        }

        public override IEnumerable<VideoFrame> Convert(MediaFrame frame)
        {
            yield return ConvertFrame(frame);
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

    public unsafe class SampleConverter : FrameConverter<AudioFrame>
    {
        private AudioFifo audioFifo;
        private SwrContext* pSwrContext = null;
        public readonly AVSampleFormat DstFormat;
        public readonly ulong DstChannelLayout;
        public readonly int DstNbSamples;
        public readonly int DstSampleRate;

        public SampleConverter(AVSampleFormat dstFormat, ulong dstChannelLayout, int dstNbSamples, int dstSampleRate)
        {
            DstFormat = dstFormat;
            DstChannelLayout = dstChannelLayout;
            DstNbSamples = dstNbSamples;
            DstSampleRate = dstSampleRate;
            dstFrame = new AudioFrame(DstFormat, (AVChannelLayout)DstChannelLayout, DstNbSamples, DstSampleRate);
            audioFifo = new AudioFifo(DstFormat, ffmpeg.av_get_channel_layout_nb_channels(DstChannelLayout), 1);
        }

        public SampleConverter(MediaCodec dstCodec)
        {
            if (dstCodec.Type != AVMediaType.AVMEDIA_TYPE_AUDIO)
                throw new FFmpegException(FFmpegMessage.CodecTypeError);
            DstFormat = dstCodec.AVCodecContext.sample_fmt;
            DstChannelLayout = dstCodec.AVCodecContext.channel_layout;
            DstNbSamples = dstCodec.AVCodecContext.frame_size;
            DstSampleRate = dstCodec.AVCodecContext.sample_rate;
            dstFrame = new AudioFrame(DstFormat, (AVChannelLayout)DstChannelLayout, DstNbSamples, DstSampleRate);
            audioFifo = new AudioFifo(DstFormat, ffmpeg.av_get_channel_layout_nb_channels(DstChannelLayout), 1);
        }

        public SampleConverter(AudioFrame dstFrame)
        {
            ffmpeg.av_frame_make_writable(dstFrame).ThrowExceptionIfError();
            DstFormat = (AVSampleFormat)dstFrame.AVFrame.format;
            DstChannelLayout = dstFrame.AVFrame.channel_layout;
            DstNbSamples = dstFrame.AVFrame.nb_samples;
            DstSampleRate = dstFrame.AVFrame.sample_rate;
            base.dstFrame = dstFrame;
            audioFifo = new AudioFifo(DstFormat, ffmpeg.av_get_channel_layout_nb_channels(DstChannelLayout), 1);
        }

        public static implicit operator SwrContext*(SampleConverter value)
        {
            return value.pSwrContext;
        }

        #region  safe wapper for IEnumerable

        private void SwrCheckInit(MediaFrame srcFrame)
        {
            if (pSwrContext == null && !isDisposing)
            {
                AVFrame* src = srcFrame;
                AVFrame* dst = dstFrame;
                pSwrContext = ffmpeg.swr_alloc_set_opts(null,
                    (long)DstChannelLayout, DstFormat, DstSampleRate == 0 ? src->sample_rate : DstSampleRate,
                    (long)src->channel_layout, (AVSampleFormat)src->format, src->sample_rate,
                    0, null);
                ffmpeg.swr_init(pSwrContext).ThrowExceptionIfError();
            }
        }

        private int FifoPush(MediaFrame srcFrame)
        {
            AVFrame* src = srcFrame;
            AVFrame* dst = dstFrame;
            for (int i = 0, ret = DstNbSamples; ret == DstNbSamples && src != null; i++)
            {
                if (i == 0 && src != null)
                    ret = ffmpeg.swr_convert(pSwrContext, dst->extended_data, dst->nb_samples, src->extended_data, src->nb_samples).ThrowExceptionIfError();
                else
                    ret = ffmpeg.swr_convert(pSwrContext, dst->extended_data, dst->nb_samples, null, 0).ThrowExceptionIfError();
                audioFifo.Add((void**)dst->extended_data, ret);
            }
            return audioFifo.Size;
        }

        private AudioFrame FifoPop()
        {
            AVFrame* dst = dstFrame;
            audioFifo.Read((void**)dst->extended_data, DstNbSamples);
            return dstFrame;
        }

        #endregion

        public override IEnumerable<AudioFrame> Convert(MediaFrame srcFrame)
        {
            SwrCheckInit(srcFrame);
            FifoPush(srcFrame);
            while (audioFifo.Size >= DstNbSamples)
            {
                yield return FifoPop();
            }
        }

        /// <summary>
        /// convert input audio frame to output frame
        /// </summary>
        /// <param name="srcFrame">input audio frame</param>
        /// <param name="outSamples">number of samples actually output</param>
        /// <param name="cacheSamples">number of samples in the internal cache</param>
        /// <returns></returns>
        public AudioFrame ConvertFrame(MediaFrame srcFrame, out int outSamples, out int cacheSamples)
        {
            SwrCheckInit(srcFrame);
            int curSamples = FifoPush(srcFrame);
            AudioFrame dstframe = FifoPop();
            cacheSamples = audioFifo.Size;
            outSamples = curSamples - cacheSamples;
            return dstframe;
        }

        public void ClearCache()
        {
            audioFifo.Clear();
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
                audioFifo.Dispose();

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
            pAudioFifo = ffmpeg.av_audio_fifo_alloc(format, channels, nbSamples <= 0 ? 1 : nbSamples);
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

        /// <summary>
        /// auto <see cref="Realloc(int)"/> if <see cref="Space"/> less than <paramref name="nbSamples"/>
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