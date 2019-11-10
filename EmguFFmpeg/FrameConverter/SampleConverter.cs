using FFmpeg.AutoGen;
using System.Collections.Generic;

namespace EmguFFmpeg
{
    public unsafe class SampleConverter : FrameConverter<AudioFrame>
    {
        private AudioFifo audioFifo;
        private SwrContext* pSwrContext = null;
        public readonly AVSampleFormat DstFormat;
        public readonly ulong DstChannelLayout;
        public readonly int DstChannels;
        public readonly int DstNbSamples;
        public readonly int DstSampleRate;

        /// <summary>
        ///
        /// </summary>
        /// <param name="dstFormat"></param>
        /// <param name="dstChannelLayout">see <see cref="AVChannelLayout"/></param>
        /// <param name="dstNbSamples"></param>
        /// <param name="dstSampleRate"></param>
        public SampleConverter(AVSampleFormat dstFormat, ulong dstChannelLayout, int dstNbSamples, int dstSampleRate)
        {
            DstFormat = dstFormat;
            DstChannelLayout = dstChannelLayout;
            DstChannels = ffmpeg.av_get_channel_layout_nb_channels(dstChannelLayout);
            DstNbSamples = dstNbSamples;
            DstSampleRate = dstSampleRate;
            dstFrame = new AudioFrame(DstFormat, DstChannelLayout, DstNbSamples, DstSampleRate);
            audioFifo = new AudioFifo(DstFormat, ffmpeg.av_get_channel_layout_nb_channels(DstChannelLayout), 1);
        }

        public SampleConverter(AVSampleFormat dstFormat, int dstChannels, int dstNbSamples, int dstSampleRate)
        {
            DstFormat = dstFormat;
            DstChannels = dstChannels;
            DstChannelLayout = (ulong)ffmpeg.av_get_default_channel_layout(dstChannels);
            DstNbSamples = dstNbSamples;
            DstSampleRate = dstSampleRate;
            dstFrame = new AudioFrame(DstFormat, DstChannelLayout, DstNbSamples, DstSampleRate);
            audioFifo = new AudioFifo(DstFormat, ffmpeg.av_get_channel_layout_nb_channels(DstChannelLayout), 1);
        }

        public SampleConverter(MediaCodec dstCodec)
        {
            if (dstCodec.Type != AVMediaType.AVMEDIA_TYPE_AUDIO)
                throw new FFmpegException(FFmpegException.CodecTypeError);
            DstFormat = dstCodec.AVCodecContext.sample_fmt;
            DstChannelLayout = dstCodec.AVCodecContext.channel_layout;
            DstNbSamples = dstCodec.AVCodecContext.frame_size;
            DstSampleRate = dstCodec.AVCodecContext.sample_rate;
            dstFrame = new AudioFrame(DstFormat, DstChannelLayout, DstNbSamples, DstSampleRate);
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
            return dstFrame as AudioFrame;
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
}