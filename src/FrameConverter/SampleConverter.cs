using FFmpeg.AutoGen;

using System.Collections.Generic;

namespace EmguFFmpeg
{
    /// <summary>
    /// <see cref="SwrContext"/> wapper, include a <see cref="AVAudioFifo"/>.
    /// </summary>
    public unsafe class SampleConverter : FrameConverter<AudioFrame>
    {
        private SwrContext* pSwrContext = null;
        public readonly AudioFifo AudioFifo;
        public readonly AVSampleFormat DstFormat;
        public readonly AVChannelLayout DstChannelLayout;
        //public readonly int DstChannels;
        public readonly int DstNbSamples;
        public readonly int DstSampleRate;

        /// <summary>
        /// create audio converter by dst output parames
        /// </summary>
        /// <param name="dstFormat"></param>
        /// <param name="dstChannelLayout">see <see cref="AVChannelLayout"/></param>
        /// <param name="dstNbSamples"></param>
        /// <param name="dstSampleRate"></param>
        public SampleConverter(AVSampleFormat dstFormat, AVChannelLayout dstChannelLayout, int dstNbSamples, int dstSampleRate)
        {
            DstFormat = dstFormat;
            DstChannelLayout = dstChannelLayout;
            DstNbSamples = dstNbSamples;
            DstSampleRate = dstSampleRate;
            dstFrame = new AudioFrame(DstChannelLayout.nb_channels, DstNbSamples, DstFormat, DstSampleRate);
            AudioFifo = new AudioFifo(DstFormat, DstChannelLayout.nb_channels, 1);
        }

        /// <summary>
        /// create audio converter by dst output parames
        /// </summary>
        /// <param name="dstFormat"></param>
        /// <param name="dstChannels"></param>
        /// <param name="dstNbSamples"></param>
        /// <param name="dstSampleRate"></param>
        public SampleConverter(AVSampleFormat dstFormat, int dstChannels, int dstNbSamples, int dstSampleRate)
        {
            DstFormat = dstFormat;
            DstChannelLayout = FFmpegHelper.GetChannelLayout(dstChannels);
            DstNbSamples = dstNbSamples;
            DstSampleRate = dstSampleRate;
            dstFrame = new AudioFrame(DstChannelLayout.nb_channels, DstNbSamples, DstFormat, DstSampleRate);
            AudioFifo = new AudioFifo(DstFormat, DstChannelLayout.nb_channels);
        }

        /// <summary>
        /// create audio converter by dst codec
        /// </summary>
        /// <param name="dstCodec"></param>
        public SampleConverter(MediaCodec dstCodec)
        {
            if (dstCodec.Type != AVMediaType.AVMEDIA_TYPE_AUDIO)
                throw new FFmpegException(FFmpegException.CodecTypeError);
            DstFormat = dstCodec.AVCodecContext.sample_fmt;
            DstChannelLayout = dstCodec.AVCodecContext.ch_layout;
            DstNbSamples = dstCodec.AVCodecContext.frame_size;
            DstSampleRate = dstCodec.AVCodecContext.sample_rate;
            dstFrame = new AudioFrame(DstChannelLayout.nb_channels, DstNbSamples, DstFormat, DstSampleRate);
            AudioFifo = new AudioFifo(DstFormat, DstChannelLayout.nb_channels);
        }

        /// <summary>
        /// create audio converter by dst frame
        /// </summary>
        /// <param name="dstFrame"></param>
        public SampleConverter(AudioFrame dstFrame)
        {
            ffmpeg.av_frame_make_writable(dstFrame).ThrowIfError();
            DstFormat = (AVSampleFormat)dstFrame.AVFrame.format;
            DstChannelLayout = dstFrame.AVFrame.ch_layout;
            DstNbSamples = dstFrame.AVFrame.nb_samples;
            DstSampleRate = dstFrame.AVFrame.sample_rate;
            base.dstFrame = dstFrame;
            AudioFifo = new AudioFifo(DstFormat, DstChannelLayout.nb_channels);
        }


        #region  safe wapper for IEnumerable

        private void SwrCheckInit(MediaFrame srcFrame)
        {
            if (pSwrContext == null && !isDisposing)
            {
                AVFrame* src = srcFrame;
                AVFrame* dst = dstFrame;
                fixed (AVChannelLayout* pDstChannelLayout = &DstChannelLayout)
                fixed (SwrContext** ppSwrContext = &pSwrContext)
                {
                    ffmpeg.swr_alloc_set_opts2(ppSwrContext,
                         pDstChannelLayout, DstFormat, DstSampleRate == 0 ? src->sample_rate : DstSampleRate,
                         &src->ch_layout, (AVSampleFormat)src->format, src->sample_rate,
                        0, null).ThrowIfError();
                }
                ffmpeg.swr_init(pSwrContext).ThrowIfError();
            }
        }

        private int FifoPush(MediaFrame srcFrame)
        {
            AVFrame* src = srcFrame;
            AVFrame* dst = dstFrame;
            for (int i = 0, ret = DstNbSamples; ret == DstNbSamples && src != null; i++)
            {
                if (i == 0 && src != null)
                    ret = ffmpeg.swr_convert(pSwrContext, dst->extended_data, dst->nb_samples, src->extended_data, src->nb_samples).ThrowIfError();
                else
                    ret = ffmpeg.swr_convert(pSwrContext, dst->extended_data, dst->nb_samples, null, 0).ThrowIfError();
                AudioFifo.Add((void**)dst->extended_data, ret);
            }
            return AudioFifo.Size;
        }

        private AudioFrame FifoPop()
        {
            AVFrame* dst = dstFrame;
            AudioFifo.Read((void**)dst->extended_data, DstNbSamples);
            return dstFrame as AudioFrame;
        }

        #endregion

        /// <summary>
        /// Convert <paramref name="srcFrame"/>.
        /// <para>
        /// sometimes audio inputs and outputs are used at different
        /// frequencies and need to be resampled using fifo, 
        /// so use <see cref="IEnumerable{T}"/>.
        /// </para>
        /// </summary>
        /// <param name="srcFrame"></param>
        /// <returns></returns>
        public override IEnumerable<AudioFrame> Convert(MediaFrame srcFrame)
        {
            SwrCheckInit(srcFrame);
            FifoPush(srcFrame);
            while (AudioFifo.Size >= DstNbSamples)
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
            cacheSamples = AudioFifo.Size;
            outSamples = curSamples - cacheSamples;
            return dstframe;
        }

        public static implicit operator SwrContext*(SampleConverter value)
        {
            return value.pSwrContext;
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
                AudioFifo.Dispose();

                disposedValue = true;
            }
        }

        #endregion
    }
}
