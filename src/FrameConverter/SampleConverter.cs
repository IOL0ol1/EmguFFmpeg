using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace FFmpegSharp
{
    /// <summary>
    /// <see cref="SwrContext"/> wapper, include a <see cref="AVAudioFifo"/>.
    /// </summary>
    public unsafe class SampleConverter : IFrameConverter, IDisposable
    {
        private bool disposedValue;
        protected SwrContext* pSwrContext = null;
        public readonly AudioFifo AudioFifo; // TODO: maybe remove

        public SampleConverter(SwrContext* pSwrContext, bool isDisposeByOwner = true)
        {
            this.pSwrContext = pSwrContext;
            disposedValue = !isDisposeByOwner;
        }

        public SampleConverter() : this(ffmpeg.swr_alloc())
        { }

        #region safe wapper for IEnumerable

        private void SwrCheckInit(MediaFrame srcFrame, MediaFrame dstframe)
        {
            var output = dstframe;//?? MediaFrame.CreateAudioFrame(DstChLayout, DstNbSamples, DstFormat, DstSampleRate);
            AVChannelLayout inChLayout;
            ffmpeg.av_opt_get_chlayout(pSwrContext, "in_ch_layout", 0, &inChLayout).ThrowIfError();
            AVChannelLayout outChLayout;
            ffmpeg.av_opt_get_chlayout(pSwrContext, "out_ch_layout", 0, &outChLayout).ThrowIfError();
            long inSampleRate;
            ffmpeg.av_opt_get_int(pSwrContext, "in_sample_rate", 0, &inSampleRate).ThrowIfError();
            long outSampleRate;
            ffmpeg.av_opt_get_int(pSwrContext, "out_sample_rate", 0, &outSampleRate).ThrowIfError();
            AVSampleFormat outFmt;
            ffmpeg.av_opt_get_sample_fmt(pSwrContext, "out_sample_fmt", 0, &outFmt).ThrowIfError();
            AVSampleFormat inFmt;
            ffmpeg.av_opt_get_sample_fmt(pSwrContext, "in_sample_fmt", 0, &inFmt).ThrowIfError();
            if (ffmpeg.swr_is_initialized(pSwrContext) == 0
                || inChLayout.IsContentEqual(srcFrame.ChLayout)
                || inSampleRate != srcFrame.SampleRate
                || (int)inFmt != srcFrame.Format
                || outChLayout.IsContentEqual(dstframe.ChLayout)
                || outSampleRate != dstframe.SampleRate
                || (int)outFmt != dstframe.Format)
            {
                AVFrame* src = srcFrame;
                AVFrame* dst = output;

                fixed (SwrContext** ppSwrContext = &pSwrContext)
                {
                    ffmpeg.swr_alloc_set_opts2(ppSwrContext, &outChLayout, outFmt, (int)outSampleRate,
                        &src->ch_layout, (AVSampleFormat)src->format, src->sample_rate, 0, null).ThrowIfError();
                }
                ffmpeg.swr_init(pSwrContext).ThrowIfError();
            }
        }

        private int FifoPush(MediaFrame srcFrame, MediaFrame dstFrame)
        {
            AVFrame* src = srcFrame;
            AVFrame* dst = dstFrame;
            var outNbSamples = dstFrame.NbSamples;
            for (int i = 0, ret = outNbSamples; ret == outNbSamples && src != null; i++)
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
            AudioFifo.Read((void**)dst->extended_data, dst->nb_samples);
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
        /// <param name="dstFrame"></param>
        /// <returns></returns>
        public IEnumerable<MediaFrame> Convert(MediaFrame srcFrame, MediaFrame dstFrame)
        {
            SwrCheckInit(srcFrame, dstFrame);
            FifoPush(srcFrame, dstFrame);
            while (AudioFifo.Size >= dstFrame.NbSamples)
            {
                yield return FifoPop(dstFrame);
            }
        }

        /// <summary>
        /// convert input audio frame to output frame
        /// </summary>
        /// <param name="srcFrame">input audio frame</param>
        /// <param name="dstFrame"></param>
        /// <param name="outSamples">number of samples actually output</param>
        /// <param name="cacheSamples">number of samples in the internal cache</param>
        /// <returns></returns>
        public MediaFrame ConvertFrame(MediaFrame srcFrame, MediaFrame dstFrame, out int outSamples, out int cacheSamples)
        {
            SwrCheckInit(srcFrame, dstFrame);
            int curSamples = FifoPush(srcFrame, dstFrame);
            var dstframe = FifoPop(dstFrame);
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
