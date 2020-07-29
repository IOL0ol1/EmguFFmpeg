using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace EmguFFmpeg
{

    public abstract class MediaCodec : IDisposable
    {
        protected unsafe AVCodec* pCodec = null;

        protected unsafe AVCodecContext* pCodecContext = null;

        public abstract void Initialize(Action<MediaCodec> setBeforeOpen = null, int flags = 0, MediaDictionary opts = null);

        /// <summary>
        /// Get value if <see cref="Id"/> is not <see cref="AVCodecID.AV_CODEC_ID_NONE"/>
        /// </summary>
        /// <exception cref="FFmpegException"/>
        public AVCodec AVCodec { get { unsafe { return pCodec == null ? throw new FFmpegException(FFmpegException.NullReference) : *pCodec; } } }

        /// <summary>
        /// Get value after <see cref="Initialize(Action{MediaCodec}, int, MediaDictionary)"/>
        /// </summary>
        /// <exception cref="FFmpegException"/>
        public AVCodecContext AVCodecContext { get { unsafe { return pCodecContext == null ? throw new FFmpegException(FFmpegException.NullReference) : *pCodecContext; } } }

        public AVMediaType Type { get { unsafe { return pCodec == null ? AVMediaType.AVMEDIA_TYPE_UNKNOWN : pCodec->type; } } }
        public AVCodecID Id { get { unsafe { return pCodec == null ? AVCodecID.AV_CODEC_ID_NONE : pCodec->id; } } }
        public string Name { get { unsafe { return pCodec == null ? null : ((IntPtr)pCodec->name).PtrToStringUTF8(); } } }
        public string LongName { get { unsafe { return pCodec == null ? null : ((IntPtr)pCodec->long_name).PtrToStringUTF8(); } } }
        public bool IsDecoder { get { unsafe { return pCodec == null ? false : ffmpeg.av_codec_is_decoder(pCodec) > 0; } } }
        public bool IsEncoder { get { unsafe { return pCodec == null ? false : ffmpeg.av_codec_is_encoder(pCodec) > 0; } } }

        #region Supported

        public List<AVCodecHWConfig> SupportedHardware
        {
            get
            {
                unsafe
                {
                    List<AVCodecHWConfig> result = new List<AVCodecHWConfig>();
                    if (pCodec == null) return result;
                    for (int i = 0; ; i++)
                    {
                        AVCodecHWConfig* config = ffmpeg.avcodec_get_hw_config(pCodec, i);
                        if (config == null)
                            return result;
                        result.Add(*config);
                    }
                }
            }
        }

        public List<AVPixelFormat> SupportedPixelFmts
        {
            get
            {
                unsafe
                {
                    List<AVPixelFormat> result = new List<AVPixelFormat>();
                    if (pCodec == null) return result;
                    AVPixelFormat* p = pCodec->pix_fmts;
                    if (p != null)
                    {
                        while (*p != AVPixelFormat.AV_PIX_FMT_NONE)
                        {
                            result.Add(*p);
                            p++;
                        }
                    }
                    return result;
                }
            }
        }

        public List<AVRational> SupportedFrameRates
        {
            get
            {
                unsafe
                {
                    List<AVRational> result = new List<AVRational>();
                    if (pCodec == null) return result;
                    AVRational* p = pCodec->supported_framerates;
                    if (p != null)
                    {
                        while (p->num != 0)
                        {
                            result.Add(*p);
                            p++;
                        }
                    }
                    return result;
                }
            }
        }

        public List<AVSampleFormat> SupportedSampelFmts
        {
            get
            {
                unsafe
                {
                    List<AVSampleFormat> result = new List<AVSampleFormat>();
                    if (pCodec == null) return result;
                    AVSampleFormat* p = pCodec->sample_fmts;
                    if (p != null)
                    {
                        while (*p != AVSampleFormat.AV_SAMPLE_FMT_NONE)
                        {
                            result.Add(*p);
                            p++;
                        }
                    }
                    return result;
                }
            }
        }

        public List<int> SupportedSampleRates
        {
            get
            {
                unsafe
                {
                    List<int> result = new List<int>();
                    if (pCodec == null) return result;
                    int* p = pCodec->supported_samplerates;
                    if (p != null)
                    {
                        while (*p != 0)
                        {
                            result.Add(*p);
                            p++;
                        }
                    }
                    return result;
                }
            }
        }

        public List<ulong> SupportedChannelLayout
        {
            get
            {
                unsafe
                {
                    List<ulong> result = new List<ulong>();
                    if (pCodec == null) return result;
                    ulong* p = pCodec->channel_layouts;
                    if (p != null)
                    {
                        while (*p != 0)
                        {
                            result.Add(*p);
                            p++;
                        }
                    }
                    return result;
                }
            }
        }

        #endregion

        public override string ToString()
        {
            return Name;
        }

        public unsafe static implicit operator AVCodec*(MediaCodec value)
        {
            if (value == null) return null;
            return value.pCodec;
        }

        public unsafe static implicit operator AVCodecContext*(MediaCodec value)
        {
            if (value == null) return null;
            return value.pCodecContext;
        }

        #region IDisposable Support

        private bool disposedValue = false;

        ~MediaCodec()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            unsafe
            {
                if (!disposedValue)
                {
                    fixed (AVCodecContext** ppCodecContext = &pCodecContext)
                    {
                        ffmpeg.avcodec_free_context(ppCodecContext);
                    }
                    disposedValue = true;
                }
            }
        }

        #endregion
    }
}