using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    /// <summary>
    /// Tunable options for <see cref="Swscale"/>. Reapplied whenever the underlying SwsContext is (re)allocated.
    /// </summary>
    public unsafe struct SwscaleOptions
    {
        /// <summary>SWS_BILINEAR / SWS_BICUBIC / SWS_LANCZOS / ... Default: SWS_BILINEAR.</summary>
        public int Flags;
        /// <summary>Optional pre-filter applied to the source (rare).</summary>
        public SwsFilter* SrcFilter;
        /// <summary>Optional post-filter applied to the destination (rare).</summary>
        public SwsFilter* DstFilter;
        /// <summary>Optional algorithm-specific parameters; null = defaults.</summary>
        public double* Param;
        /// <summary>True to copy frame metadata (pts, time_base, side_data) on every <c>Convert</c>. Default true.</summary>
        public bool CopyProps;

        public static SwscaleOptions Default => new SwscaleOptions
        {
            Flags = (int)SwsFlags.SWS_BILINEAR,
            CopyProps = true,
        };
    }

    /// <summary>
    /// <see cref="SwsContext"/> wrapper. Holds a cached SwsContext keyed on (srcW, srcH, srcFmt, dstW, dstH, dstFmt);
    /// on every <see cref="Convert(MediaFrame, MediaFrame)"/>, if any of those changed since the last call, the
    /// context is transparently rebuilt — no silent corruption from stale dimensions.
    /// </summary>
    public unsafe class Swscale : IConverter, IDisposable
    {
        protected SwsContext* pContext;
        private SwscaleOptions _opts = SwscaleOptions.Default;

        private int _cachedSrcW, _cachedSrcH, _cachedDstW, _cachedDstH;
        private AVPixelFormat _cachedSrcFmt = AVPixelFormat.AV_PIX_FMT_NONE;
        private AVPixelFormat _cachedDstFmt = AVPixelFormat.AV_PIX_FMT_NONE;

        public Swscale(SwsContext* pSwsContext, bool isDisposeByOwner = true)
        {
            if (pSwsContext == null) throw new ArgumentNullException(nameof(pSwsContext));
            pContext = pSwsContext;
            disposedValue = !isDisposeByOwner;
        }

        /// <summary>Default ctor — context is lazily allocated on the first <see cref="Convert(MediaFrame, MediaFrame)"/> call from frame metadata.</summary>
        public Swscale()
        { }

        public Swscale(SwscaleOptions options) : this()
        {
            _opts = options;
        }

        public Swscale(int srcWidth, int srcHeight, AVPixelFormat srcFormat,
            int dstWidth, int dstHeight, AVPixelFormat dstFormat,
            SwscaleOptions? options = null)
        {
            if (options.HasValue) _opts = options.Value;
            Reset(srcWidth, srcHeight, srcFormat, dstWidth, dstHeight, dstFormat);
        }

        public SwscaleOptions Options
        {
            get => _opts;
            set
            {
                _opts = value;
                if (pContext != null && _cachedSrcW > 0)
                    Reset(_cachedSrcW, _cachedSrcH, _cachedSrcFmt, _cachedDstW, _cachedDstH, _cachedDstFmt);
            }
        }

        public void Reset(int srcWidth, int srcHeight, AVPixelFormat srcFormat,
            int dstWidth, int dstHeight, AVPixelFormat dstFormat)
        {
            ffmpeg.sws_freeContext(pContext);
            pContext = ffmpeg.sws_getContext(
                srcWidth, srcHeight, srcFormat,
                dstWidth, dstHeight, dstFormat,
                _opts.Flags, _opts.SrcFilter, _opts.DstFilter, _opts.Param);
            if (pContext == null)
                throw new FFmpegException(
                    $"sws_getContext returned null for {srcWidth}x{srcHeight} {srcFormat} -> {dstWidth}x{dstHeight} {dstFormat} (flags={_opts.Flags}).");
            _cachedSrcW = srcWidth; _cachedSrcH = srcHeight; _cachedSrcFmt = srcFormat;
            _cachedDstW = dstWidth; _cachedDstH = dstHeight; _cachedDstFmt = dstFormat;
        }

        /// <summary>
        /// Convert <paramref name="src"/> into <paramref name="dst"/>. <paramref name="dst"/> must be a pre-allocated
        /// frame: set Width/Height/Format then call <see cref="MediaFrame.AllocateBuffer"/>.
        /// </summary>
        public int Convert(MediaFrame src, MediaFrame dst)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            if (dst == null) throw new ArgumentNullException(nameof(dst));
            AVFrame* s = src; AVFrame* d = dst;

            // Rebuild context whenever the input or output shape changes — prevents silent corruption when the
            // demuxer's resolution/format drifts mid-stream (e.g. an ABR ladder).
            if (pContext == null
                || s->width != _cachedSrcW || s->height != _cachedSrcH || (AVPixelFormat)s->format != _cachedSrcFmt
                || d->width != _cachedDstW || d->height != _cachedDstH || (AVPixelFormat)d->format != _cachedDstFmt)
            {
                Reset(s->width, s->height, (AVPixelFormat)s->format,
                      d->width, d->height, (AVPixelFormat)d->format);
            }

            if (_opts.CopyProps)
                ffmpeg.av_frame_copy_props(d, s).ThrowIfError();
            ffmpeg.sws_scale_frame(pContext, dst, src).ThrowIfError();
            return 1;
        }

        /// <summary>
        /// Backwards-compatible enumerable-yielding shim. Prefer <see cref="Convert(MediaFrame, MediaFrame)"/>.
        /// </summary>
        [Obsolete("Use Convert(src, dst) returning int instead — the enumerable signature allocated per call.")]
        public IEnumerable<MediaFrame> ConvertEnumerable(MediaFrame src, MediaFrame dst)
        {
            Convert(src, dst);
            yield return dst;
        }

        public static implicit operator SwsContext*(Swscale value)
        {
            if (value is null) return null;
            return value.pContext;
        }

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
    }
}
