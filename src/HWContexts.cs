using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    /// <summary>
    /// Owns one reference to an <see cref="AVHWDeviceContext"/> buffer (refcounted via <see cref="AVBufferRef"/>).
    /// Consumers that attach it (codec contexts, filter graphs, frames contexts) take their own reference,
    /// so disposing this wrapper never invalidates them.
    /// </summary>
    public unsafe sealed class HWDeviceContext : IDisposable
    {
        private bool disposedValue;
        internal AVBufferRef* pBufferRef;

        // takeRef: wrap a borrowed pointer by taking our own reference; false adopts the given reference.
        internal HWDeviceContext(AVBufferRef* pRef, bool takeRef)
        {
            if (pRef == null) throw new ArgumentNullException(nameof(pRef));
            pBufferRef = takeRef ? ffmpeg.av_buffer_ref(pRef) : pRef;
            if (pBufferRef == null) throw new FFmpegException("av_buffer_ref returned null");
        }

        /// <summary>
        /// Create a HW device context (<see cref="ffmpeg.av_hwdevice_ctx_create(AVBufferRef**, AVHWDeviceType, string, AVDictionary*, int)"/>).
        /// </summary>
        /// <param name="type">Device type (e.g. CUDA, D3D11VA, QSV, VAAPI).</param>
        /// <param name="device">Device specifier (driver-defined, e.g. "0" for CUDA, "/dev/dri/renderD128" for VAAPI).</param>
        /// <param name="opts">Device-creation options.</param>
        /// <param name="flags">Reserved (currently ignored by FFmpeg).</param>
        public static HWDeviceContext Create(AVHWDeviceType type, string device = null, MediaDictionary opts = null, int flags = 0)
        {
            AVBufferRef* dev = null;
            if (opts == null)
            {
                ffmpeg.av_hwdevice_ctx_create(&dev, type, device, null, flags).ThrowIfError();
            }
            else
            {
                fixed (AVDictionary** pOpts = &opts.pDictionary)
                    ffmpeg.av_hwdevice_ctx_create(&dev, type, device, *pOpts, flags).ThrowIfError();
            }
            return new HWDeviceContext(dev, takeRef: false);
        }

        /// <summary>Name-based convenience (e.g. "cuda", "d3d11va", "qsv", "vaapi").</summary>
        public static HWDeviceContext Create(string typeName, string device = null, MediaDictionary opts = null, int flags = 0)
        {
            var resolved = ffmpeg.av_hwdevice_find_type_by_name(typeName);
            if (resolved == AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
                throw new ArgumentException($"Unknown HW device type '{typeName}'.", nameof(typeName));
            return Create(resolved, device, opts, flags);
        }

        public AVHWDeviceType Type => ((AVHWDeviceContext*)pBufferRef->data)->type;

        /// <summary>Borrowed pointer escape hatch — valid while this wrapper is alive.</summary>
        public static implicit operator AVBufferRef*(HWDeviceContext ctx) => ctx == null ? null : ctx.pBufferRef;

        private void DisposeCore()
        {
            if (disposedValue) return;
            if (pBufferRef != null)
            {
                fixed (AVBufferRef** pp = &pBufferRef)
                    ffmpeg.av_buffer_unref(pp);
            }
            disposedValue = true;
        }

        ~HWDeviceContext() => DisposeCore();

        public void Dispose()
        {
            DisposeCore();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Owns one reference to an <see cref="AVHWFramesContext"/> buffer (a GPU surface pool tied to a
    /// <see cref="HWDeviceContext"/>). Same refcounting semantics as <see cref="HWDeviceContext"/>.
    /// </summary>
    public unsafe sealed class HWFramesContext : IDisposable
    {
        private bool disposedValue;
        internal AVBufferRef* pBufferRef;

        internal HWFramesContext(AVBufferRef* pRef, bool takeRef)
        {
            if (pRef == null) throw new ArgumentNullException(nameof(pRef));
            pBufferRef = takeRef ? ffmpeg.av_buffer_ref(pRef) : pRef;
            if (pBufferRef == null) throw new FFmpegException("av_buffer_ref returned null");
        }

        /// <summary>
        /// Allocate and initialize a frames pool on <paramref name="device"/>
        /// (<see cref="ffmpeg.av_hwframe_ctx_alloc(AVBufferRef*)"/> + <see cref="ffmpeg.av_hwframe_ctx_init(AVBufferRef*)"/>).
        /// </summary>
        /// <param name="device">The device the surfaces live on.</param>
        /// <param name="hwPixelFormat">HW surface pixel format (e.g. AV_PIX_FMT_CUDA).</param>
        /// <param name="swPixelFormat">Underlying SW pixel format of the surfaces (typically NV12).</param>
        /// <param name="width">Surface width.</param>
        /// <param name="height">Surface height.</param>
        /// <param name="poolSize">
        /// Initial surface pool size. On most device types (QSV, VAAPI, D3D11) the pool is FIXED at init —
        /// undersizing fails at runtime, while each surface costs real VRAM (a 4K NV12 surface is ~12 MB).
        /// Size to the pipeline depth instead of guessing: an encoder needs roughly
        /// <c>max_b_frames + lookahead + async/surface queue depth + frames you hold in flight</c>;
        /// pure upload/filter pools usually get by with 2–4. The default of 20 is a conservative
        /// upper bound for encode pipelines.
        /// </param>
        public static HWFramesContext Create(HWDeviceContext device, AVPixelFormat hwPixelFormat, AVPixelFormat swPixelFormat, int width, int height, int poolSize = 20)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            var framesRef = ffmpeg.av_hwframe_ctx_alloc(device);
            if (framesRef == null) throw new FFmpegException("av_hwframe_ctx_alloc returned null");
            var ctx = (AVHWFramesContext*)framesRef->data;
            ctx->format = hwPixelFormat;
            ctx->sw_format = swPixelFormat;
            ctx->width = width;
            ctx->height = height;
            ctx->initial_pool_size = poolSize;
            int ret = ffmpeg.av_hwframe_ctx_init(framesRef);
            if (ret < 0)
            {
                ffmpeg.av_buffer_unref(&framesRef);
                ret.ThrowIfError();
            }
            return new HWFramesContext(framesRef, takeRef: false);
        }

        public AVPixelFormat Format => ((AVHWFramesContext*)pBufferRef->data)->format;
        public AVPixelFormat SwFormat => ((AVHWFramesContext*)pBufferRef->data)->sw_format;
        public int Width => ((AVHWFramesContext*)pBufferRef->data)->width;
        public int Height => ((AVHWFramesContext*)pBufferRef->data)->height;

        /// <summary>Borrowed pointer escape hatch — valid while this wrapper is alive.</summary>
        public static implicit operator AVBufferRef*(HWFramesContext ctx) => ctx == null ? null : ctx.pBufferRef;

        private void DisposeCore()
        {
            if (disposedValue) return;
            if (pBufferRef != null)
            {
                fixed (AVBufferRef** pp = &pBufferRef)
                    ffmpeg.av_buffer_unref(pp);
            }
            disposedValue = true;
        }

        ~HWFramesContext() => DisposeCore();

        public void Dispose()
        {
            DisposeCore();
            GC.SuppressFinalize(this);
        }
    }
}
