using System;
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
        /// <summary>
        /// True to copy frame metadata (pts, time_base, side_data) on every <c>Convert</c>. Default true.
        /// Set to false for pure pixel conversion to save one native call per frame.
        /// </summary>
        public bool CopyProps;
        /// <summary>
        /// Worker thread count for the conversion. 0 or 1 (default) = single-threaded classic context.
        /// Values &gt; 1 build the context via the AVOption path and request that many slice threads —
        /// requires FFmpeg ≥ 5.1 (older builds silently stay single-threaded). Pass
        /// <c>Environment.ProcessorCount</c> for "auto".
        /// </summary>
        public int Threads;

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
    /// <para>
    /// Conversion is single-threaded by default. For heavy work (4K+, expensive scalers) either set
    /// <see cref="SwscaleOptions.Threads"/> (FFmpeg ≥ 5.1), or run the scale inside a
    /// <see cref="MediaFilterGraph"/> with <see cref="MediaFilterGraph.ThreadCount"/>.
    /// </para>
    /// </summary>
    public unsafe class Swscale : IConverter, IDisposable
    {
        protected SwsContext* pContext;
        private SwscaleOptions _opts = SwscaleOptions.Default;

        private int _cachedSrcW, _cachedSrcH, _cachedDstW, _cachedDstH;
        private AVPixelFormat _cachedSrcFmt = AVPixelFormat.AV_PIX_FMT_NONE;
        private AVPixelFormat _cachedDstFmt = AVPixelFormat.AV_PIX_FMT_NONE;

        /// <summary>
        /// Wrap an existing <see cref="SwsContext"/> pointer.
        /// </summary>
        /// <param name="pSwsContext">Native context (must be non-null).</param>
        /// <param name="leaveOpen">
        /// When <see langword="true"/>, the wrapper does NOT free the context on dispose; the caller retains ownership.
        /// </param>
        public Swscale(SwsContext* pSwsContext, bool leaveOpen)
        {
            if (pSwsContext == null) throw new ArgumentNullException(nameof(pSwsContext));
            pContext = pSwsContext;
            disposedValue = leaveOpen;
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
            pContext = null;
            pContext = _opts.Threads > 1
                ? CreateThreadedContext(srcWidth, srcHeight, srcFormat, dstWidth, dstHeight, dstFormat)
                : ffmpeg.sws_getContext(
                    srcWidth, srcHeight, srcFormat,
                    dstWidth, dstHeight, dstFormat,
                    _opts.Flags, _opts.SrcFilter, _opts.DstFilter, _opts.Param);
            if (pContext == null)
                throw new FFmpegException(
                    $"sws_getContext returned null for {srcWidth}x{srcHeight} {srcFormat} -> {dstWidth}x{dstHeight} {dstFormat} (flags={_opts.Flags}).");
            _cachedSrcW = srcWidth; _cachedSrcH = srcHeight; _cachedSrcFmt = srcFormat;
            _cachedDstW = dstWidth; _cachedDstH = dstHeight; _cachedDstFmt = dstFormat;
        }

        // sws_getContext has no threading parameter — a multi-threaded context can only be built through
        // sws_alloc_context + AVOptions + sws_init_context. Threading applies to sws_scale_frame (which
        // Convert uses), not the slice API.
        private SwsContext* CreateThreadedContext(int srcWidth, int srcHeight, AVPixelFormat srcFormat,
            int dstWidth, int dstHeight, AVPixelFormat dstFormat)
        {
            var ctx = ffmpeg.sws_alloc_context();
            if (ctx == null) throw new FFmpegException("sws_alloc_context returned null");
            try
            {
                MediaOptions.SetInt(ctx, "srcw", srcWidth, 0).ThrowIfError();
                MediaOptions.SetInt(ctx, "srch", srcHeight, 0).ThrowIfError();
                MediaOptions.SetInt(ctx, "src_format", (int)srcFormat, 0).ThrowIfError();
                MediaOptions.SetInt(ctx, "dstw", dstWidth, 0).ThrowIfError();
                MediaOptions.SetInt(ctx, "dsth", dstHeight, 0).ThrowIfError();
                MediaOptions.SetInt(ctx, "dst_format", (int)dstFormat, 0).ThrowIfError();
                MediaOptions.SetInt(ctx, "sws_flags", _opts.Flags, 0).ThrowIfError();
                if (_opts.Param != null)
                {
                    ffmpeg.av_opt_set_double(ctx, "param0", _opts.Param[0], 0).ThrowIfError();
                    ffmpeg.av_opt_set_double(ctx, "param1", _opts.Param[1], 0).ThrowIfError();
                }
                // Best effort: the "threads" option only exists on FFmpeg >= 5.1; on older builds the
                // context simply stays single-threaded.
                MediaOptions.SetInt(ctx, "threads", _opts.Threads, 0);
                ffmpeg.sws_init_context(ctx, _opts.SrcFilter, _opts.DstFilter).ThrowIfError();
                return ctx;
            }
            catch
            {
                ffmpeg.sws_freeContext(ctx);
                throw;
            }
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
