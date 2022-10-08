using System;
using System.Collections.Generic;

using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    /// <summary>
    /// <see cref="SwsContext"/> wapper
    /// </summary>
    public unsafe class PixelConverter : IFrameConverter, IDisposable
    {
        protected SwsContext* pContext = null;
        private SwsFilter* dstFilter;
        private double* param;
        private MediaFrame dstFrame;
        private bool disposedValue;
        public readonly AVPixelFormat DstFormat;
        public readonly int DstWidth;
        public readonly int DstHeight;
        public readonly int Flags;

        /// <summary>
        /// create video frame converter by dst parames, will allocate new frame and buffer data, convert will return new frame
        /// </summary>
        /// <param name="pSwsContext"></param>
        /// <param name="dstFormat"></param>
        /// <param name="dstWidth"></param>
        /// <param name="dstHeight"></param>
        /// <param name="flags"></param>
        /// <param name="dstFilter"></param>
        /// <param name="param"></param>
        /// <param name="isDisposeByOwner"></param>
        public PixelConverter(SwsContext* pSwsContext, AVPixelFormat dstFormat, int dstWidth, int dstHeight, int flags = ffmpeg.SWS_BILINEAR, SwsFilter* dstFilter = null, double* param = null, bool isDisposeByOwner = true)
           : this(dstFormat, dstWidth, dstHeight, flags, dstFilter, param)
        {
            pContext = pSwsContext;
            disposedValue = !isDisposeByOwner;
        }

        /// <summary>
        /// create video frame converter by dstframe,<paramref name="dstFrame"/> need allocate new buffer(s) data, convert will return <paramref name="dstFrame"/>
        /// </summary>
        /// <param name="pSwsContext"></param>
        /// <param name="dstFrame"></param>
        /// <param name="flags"></param>
        /// <param name="dstFilter"></param>
        /// <param name="param"></param>
        /// <param name="isDisposeByOwner"></param>
        public PixelConverter(SwsContext* pSwsContext, MediaFrame dstFrame, int flags = ffmpeg.SWS_BILINEAR, SwsFilter* dstFilter = null, double* param = null, bool isDisposeByOwner = true)
            : this(dstFrame, flags, dstFilter, param)
        {
            pContext = pSwsContext;
            disposedValue = !isDisposeByOwner;
        }

        /// <summary>
        /// create video frame converter by dst parames, will allocate new frame and buffer data, convert will return new frame
        /// </summary>
        /// <param name="dstFormat"></param>
        /// <param name="dstWidth"></param>
        /// <param name="dstHeight"></param>
        /// <param name="flags"></param>
        /// <param name="dstFilter"></param>
        /// <param name="param"></param>
        public PixelConverter(AVPixelFormat dstFormat, int dstWidth, int dstHeight, int flags = ffmpeg.SWS_BILINEAR, SwsFilter* dstFilter = null, double* param = null)
        {
            DstWidth = dstWidth;
            DstHeight = dstHeight;
            DstFormat = dstFormat;
            Flags = flags;
            this.dstFilter = dstFilter;
            this.param = param;
        }

        /// <summary>
        /// create video frame converter by dstframe,<paramref name="dstFrame"/> need allocate new buffer(s) data, convert will return <paramref name="dstFrame"/>
        /// </summary>
        /// <param name="dstFrame"></param>
        /// <param name="flags"></param>
        /// <param name="dstFilter"></param>
        /// <param name="param"></param>
        public PixelConverter(MediaFrame dstFrame, int flags = ffmpeg.SWS_BILINEAR, SwsFilter* dstFilter = null, double* param = null)
            : this((AVPixelFormat)dstFrame.Format, dstFrame.Width, dstFrame.Height, flags, dstFilter, param)
        {
            this.dstFrame = dstFrame;
        }

        /// <summary>
        /// create video frame converter by dst codec
        /// </summary>
        /// <param name="dstCodecContext"></param>
        /// <param name="flags"></param>
        /// <param name="dstFilter"></param>
        /// <param name="param"></param>
        public static PixelConverter CreateByDstCodeContext(MediaCodecContext dstCodecContext, int flags = ffmpeg.SWS_BILINEAR, SwsFilter* dstFilter = null, double* param = null)
        {
            if (dstCodecContext.AVCodecContext.codec_type != AVMediaType.AVMEDIA_TYPE_VIDEO)
                throw new FFmpegException(ffmpeg.AVERROR_INVALIDDATA);
            return new PixelConverter(dstCodecContext.PixFmt, dstCodecContext.Width, dstCodecContext.Height, flags, dstFilter, param);
        }

        /// <summary>
        /// Convert <paramref name="srcframe"/>
        /// <para>
        /// Video conversion can be made without the use of IEnumerable,
        /// here In order to be consistent with the <see cref="SampleConverter"/> interface.
        /// </para>
        /// </summary>
        /// <param name="srcframe"></param>
        /// <returns></returns>
        public IEnumerable<MediaFrame> Convert(MediaFrame srcframe) => Convert(srcframe, null);
        public IEnumerable<MediaFrame> Convert(MediaFrame srcframe, SwsFilter* srcFilter)
        {
            return new[] { ConvertFrame(srcframe, srcFilter) };
        }

        public MediaFrame ConvertFrame(MediaFrame srcFrame, SwsFilter* srcFilter = null)
        {
            AVFrame* src = srcFrame;
            var output = dstFrame ?? MediaFrame.CreateVideoFrame(DstWidth,DstHeight,DstFormat);
            AVFrame* dst = output;
            pContext = ffmpeg.sws_getCachedContext(pContext,
                src->width, src->height, (AVPixelFormat)src->format,
                DstWidth, DstHeight, DstFormat, Flags, srcFilter, dstFilter, param);
            ffmpeg.sws_scale(pContext, src->data, src->linesize, 0, src->height, dst->data, dst->linesize).ThrowIfError();
            return output;
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
