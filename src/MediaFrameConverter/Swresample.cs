using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    /// <summary>
    /// <see cref="SwrContext"/> wapper, include a <see cref="AVAudioFifo"/>.
    /// </summary>
    public unsafe class Swresample : IConverter, IDisposable
    {
        protected SwrContext* pSwrContext;

        public Swresample(SwrContext* pSwrContext, bool isDisposeByOwner = true)
        {
            this.pSwrContext = pSwrContext;
            disposedValue = !isDisposeByOwner;
        }

        public Swresample() : this(ffmpeg.swr_alloc())
        { }

        public Swresample(AVChannelLayout out_ch_layout, AVSampleFormat out_sample_fmt, int out_sample_rate,
                         AVChannelLayout in_ch_layout, AVSampleFormat in_sample_fmt, int in_sample_rate,
                        int log_offset = 0, void* log_ctx = null)
        {
            fixed (SwrContext** ppSwrContext = &pSwrContext)
            {
                ffmpeg.swr_alloc_set_opts2(ppSwrContext, &out_ch_layout, out_sample_fmt, out_sample_rate, &in_ch_layout, in_sample_fmt, in_sample_rate, log_offset, log_ctx).ThrowIfError();
            }
        }

        /// <summary>
        /// Convert <paramref name="srcFrame"/>.
        /// </summary>
        /// <param name="srcFrame"></param>
        /// <param name="dstFrame"></param>
        /// <returns></returns>
        public IEnumerable<MediaFrame> Convert(MediaFrame srcFrame, MediaFrame dstFrame)
        {
            ffmpeg.swr_convert_frame(pSwrContext, dstFrame, srcFrame).ThrowIfError();
            return new[] { dstFrame };
        }

        public static implicit operator SwrContext*(Swresample value)
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
                disposedValue = true;
            }
        }

        ~Swresample()
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
}
