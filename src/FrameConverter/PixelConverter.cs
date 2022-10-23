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
        protected SwsContext* pContext;
        protected int dstWidth;
        protected int dstHeight;
        protected AVPixelFormat dstFormat;
        protected SwsFilter dstFilter;
        protected SwsFilter srcFilter;

        public PixelConverter(SwsContext* pSwsContext, bool isDisposeByOwner = true)
        {
            pContext = pSwsContext;
            disposedValue = !isDisposeByOwner;
        }

        public PixelConverter() : this(ffmpeg.sws_alloc_context())
        { }


        public static PixelConverter Create(int dstWidth, int dstHeight, AVPixelFormat dstFormat, SwsFilter dstFilter = default)
        {
            var pixelConverter = new PixelConverter();
            pixelConverter.SetOpts(dstWidth,dstHeight,dstFormat,dstFilter);
            return pixelConverter;
        }

        /// <summary>
        ///  
        /// </summary>
        /// <param name="dstWidth"></param>
        /// <param name="dstHeight"></param>
        /// <param name="dstFormat"></param>
        /// <param name="dstFilter"></param>
        public void SetOpts(int dstWidth, int dstHeight, AVPixelFormat dstFormat, SwsFilter dstFilter = default)
        {
            this.dstWidth = dstWidth;
            this.dstHeight = dstHeight;
            this.dstFormat = dstFormat;
            this.dstFilter = dstFilter;
        }

        IEnumerable<MediaFrame> IFrameConverter.Convert(MediaFrame srcframe, MediaFrame dstframe) => Convert(srcframe, dstframe);

        /// <summary>
        /// Convert <paramref name="srcframe"/>
        /// <para>
        /// Video conversion can be made without the use of IEnumerable,
        /// here In order to be consistent with the <see cref="SampleConverter"/> interface.
        /// </para>
        /// </summary>
        /// <param name="srcframe"></param>
        /// <param name="dstframe">the frame must be buffered</param>
        /// <param name="flags"></param>
        /// <param name="srcFilter"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public IEnumerable<MediaFrame> Convert(MediaFrame srcframe, MediaFrame dstframe = null, int flags = ffmpeg.SWS_BICUBIC, SwsFilter srcFilter = default, double[] param = null)
        {
            var tmpframe = ConvertFrame(srcframe, dstframe, flags, srcFilter, param);
            yield return tmpframe;
            if (dstframe == null) tmpframe?.Dispose();
        }

        private MediaFrame ConvertFrame(MediaFrame srcframe, MediaFrame dstframe, int flags = ffmpeg.SWS_BICUBIC, SwsFilter srcFilter = default, double[] param = null)
        {
            if (dstframe == null)
                dstframe = new MediaFrame();
            else
                dstframe.Unref();
            srcframe.CopyProps(dstframe);
            dstframe.Width = dstWidth;
            dstframe.Height = dstHeight;
            dstframe.Format = (int)dstFormat;
            dstframe.AllocateBuffer();
            AVFrame* src = srcframe;
            AVFrame* dst = dstframe;
            fixed (SwsFilter* pDstFilter = &dstFilter)
            fixed (double* pparam = param)
            {
                pContext = ffmpeg.sws_getCachedContext(pContext,
                    src->width, src->height, (AVPixelFormat)src->format,
                    dstframe.Width, dstframe.Height, (AVPixelFormat)dstframe.Format, flags, &srcFilter, pDstFilter, pparam);
            }
            ffmpeg.sws_scale(pContext, src->data, src->linesize, 0, src->height, dst->data, dst->linesize).ThrowIfError();
            return dstframe;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcframe"></param>
        /// <param name="flags"></param>
        /// <param name="srcFilter"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public MediaFrame ConvertFrame(MediaFrame srcframe, int flags = ffmpeg.SWS_BICUBIC, SwsFilter srcFilter = default, double[] param = null)
        {
            return ConvertFrame(srcframe, null, flags, srcFilter, param);
        }

        public static implicit operator SwsContext*(PixelConverter value)
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
        #endregion

    }
}
