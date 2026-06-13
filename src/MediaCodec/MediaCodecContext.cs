using System;
using System.Collections.Generic;
using System.Linq;
using FFmpeg.AutoGen;


namespace FFmpeg.Sharp
{
    public unsafe partial class MediaCodecContext : IDisposable
    {
        // Default `false` (owned). The (ptr, leaveOpen) ctor flips to `true` for borrowed pointers.
        // Historical bug (fixed): this was `true`, which made every raw-pointer ctor silently leak the codec context.
        private bool disposedValue;

        public MediaCodecContext(AVCodecContext* pAVCodecContext, bool leaveOpen)
            : this(pAVCodecContext)
        {
            disposedValue = leaveOpen;
        }

        public MediaCodecContext(MediaCodec codec = null)
            : this(ffmpeg.avcodec_alloc_context3(codec), false)
        { }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (pCodecContext != null)
                {
                    fixed (AVCodecContext** ppCodecContext = &pCodecContext)
                    {
                        ffmpeg.avcodec_free_context(ppCodecContext);
                    }
                }
                disposedValue = true;
            }
            // Outside the disposedValue guard so borrowed (leaveOpen) wrappers that wired get_format
            // still release the handle; IsAllocated makes double-dispose safe.
            if (_getFormatHandle.IsAllocated) _getFormatHandle.Free();
        }

        ~MediaCodecContext()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
  
        public MediaCodec GetCodec() => pCodecContext->codec == null ? null : new MediaCodec(pCodecContext->codec);

        /// <summary>True if avcodec_open2 has been called on this context.</summary>
        public bool IsOpen => ffmpeg.avcodec_is_open(pCodecContext) != 0;

        /// <summary>
        /// Open the codec context (<see cref="ffmpeg.avcodec_open2(AVCodecContext*, AVCodec*, AVDictionary**)"/>).
        /// <para>
        /// Configure the context between construction and this call — via <c>.Ref</c> field access,
        /// <see cref="SetCodecParameters"/>, <see cref="InitHWDeviceContext(AVHWDeviceType?, string, MediaDictionary, int, bool)"/>, etc.
        /// Once open, most parameters are frozen by FFmpeg.
        /// </para>
        /// </summary>
        /// <param name="opts">Codec options. Consumed keys are removed (avcodec_open2 semantics).</param>
        /// <returns>This context, for chaining (e.g. <c>new MediaDecoder(codec).Open()</c>).</returns>
        public MediaCodecContext Open(MediaDictionary opts = null)
        {
            if (pCodecContext == null) throw new InvalidOperationException("Codec context is not allocated.");
            if (pCodecContext->codec == null) throw new InvalidOperationException("No codec is bound to this context — construct with a MediaCodec.");
            if (IsOpen) throw new InvalidOperationException("Codec context is already open.");
            if (opts == null)
            {
                ffmpeg.avcodec_open2(pCodecContext, pCodecContext->codec, null).ThrowIfError();
            }
            else
            {
                fixed (AVDictionary** pOpts = &opts.pDictionary)
                    ffmpeg.avcodec_open2(pCodecContext, pCodecContext->codec, pOpts).ThrowIfError();
            }
            return this;
        }

        /// <summary>
        /// Fill this codec context based on the values from the supplied codec parameters.
        /// <para>
        /// <seealso cref="ffmpeg.avcodec_parameters_to_context(AVCodecContext*, AVCodecParameters*)"/>
        /// </para>
        /// </summary>
        /// <param name="codecpar">Source codec parameters.</param>
        public void SetCodecParameters(ref AVCodecParameters codecpar)
        {
            fixed (AVCodecParameters* p = &codecpar)
                ffmpeg.avcodec_parameters_to_context(pCodecContext, p).ThrowIfError();
        }

        /// <summary>
        /// Apply <paramref name="opts"/> to this codec context via <see cref="ffmpeg.av_opt_set_dict(void*, AVDictionary**)"/>,
        /// and, when <paramref name="includePrivate"/> is <see langword="true"/> and the context has codec private data,
        /// to <c>priv_data</c> as well.
        /// <para>
        /// Consumed keys are removed from <paramref name="opts"/> (av_opt_set_dict semantics); unrecognised keys remain.
        /// </para>
        /// </summary>
        /// <param name="opts">Options to apply. Entries that are consumed are removed.</param>
        /// <param name="includePrivate">Also apply the options to the codec's private context.</param>
        /// <returns>The first negative error code, or 0 on success.</returns>
        public int SetOptions(MediaDictionary opts, bool includePrivate = true)
        {
            if (opts == null) throw new ArgumentNullException(nameof(opts));
            fixed (AVDictionary** pOpts = &opts.pDictionary)
            {
                int ret = ffmpeg.av_opt_set_dict(pCodecContext, pOpts);
                if (ret < 0) return ret;
                if (includePrivate && pCodecContext->priv_data != null)
                {
                    ret = ffmpeg.av_opt_set_dict(pCodecContext->priv_data, pOpts);
                    if (ret < 0) return ret;
                }
                return 0;
            }
        }

        // AVCodecHWConfig.methods bit flags (copied here so users can reference them by name).
        public const int AV_CODEC_HW_CONFIG_METHOD_HW_DEVICE_CTX = 0x01;
        public const int AV_CODEC_HW_CONFIG_METHOD_HW_FRAMES_CTX = 0x02;
        public const int AV_CODEC_HW_CONFIG_METHOD_INTERNAL = 0x04;
        public const int AV_CODEC_HW_CONFIG_METHOD_AD_HOC = 0x08;

        // We keep the chosen HW pixel format as an instance field rather than capturing it in a closure —
        // the field is then read by the static get_format trampoline without per-call allocation.
        private AVPixelFormat _hwPixFmt = AVPixelFormat.AV_PIX_FMT_NONE;
        // Delegate assigned once and pinned via GCHandle (see docs/architecture.md: field references alone
        // are not sufficient to keep callback trampolines alive). Freed in Dispose.
        private AVCodecContext_get_format _getFormatDelegate;
        private System.Runtime.InteropServices.GCHandle _getFormatHandle;
        // Whether to fall back to a software pixel format if the negotiated HW format isn't offered by the codec.
        private bool _hwFallbackToSw;

        /// <summary>
        /// Initialize hardware acceleration on this codec context.
        /// <para>
        /// Walks the codec's HW configs and picks the first one matching <paramref name="type"/> that supports
        /// any of <c>HW_DEVICE_CTX</c>, <c>HW_FRAMES_CTX</c>, or <c>INTERNAL</c> methods, then creates the
        /// corresponding <see cref="ffmpeg.av_hwdevice_ctx_create"/> and wires the <c>get_format</c> callback.
        /// </para>
        /// </summary>
        /// <param name="type">Device type. Pass <see langword="null"/> to auto-pick the first available.</param>
        /// <param name="device">Optional device specifier (e.g. "/dev/dri/renderD128" for VAAPI, "0" for CUDA index).</param>
        /// <param name="opts">Device-creation options.</param>
        /// <param name="flags">Reserved (currently ignored by FFmpeg).</param>
        /// <param name="fallbackToSw">When <see langword="true"/>, the get_format callback will fall back to the first SW format if no HW format is offered.</param>
        /// <returns>The HW config method that matched (<c>HW_DEVICE_CTX</c>/<c>HW_FRAMES_CTX</c>/<c>INTERNAL</c>), or 0 if nothing matched.</returns>
        public int InitHWDeviceContext(AVHWDeviceType? type = null, string device = null, MediaDictionary opts = null, int flags = 0, bool fallbackToSw = false)
        {
            if (type == AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
                throw new ArgumentException("Pass null for auto-detect, not AV_HWDEVICE_TYPE_NONE.", nameof(type));
            GuardHWInit();
            if (pCodecContext->hw_device_ctx != null)
                return 0; // already initialised

            var chosen = PickHWConfig(type);
            if (chosen == null) return 0;

            var cfg = chosen.Value;
            _hwPixFmt = cfg.pix_fmt;
            _hwFallbackToSw = fallbackToSw;

            // HW_DEVICE_CTX path: create a device and let the get_format callback select the HW pix_fmt.
            if ((cfg.methods & AV_CODEC_HW_CONFIG_METHOD_HW_DEVICE_CTX) != 0)
            {
                using (var dev = HWDeviceContext.Create(cfg.device_type, device, opts, flags))
                    AttachHWDeviceRef(dev);
                WireGetFormat();
                return AV_CODEC_HW_CONFIG_METHOD_HW_DEVICE_CTX;
            }
            // FRAMES_CTX-only path: caller is expected to supply hw_frames_ctx (e.g. via filter graph). We still wire get_format.
            if ((cfg.methods & AV_CODEC_HW_CONFIG_METHOD_HW_FRAMES_CTX) != 0)
            {
                WireGetFormat();
                return AV_CODEC_HW_CONFIG_METHOD_HW_FRAMES_CTX;
            }
            // INTERNAL path: codec handles it without device/frames context. No further setup needed.
            return AV_CODEC_HW_CONFIG_METHOD_INTERNAL;
        }

        /// <summary>String-name overload that resolves the device type via <see cref="ffmpeg.av_hwdevice_find_type_by_name"/>.</summary>
        public int InitHWDeviceContext(string typeName, string device = null, MediaDictionary opts = null, int flags = 0, bool fallbackToSw = false)
        {
            if (typeName == null) return InitHWDeviceContext((AVHWDeviceType?)null, device, opts, flags, fallbackToSw);
            var resolved = ffmpeg.av_hwdevice_find_type_by_name(typeName);
            if (resolved == AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
                throw new ArgumentException($"Unknown HW device type '{typeName}'. Use ffmpeg.av_hwdevice_get_type_name() to enumerate.", nameof(typeName));
            return InitHWDeviceContext(resolved, device, opts, flags, fallbackToSw);
        }

        /// <summary>
        /// Initialize hardware acceleration using an existing (shared) <see cref="HWDeviceContext"/> —
        /// e.g. one device driving decoder, filter graph and encoder. Unlike <see cref="AttachHWDevice"/>,
        /// this walks the codec's HW configs for <paramref name="device"/>'s type and wires the
        /// <c>get_format</c> negotiation, which is required for decoders.
        /// </summary>
        /// <param name="device">The shared device. This context takes its own reference.</param>
        /// <param name="fallbackToSw">When <see langword="true"/>, fall back to the first SW format if no HW format is offered.</param>
        /// <returns>The HW config method that matched, or 0 if the codec does not support this device type.</returns>
        public int InitHWDeviceContext(HWDeviceContext device, bool fallbackToSw = false)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            GuardHWInit();
            if (pCodecContext->hw_device_ctx != null)
                return 0; // already initialised

            var chosen = PickHWConfig(device.Type);
            if (chosen == null) return 0;

            var cfg = chosen.Value;
            _hwPixFmt = cfg.pix_fmt;
            _hwFallbackToSw = fallbackToSw;

            if ((cfg.methods & AV_CODEC_HW_CONFIG_METHOD_HW_DEVICE_CTX) != 0)
            {
                AttachHWDeviceRef(device);
                WireGetFormat();
                return AV_CODEC_HW_CONFIG_METHOD_HW_DEVICE_CTX;
            }
            if ((cfg.methods & AV_CODEC_HW_CONFIG_METHOD_HW_FRAMES_CTX) != 0)
            {
                WireGetFormat();
                return AV_CODEC_HW_CONFIG_METHOD_HW_FRAMES_CTX;
            }
            return AV_CODEC_HW_CONFIG_METHOD_INTERNAL;
        }

        private void GuardHWInit()
        {
            if (pCodecContext == null) throw new InvalidOperationException("Codec context is not allocated.");
            if (pCodecContext->codec == null) throw new InvalidOperationException("Set the codec before calling InitHWDeviceContext.");
        }

        private AVCodecHWConfig? PickHWConfig(AVHWDeviceType? type)
        {
            const int wantedMask = AV_CODEC_HW_CONFIG_METHOD_HW_DEVICE_CTX
                                 | AV_CODEC_HW_CONFIG_METHOD_HW_FRAMES_CTX
                                 | AV_CODEC_HW_CONFIG_METHOD_INTERNAL;
            return new MediaCodec(pCodecContext->codec).GetHWConfigs()
                .Select(c => (AVCodecHWConfig?)c)
                .FirstOrDefault(c => (type == null || c.Value.device_type == type.Value) && (c.Value.methods & wantedMask) != 0);
        }

        private void WireGetFormat()
        {
            if (_getFormatDelegate == null)
            {
                _getFormatDelegate = GetFormatInstance;
                _getFormatHandle = System.Runtime.InteropServices.GCHandle.Alloc(_getFormatDelegate);
            }
            pCodecContext->get_format = _getFormatDelegate;
        }

        /// <summary>
        /// Attach an existing <see cref="HWDeviceContext"/> as this codec's hw_device_ctx (this context takes
        /// its own reference, so the wrapper may be disposed independently). Pure plumbing — decoders that need
        /// pixel-format negotiation should call <see cref="InitHWDeviceContext(HWDeviceContext, bool)"/> instead.
        /// </summary>
        public void AttachHWDevice(HWDeviceContext device)
        {
            if (pCodecContext == null) throw new InvalidOperationException("Codec context is not allocated.");
            if (device == null) throw new ArgumentNullException(nameof(device));
            AttachHWDeviceRef(device);
        }

        private void AttachHWDeviceRef(AVBufferRef* deviceRef)
        {
            if (pCodecContext->hw_device_ctx != null)
                ffmpeg.av_buffer_unref(&pCodecContext->hw_device_ctx);
            pCodecContext->hw_device_ctx = ffmpeg.av_buffer_ref(deviceRef);
        }

        /// <summary>
        /// Attach an existing <see cref="HWFramesContext"/> (required for HW encoding, optional for HW decoding).
        /// This context takes its own reference, so the wrapper may be disposed independently.
        /// </summary>
        public void AttachHWFramesContext(HWFramesContext frames)
        {
            if (pCodecContext == null) throw new InvalidOperationException("Codec context is not allocated.");
            if (frames == null) throw new ArgumentNullException(nameof(frames));
            if (pCodecContext->hw_frames_ctx != null)
                ffmpeg.av_buffer_unref(&pCodecContext->hw_frames_ctx);
            pCodecContext->hw_frames_ctx = ffmpeg.av_buffer_ref(frames);
        }

        /// <summary>The codec's hw_device_ctx as an owning wrapper (refcounted — dispose when done), or null if not set.</summary>
        public HWDeviceContext GetHWDevice()
            => pCodecContext == null || pCodecContext->hw_device_ctx == null
                ? null
                : new HWDeviceContext(pCodecContext->hw_device_ctx, takeRef: true);

        /// <summary>The codec's hw_frames_ctx as an owning wrapper (refcounted — dispose when done), or null if not set.</summary>
        public HWFramesContext GetHWFrames()
            => pCodecContext == null || pCodecContext->hw_frames_ctx == null
                ? null
                : new HWFramesContext(pCodecContext->hw_frames_ctx, takeRef: true);

        /// <summary>True when a hw_device_ctx is attached to this codec context.</summary>
        public bool HasHWDevice => pCodecContext != null && pCodecContext->hw_device_ctx != null;

        /// <summary>
        /// Transfer data from a HW frame to a SW frame (download), or from SW to HW (upload), depending on which side
        /// has hw_frames_ctx set.
        /// </summary>
        public static void HWFrameTransferData(MediaFrame dst, MediaFrame src, int flags = 0)
        {
            ffmpeg.av_hwframe_transfer_data(dst, src, flags).ThrowIfError();
        }

        /// <summary>
        /// Enumerate the pixel formats supported for hwframe transfer in the given direction for <paramref name="hwFrame"/>'s frames ctx.
        /// </summary>
        public static AVPixelFormat[] HWFrameTransferGetFormats(MediaFrame hwFrame, AVHWFrameTransferDirection direction)
        {
            if (hwFrame == null) throw new ArgumentNullException(nameof(hwFrame));
            AVFrame* f = hwFrame;
            if (f->hw_frames_ctx == null) return Array.Empty<AVPixelFormat>();
            AVPixelFormat* p = null;
            ffmpeg.av_hwframe_transfer_get_formats(f->hw_frames_ctx, direction, &p, 0).ThrowIfError();
            try
            {
                int n = 0;
                while (p[n] != AVPixelFormat.AV_PIX_FMT_NONE) n++;
                var arr = new AVPixelFormat[n];
                for (int i = 0; i < n; i++) arr[i] = p[i];
                return arr;
            }
            finally { ffmpeg.av_freep(&p); }
        }

        // Instance get_format callback: scan the offered formats, pick our HW one; optionally fall back to first SW format.
        private AVPixelFormat GetFormatInstance(AVCodecContext* avctx, AVPixelFormat* pix_fmts)
        {
            AVPixelFormat firstSw = AVPixelFormat.AV_PIX_FMT_NONE;
            for (AVPixelFormat* p = pix_fmts; *p != AVPixelFormat.AV_PIX_FMT_NONE; p++)
            {
                if (*p == _hwPixFmt) return *p;
                // Track first SW format as fallback candidate. SW formats are identified by absence of HWACCEL flag.
                if (firstSw == AVPixelFormat.AV_PIX_FMT_NONE)
                {
                    var desc = ffmpeg.av_pix_fmt_desc_get(*p);
                    if (desc != null && (desc->flags & ffmpeg.AV_PIX_FMT_FLAG_HWACCEL) == 0)
                        firstSw = *p;
                }
            }
            return _hwFallbackToSw && firstSw != AVPixelFormat.AV_PIX_FMT_NONE ? firstSw : AVPixelFormat.AV_PIX_FMT_NONE;
        }
    }
}
