using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using FFmpegSharp.Internal;

namespace FFmpegSharp
{
    public unsafe partial class MediaCodecContext : MediaCodecContextBase, IDisposable
    {
        protected bool disposedValue;

        public static MediaCodecContext Create(MediaCodec codec, Action<MediaCodecContextBase> beforeOpenSetting, MediaDictionary opts = null)
        {
            return new MediaCodecContext(codec).Open(beforeOpenSetting, null, opts);
        }

        public MediaCodecContext(AVCodecContext* pAVCodecContext, bool isDisposeByOwner = true)
            : base(pAVCodecContext)
        {
            disposedValue = !isDisposeByOwner;
        }

        public MediaCodecContext(MediaCodec codec = null)
                    : this(ffmpeg.avcodec_alloc_context3(codec), true)
        { }

        /// <summary>
        /// <see cref="ffmpeg.avcodec_open2(AVCodecContext*, AVCodec*, AVDictionary**)"/>
        /// </summary>
        /// <param name="beforeOpenSetting"></param>
        /// <param name="codec"></param>
        /// <param name="opts"></param>
        public virtual MediaCodecContext Open(Action<MediaCodecContextBase> beforeOpenSetting = null, MediaCodec codec = null, MediaDictionary opts = null)
        {
            beforeOpenSetting?.Invoke(this);
            ffmpeg.avcodec_open2(pCodecContext, codec, opts).ThrowIfError();
            return this;
        }

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
                    disposedValue = true;
                }
            }
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
    }
}

namespace FFmpegSharp.Internal
{
    public unsafe partial class MediaCodecContextBase
    {
        /// 
        /// The codec supports this format via the hw_device_ctx interface.
        /// 
        /// When selecting this format, AVCodecContext.hw_device_ctx should
        /// have been set to a device of the specified type before calling
        /// avcodec_open2().
        /// 
        private const int AV_CODEC_HW_CONFIG_METHOD_HW_DEVICE_CTX = 0x01;
        /// The codec supports this format via the hw_frames_ctx interface.
        /// 
        /// When selecting this format for a decoder,
        /// AVCodecContext.hw_frames_ctx should be set to a suitable frames
        /// context inside the get_format() callback.  The frames context
        /// must have been created on a device of the specified type.
        /// 
        /// When selecting this format for an encoder,
        /// AVCodecContext.hw_frames_ctx should be set to the context which
        /// will be used for the input frames before calling avcodec_open2().
        private const int AV_CODEC_HW_CONFIG_METHOD_HW_FRAMES_CTX = 0x02;
        /// 
        /// The codec supports this format by some internal method.
        /// 
        /// This format can be selected without any additional configuration -
        /// no device or frames context is required.
        /// 
        private const int AV_CODEC_HW_CONFIG_METHOD_INTERNAL = 0x04;
        /// 
        /// The codec supports this format by some ad-hoc method.
        /// 
        /// Additional settings and/or function calls are required.  See the
        /// codec-specific documentation for details.  (Methods requiring
        /// this sort of configuration are deprecated and others should be
        /// used in preference.)
        /// 
        private const int AV_CODEC_HW_CONFIG_METHOD_AD_HOC = 0x08;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="device"></param>
        /// <param name="opts"></param>
        /// <param name="flags"></param>
        public int InitHWDeviceContext(AVHWDeviceType type, string device = null, MediaDictionary opts = null, int flags = 0)
        {
            if (pCodecContext->codec != null && pCodecContext->hw_device_ctx == null)
            {
                var codec = new MediaCodec(pCodecContext->codec);

                if (codec.GetHWConfigs().Select(_ => (AVCodecHWConfig?)_)
                    .FirstOrDefault(_ => _.Value.device_type == type && (_.Value.methods & AV_CODEC_HW_CONFIG_METHOD_HW_DEVICE_CTX) != 0) is AVCodecHWConfig hWConfig)
                {
                    ffmpeg.av_hwdevice_ctx_create(&pCodecContext->hw_device_ctx, type, device, opts, flags).ThrowIfError();
                    pCodecContext->get_format = (AVCodecContext_get_format_func)((avctx, pix_fmts) => GetFormat(avctx, pix_fmts, hWConfig.pix_fmt));
                }
            }
            return IsHWDeviceCtxInit() ? AV_CODEC_HW_CONFIG_METHOD_HW_DEVICE_CTX : 0;
        }

        public int InitHWDeviceContext(string typeName, string device = null, MediaDictionary opts = null, int flags = 0)
        {
            var type = ffmpeg.av_hwdevice_find_type_by_name(typeName);
            if (type != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE && pCodecContext->codec != null && pCodecContext->hw_device_ctx == null)
            {
                var codec = new MediaCodec(pCodecContext->codec);

                if (codec.GetHWConfigs().Select(_ => (AVCodecHWConfig?)_)
                    .FirstOrDefault(_ => _.Value.device_type == type && (_.Value.methods & AV_CODEC_HW_CONFIG_METHOD_HW_DEVICE_CTX) != 0) is AVCodecHWConfig hWConfig)
                {
                    ffmpeg.av_hwdevice_ctx_create(&pCodecContext->hw_device_ctx, type, device, opts, flags).ThrowIfError();
                    pCodecContext->get_format = (AVCodecContext_get_format_func)((avctx, pix_fmts) => GetFormat(avctx, pix_fmts, hWConfig.pix_fmt));
                }
            }
            return IsHWDeviceCtxInit() ? AV_CODEC_HW_CONFIG_METHOD_HW_DEVICE_CTX : 0;
        }

        protected bool IsHWDeviceCtxInit() => pCodecContext->hw_device_ctx != null;

        protected void HWFrameTransferData(MediaFrame dst,MediaFrame src,int flags = 0)
        {
            ffmpeg.av_hwframe_transfer_data(dst, src, flags).ThrowIfError();
        }

        private AVPixelFormat GetFormat(AVCodecContext* avctx, AVPixelFormat* pix_fmts, AVPixelFormat hw_pix_fmts)
        {
            while (*pix_fmts != AVPixelFormat.AV_PIX_FMT_NONE)
            {
                if (*pix_fmts == hw_pix_fmts)
                {
                    return *pix_fmts;
                }
                pix_fmts++;
            }
            return AVPixelFormat.AV_PIX_FMT_NONE;
        }

    }



}

