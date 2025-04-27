using System;
using System.Collections.Generic;
using FFmpeg.AutoGen.Abstractions;

namespace FFmpegSharp
{
    /// <summary>
    /// <see cref="SwsContext"/> wapper
    /// </summary>
    public unsafe class Swscale : IConverter, IDisposable
    {
        protected SwsContext* pContext;

        public Swscale(SwsContext* pSwsContext, bool isDisposeByOwner = true)
        {
            pContext = pSwsContext;
            disposedValue = !isDisposeByOwner;
        }

        public Swscale() : this(ffmpeg.sws_alloc_context())
        { }

        public Swscale(int srcWidth, int srcHeight, AVPixelFormat srcFormat,
            int dstWidth, int dstHeight, AVPixelFormat dstFormat,
            int flags = ffmpeg.SWS_BILINEAR, SwsFilter* swsFilter = null, SwsFilter* dstFilter = null, double* param = null)
        {
            Reset(srcWidth, srcHeight, srcFormat, dstWidth, dstHeight, dstFormat, flags, swsFilter, dstFilter, param);
        }

        public void Reset(int srcWidth, int srcHeight, AVPixelFormat srcFormat,
            int dstWidth, int dstHeight, AVPixelFormat dstFormat,
            int flags = ffmpeg.SWS_BILINEAR, SwsFilter* swsFilter = null, SwsFilter* dstFilter = null, double* param = null)
        {
            ffmpeg.sws_freeContext(pContext);
            pContext = ffmpeg.sws_getContext(srcWidth, srcHeight, srcFormat, dstWidth, dstHeight, dstFormat, flags, swsFilter, dstFilter, param);
        }

        public IEnumerable<MediaFrame> Convert(MediaFrame srcframe, MediaFrame dstframe)
        {
            ffmpeg.av_frame_copy_props(dstframe, srcframe).ThrowIfError();
            AVFrame* src = srcframe;
            AVFrame* dst = dstframe;
            if (pContext == null)
                Reset(src->width, src->height, (AVPixelFormat)src->format, dst->width, dst->height, (AVPixelFormat)dst->format);
            ffmpeg.sws_scale_frame(pContext, srcframe, dstframe).ThrowIfError();
            return [dstframe];
        }

        public static implicit operator SwsContext*(Swscale value)
        {
            if (value is null) return null;
            return value.pContext;
        }

        #region

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                ffmpeg.sws_freeContext(pContext);
                pContext = null;
                disposedValue = true;
            }
        }

        ~Swscale()
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
