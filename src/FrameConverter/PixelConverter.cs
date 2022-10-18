using System;
using System.Collections.Generic;

using FFmpeg.AutoGen;

namespace FFmpegSharp
{
    /// <summary>
    /// <see cref="SwsContext"/> wapper
    /// </summary>
    public unsafe class PixelConverter : IFrameConverter, IDisposable
    {
        protected SwsContext* pContext = null;
        private bool disposedValue;

        public PixelConverter(SwsContext* pSwsContext, bool isDisposeByOwner = true)
        {
            pContext = pSwsContext;
            disposedValue = !isDisposeByOwner;
        }

        public PixelConverter() : this(ffmpeg.sws_alloc_context())
        { }

        /// <summary>
        /// Convert <paramref name="srcframe"/>
        /// <para>
        /// Video conversion can be made without the use of IEnumerable,
        /// here In order to be consistent with the <see cref="SampleConverter"/> interface.
        /// </para>
        /// </summary>
        /// <param name="srcframe"></param>
        /// <param name="dstframe"></param>
        /// <returns></returns>
        public IEnumerable<MediaFrame> Convert(MediaFrame srcframe, MediaFrame dstframe) => Convert(srcframe, dstframe, ffmpeg.SWS_BICUBIC, null, null, null);
        public IEnumerable<MediaFrame> Convert(MediaFrame srcframe, int dstWidth, int dstHeight, AVPixelFormat dstPixFmt, int flags = ffmpeg.SWS_BICUBIC, SwsFilter* srcFilter = null, SwsFilter* dstFilter = null, double* param = null)
        {
            return Convert(srcframe, MediaFrame.CreateVideoFrame(dstWidth, dstHeight, dstPixFmt), flags, srcFilter, dstFilter, param);
        }
        public IEnumerable<MediaFrame> Convert(MediaFrame srcframe, MediaFrame dstframe, int flags = ffmpeg.SWS_BICUBIC, SwsFilter* srcFilter = null, SwsFilter* dstFilter = null, double* param = null)
        {
            return new[] { ConvertFrame(srcframe, dstframe, flags, srcFilter, dstFilter, param) };
        }

        public MediaFrame ConvertFrame(MediaFrame srcframe, MediaFrame dstframe, int flags = ffmpeg.SWS_BICUBIC, SwsFilter* srcFilter = null, SwsFilter* dstFilter = null, double* param = null)
        {
            AVFrame* src = srcframe;
            AVFrame* dst = dstframe;
            pContext = ffmpeg.sws_getCachedContext(pContext,
                src->width, src->height, (AVPixelFormat)src->format,
                dstframe.Width, dstframe.Height, (AVPixelFormat)dstframe.Format, flags, srcFilter, dstFilter, param);
            ffmpeg.sws_scale(pContext, src->data, src->linesize, 0, src->height, dst->data, dst->linesize).ThrowIfError();
            return dstframe;
        }

        public static implicit operator SwsContext*(PixelConverter value)
        {
            if (value is null) return null;
            return value.pContext;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                ffmpeg.sws_freeContext(pContext);
                disposedValue = true;
            }
        }

        ~PixelConverter()
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
