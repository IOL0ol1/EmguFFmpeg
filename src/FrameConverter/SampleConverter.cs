using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    /// <summary>
    /// <see cref="SwrContext"/> wapper, include a <see cref="AVAudioFifo"/>.
    /// </summary>
    public unsafe class SampleConverter : IFrameConverter, IDisposable
    {
        private MediaFrame dstFrame;
        private bool disposedValue;
        protected SwrContext* pSwrContext = null;
        public readonly AudioFifo AudioFifo;
        public readonly AVSampleFormat DstFormat;
        public readonly AVChannelLayout DstChLayout;
        public readonly int DstNbSamples;
        public readonly int DstSampleRate;

        public SampleConverter(SwrContext* pSwrContext, AVSampleFormat dstFormat, AVChannelLayout dstChLayout, int dstNbSamples, int dstSampleRate, bool isDisposeByOwner = true)
            : this(dstFormat, dstChLayout, dstNbSamples, dstSampleRate)
        {
            this.pSwrContext = pSwrContext;
            disposedValue = !isDisposeByOwner;
        }

        public SampleConverter(SwrContext* pSwrContext, AVSampleFormat dstFormat, int dstChannels, int dstNbSamples, int dstSampleRate, bool isDisposeByOwner = true)
            : this(pSwrContext, dstFormat, AVChannelLayoutExtension.Default(dstChannels), dstNbSamples, dstSampleRate, isDisposeByOwner)
        { }


        /// <summary>
        /// create audio converter by dst parames
        /// </summary>
        /// <param name="dstFormat"></param>
        /// <param name="dstChLayout">see <see cref="AVChannelLayout"/></param>
        /// <param name="dstNbSamples"></param>
        /// <param name="dstSampleRate"></param>
        public SampleConverter(AVSampleFormat dstFormat, AVChannelLayout dstChLayout, int dstNbSamples, int dstSampleRate)
        {
            DstFormat = dstFormat;
            DstChLayout = dstChLayout;
            DstNbSamples = dstNbSamples;
            DstSampleRate = dstSampleRate;
            AudioFifo = new AudioFifo(DstFormat, dstChLayout.nb_channels, 1);
        }
        public SampleConverter(MediaFrame dstFrame)
            : this((AVSampleFormat)dstFrame.Format, dstFrame.ChLayout, dstFrame.NbSamples, dstFrame.SampleRate)
        {
            this.dstFrame = dstFrame;
        }


        /// <summary>
        /// create audio converter by dst output parames
        /// </summary>
        /// <param name="dstFormat"></param>
        /// <param name="dstChannels"></param>
        /// <param name="dstNbSamples"></param>
        /// <param name="dstSampleRate"></param>
        public SampleConverter(AVSampleFormat dstFormat, int dstChannels, int dstNbSamples, int dstSampleRate)
            : this(dstFormat, AVChannelLayoutExtension.Default(dstChannels), dstNbSamples, dstSampleRate)
        { }

        /// <summary>
        /// create audio converter by dst codec
        /// </summary>
        /// <param name="dstCodec"></param>
        public static SampleConverter CreateByCodeContext(MediaCodecContext dstCodec)
        {
            if (dstCodec.Context.codec_type != AVMediaType.AVMEDIA_TYPE_AUDIO)
                throw new FFmpegException(ffmpeg.AVERROR_INVALIDDATA);
            return new SampleConverter(dstCodec.SampleFmt, dstCodec.ChLayout, dstCodec.FrameSize, dstCodec.SampleRate);
        }

        #region safe wapper for IEnumerable

        private MediaFrame SwrCheckInit(MediaFrame srcFrame)
        {
            var output = dstFrame ?? MediaFrame.CreateAudioFrame(DstChLayout, DstNbSamples, DstFormat, DstSampleRate);
            if (pSwrContext == null)
            {
                AVFrame* src = srcFrame;
                AVFrame* dst = output;

                fixed (AVChannelLayout* pDst = &DstChLayout)
                fixed (SwrContext** ppSwrContext = &pSwrContext)
                {
                    ffmpeg.swr_alloc_set_opts2(ppSwrContext, pDst, DstFormat, DstSampleRate, &src->ch_layout, (AVSampleFormat)src->format, src->sample_rate, 0, null).ThrowIfError();
                }
                ffmpeg.swr_init(pSwrContext).ThrowIfError();
            }
            return output;
        }

        private int FifoPush(MediaFrame srcFrame, MediaFrame dstFrame)
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

        private MediaFrame FifoPop(MediaFrame dstFrame)
        {
            AVFrame* dst = dstFrame;
            AudioFifo.Read((void**)dst->extended_data, DstNbSamples);
            return dstFrame;
        }

        #endregion safe wapper for IEnumerable

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
        public IEnumerable<MediaFrame> Convert(MediaFrame srcFrame)
        {
            var tmpFrame = SwrCheckInit(srcFrame);
            FifoPush(srcFrame, tmpFrame);
            while (AudioFifo.Size >= DstNbSamples)
            {
                yield return FifoPop(tmpFrame);
            }
        }

        /// <summary>
        /// convert input audio frame to output frame
        /// </summary>
        /// <param name="srcFrame">input audio frame</param>
        /// <param name="outSamples">number of samples actually output</param>
        /// <param name="cacheSamples">number of samples in the internal cache</param>
        /// <returns></returns>
        public MediaFrame ConvertFrame(MediaFrame srcFrame, out int outSamples, out int cacheSamples)
        {
            var tmpFrame = SwrCheckInit(srcFrame);
            int curSamples = FifoPush(srcFrame,tmpFrame);
            var dstframe = FifoPop(tmpFrame);
            cacheSamples = AudioFifo.Size;
            outSamples = curSamples - cacheSamples;
            return dstframe;
        }

        public static implicit operator SwrContext*(SampleConverter value)
        {
            if (value is null) return null;
            return value.pSwrContext;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                fixed (SwrContext** ppSwrContext = &pSwrContext)
                {
                    ffmpeg.swr_free(ppSwrContext);
                }
                AudioFifo.Dispose();
                disposedValue = true;
            }
        }

        ~SampleConverter()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
