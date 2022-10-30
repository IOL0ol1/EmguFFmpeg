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
        protected SwrContext* pSwrContext;
        protected AVChannelLayout dstChLayout;
        protected int dstSampleRate;
        protected AVSampleFormat dstFormat;
        protected int dstSamples;
        protected AudioFifo AudioFifo; 

        public SampleConverter(SwrContext* pSwrContext, bool isDisposeByOwner = true)
        {
            this.pSwrContext = pSwrContext;
            disposedValue = !isDisposeByOwner;
        }

        public SampleConverter() : this(ffmpeg.swr_alloc())
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dstChLayout"></param>
        /// <param name="dstSampleRate"></param>
        /// <param name="dstFormat"></param>
        /// <param name="dstSamplesMax">set 0 will use src frame's NbSamples</param>
        /// <returns></returns>
        public static SampleConverter Create(AVChannelLayout dstChLayout, int dstSampleRate, AVSampleFormat dstFormat, int dstSamplesMax)
        {
            var sampleConverter = new SampleConverter();
            sampleConverter.SetOpts(dstChLayout, dstSampleRate, dstFormat, dstSamplesMax);
            return sampleConverter;
        }


        public void SetOpts(AVChannelLayout dstChLayout, int dstSampleRate, AVSampleFormat dstFormat, int dstSamplesMax)
        {
            this.dstChLayout = dstChLayout;
            this.dstSampleRate = dstSampleRate;
            this.dstFormat = dstFormat;
            this.dstSamples = dstSamplesMax;
            AudioFifo = new AudioFifo(dstFormat, dstChLayout.nb_channels, dstSampleRate);
        }


        #region safe wapper for IEnumerable

        private void SwrCheckInit(MediaFrame srcFrame, AVChannelLayout dstChLayout, int dstSampleRate, AVSampleFormat dstFormat)
        {
            if (srcFrame == null) return;
            AVChannelLayout inChLayout;
            ffmpeg.av_opt_get_chlayout(pSwrContext, "ichl", 0, &inChLayout).ThrowIfError();
            AVChannelLayout outChLayout;
            ffmpeg.av_opt_get_chlayout(pSwrContext, "ochl", 0, &outChLayout).ThrowIfError();
            long inSampleRate;
            ffmpeg.av_opt_get_int(pSwrContext, "isr", 0, &inSampleRate).ThrowIfError();
            long outSampleRate;
            ffmpeg.av_opt_get_int(pSwrContext, "osr", 0, &outSampleRate).ThrowIfError();
            AVSampleFormat inFmt;
            ffmpeg.av_opt_get_sample_fmt(pSwrContext, "isf", 0, &inFmt).ThrowIfError();
            AVSampleFormat outFmt;
            ffmpeg.av_opt_get_sample_fmt(pSwrContext, "osf", 0, &outFmt).ThrowIfError();
            var srcChLayout = srcFrame.ChLayout;
            var srcSampleRate = srcFrame.SampleRate;
            var srcFormat = srcFrame.Format;
            if (!inChLayout.IsContentEqual(srcChLayout)
                || inSampleRate != srcSampleRate
                || (int)inFmt != srcFormat
                || !outChLayout.IsContentEqual(dstChLayout)
                || outSampleRate != dstSampleRate
                || outFmt != dstFormat) // need reset
            {
                AVFrame* src = srcFrame;
                fixed (SwrContext** ppSwrContext = &pSwrContext)
                {
                    ffmpeg.swr_alloc_set_opts2(ppSwrContext, &dstChLayout, dstFormat, dstSampleRate,
                        &srcChLayout, (AVSampleFormat)srcFormat, srcSampleRate, 0, null).ThrowIfError();
                }
                ffmpeg.swr_init(pSwrContext).ThrowIfError();
            }
            else
            {
                if (ffmpeg.swr_is_initialized(pSwrContext) == 0) // just init
                    ffmpeg.swr_init(pSwrContext).ThrowIfError();
            }
        }

        private int FifoPush(MediaFrame srcFrame, MediaFrame dstFrame)
        {
            AVFrame* dst = dstFrame;
            var outNbSamples = dstFrame.NbSamples;
            for (int i = 0, ret = outNbSamples; ret == outNbSamples; i++)
            {
                if (i == 0 && srcFrame != null)
                {
                    AVFrame* src = srcFrame;
                    ret = ffmpeg.swr_convert(pSwrContext, dst->extended_data, dst->nb_samples, src->extended_data, src->nb_samples).ThrowIfError();
                }
                else
                    ret = ffmpeg.swr_convert(pSwrContext, dst->extended_data, dst->nb_samples, null, 0).ThrowIfError();
                AudioFifo.Add((void**)dst->extended_data, ret);
            }
            return AudioFifo.Size;
        }

        private MediaFrame FifoPop(MediaFrame dstFrame)
        {
            AVFrame* dst = dstFrame;
            var samples = AudioFifo.Read((void**)dst->extended_data, dst->nb_samples);
            dst->nb_samples = samples;
            return dstFrame;
        }

        private int GetOutSamples(int inSamples)
        {
            return ffmpeg.swr_get_out_samples(pSwrContext, inSamples);
        }

        #endregion safe wapper for IEnumerable

        /// <summary>
        /// Convert <paramref name="srcframe"/>.
        /// <para>
        /// sometimes audio inputs and outputs are used at different
        /// frequencies and need to be resampled using fifo,
        /// so use <see cref="IEnumerable{T}"/>.
        /// </para>
        /// </summary>
        /// <param name="srcframe"></param>
        /// <param name="dstframe"></param>
        /// <returns></returns>
        public IEnumerable<MediaFrame> Convert(MediaFrame srcframe, MediaFrame dstframe = null)
        {
            var dst = dstframe == null ? new MediaFrame() : dstframe;
            if (!dst.IsWriteable())
            {
                dst.ChLayout = dstChLayout;
                dst.Format = (int)dstFormat;
                dst.SampleRate = dstSampleRate;
                dst.NbSamples = dstSamples;
                dst.AllocateBuffer();
            }
            srcframe?.CopyProps(dst);
            SwrCheckInit(srcframe, dstChLayout, dstSampleRate, dstFormat);
            FifoPush(srcframe, dst);
            while (AudioFifo.Size >= dst.NbSamples || (AudioFifo.Size > 0 && srcframe == null))
            {
                yield return FifoPop(dst);
            }
            if (dstframe == null) dst?.Dispose();
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
            var dstFrame = new MediaFrame();
            dstFrame.ChLayout = dstChLayout;
            dstFrame.Format = (int)dstFormat;
            dstFrame.SampleRate = dstSampleRate;
            dstFrame.NbSamples = dstSamples;
            dstFrame.AllocateBuffer();
            SwrCheckInit(srcFrame, dstChLayout, dstSampleRate, dstFormat);
            int curSamples = FifoPush(srcFrame, dstFrame);
            FifoPop(dstFrame);
            cacheSamples = AudioFifo.Size;
            outSamples = curSamples - cacheSamples;
            return dstFrame;
        }

        public static implicit operator SwrContext*(SampleConverter value)
        {
            if (value is null) return null;
            return value.pSwrContext;
        }

        #region 
        private bool disposedValue;

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
        #endregion
    }

    //public static partial class MediaFrameExtension
    //{
    //    public static MediaFrame Convert(this MediaFrame frame, AVChannelLayout dstChLayout, int dstSampleRate, AVSampleFormat dstFormat, int dstSamplesMax, out int outSamples, out int cacheSamples)
    //    {
    //        using (var p = new SampleConverter())
    //        {
    //            p.SetOpts(dstChLayout, dstSampleRate, dstFormat, dstSamplesMax);
    //            return p.ConvertFrame(frame, out outSamples, out cacheSamples);
    //        }
    //    }
    //}
}
