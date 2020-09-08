using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;

namespace EmguFFmpeg
{
    /// <summary>
    /// <see cref="AVCodec"/> and <see cref="AVCodecContext"/> wapper
    /// </summary>
    public unsafe abstract class MediaCodec : IDisposable
    {
        protected AVCodec* pCodec = null;

        protected AVCodecContext* pCodecContext = null;

        public abstract int Initialize(Action<MediaCodec> setBeforeOpen = null, int flags = 0, MediaDictionary opts = null);

        /// <summary>
        /// Get value if <see cref="Id"/> is not <see cref="AVCodecID.AV_CODEC_ID_NONE"/>
        /// </summary>
        /// <exception cref="FFmpegException"/>
        public AVCodec AVCodec => pCodec == null ? throw new FFmpegException(FFmpegException.NullReference) : *pCodec;

        /// <summary>
        /// Get value after <see cref="Initialize(Action{MediaCodec}, int, MediaDictionary)"/>
        /// </summary>
        /// <exception cref="FFmpegException"/>
        public AVCodecContext AVCodecContext => pCodecContext == null ? throw new FFmpegException(FFmpegException.NullReference) : *pCodecContext;

        public AVMediaType Type => pCodec == null ? AVMediaType.AVMEDIA_TYPE_UNKNOWN : pCodec->type;
        public AVCodecID Id => pCodec == null ? AVCodecID.AV_CODEC_ID_NONE : pCodec->id;
        public string Name => pCodec == null ? null : ((IntPtr)pCodec->name).PtrToStringUTF8();
        public string LongName => pCodec == null ? null : ((IntPtr)pCodec->long_name).PtrToStringUTF8();
        public bool IsDecoder => pCodec == null ? false : ffmpeg.av_codec_is_decoder(pCodec) > 0;
        public bool IsEncoder => pCodec == null ? false : ffmpeg.av_codec_is_encoder(pCodec) > 0;

        #region safe wapper for IEnumerable
        protected static IntPtr CodecIterate(IntPtr2Ptr opaque)
        {
            return (IntPtr)ffmpeg.av_codec_iterate(opaque);
        }

        protected static bool CodecIsDecoder(IntPtr codec)
        {
            return ffmpeg.av_codec_is_decoder((AVCodec*)codec) != 0;
        }

        protected static bool CodecIsEncoder(IntPtr codec)
        {
            return ffmpeg.av_codec_is_encoder((AVCodec*)codec) != 0;
        }
        #endregion

        #region Supported

        public AVCodecHWConfig[] SupportedHardware
        {
            get
            {
                List<AVCodecHWConfig> result = new List<AVCodecHWConfig>();
                if (pCodec == null) return result.ToArray();
                for (int i = 0; ; i++)
                {
                    AVCodecHWConfig* config = ffmpeg.avcodec_get_hw_config(pCodec, i);
                    if (config == null)
                        return result.ToArray();
                    result.Add(*config);
                }
            }
        }

        public AVPixelFormat[] SupportedPixelFmts
        {
            get
            {
                List<AVPixelFormat> result = new List<AVPixelFormat>();
                if (pCodec == null) return result.ToArray();
                AVPixelFormat* p = pCodec->pix_fmts;
                if (p != null)
                {
                    while (*p != AVPixelFormat.AV_PIX_FMT_NONE)
                    {
                        result.Add(*p);
                        p++;
                    }
                }
                return result.ToArray();
            }
        }

        public AVRational[] SupportedFrameRates
        {
            get
            {
                List<AVRational> result = new List<AVRational>();
                if (pCodec == null) return result.ToArray();
                AVRational* p = pCodec->supported_framerates;
                if (p != null)
                {
                    while (p->num != 0)
                    {
                        result.Add(*p);
                        p++;
                    }
                }
                return result.ToArray();
            }
        }

        public AVSampleFormat[] SupportedSampelFmts
        {
            get
            {
                List<AVSampleFormat> result = new List<AVSampleFormat>();
                if (pCodec == null) return result.ToArray();
                AVSampleFormat* p = pCodec->sample_fmts;
                if (p != null)
                {
                    while (*p != AVSampleFormat.AV_SAMPLE_FMT_NONE)
                    {
                        result.Add(*p);
                        p++;
                    }
                }
                return result.ToArray();
            }
        }

        public int[] SupportedSampleRates
        {
            get
            {
                List<int> result = new List<int>();
                if (pCodec == null) return result.ToArray();
                int* p = pCodec->supported_samplerates;
                if (p != null)
                {
                    while (*p != 0)
                    {
                        result.Add(*p);
                        p++;
                    }
                }
                return result.ToArray();
            }
        }

        public ulong[] SupportedChannelLayout
        {
            get
            {
                List<ulong> result = new List<ulong>();
                if (pCodec == null) return result.ToArray();
                ulong* p = pCodec->channel_layouts;
                if (p != null)
                {
                    while (*p != 0)
                    {
                        result.Add(*p);
                        p++;
                    }
                }
                return result.ToArray();
            }
        }

        #endregion

        public override string ToString()
        {
            return Name;
        }

        public static implicit operator AVCodec*(MediaCodec value)
        {
            if (value == null) return null;
            return value.pCodec;
        }

        public static implicit operator AVCodecContext*(MediaCodec value)
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
            if (!disposedValue)
            {
                fixed (AVCodecContext** ppCodecContext = &pCodecContext)
                {
                    ffmpeg.avcodec_free_context(ppCodecContext);
                }
                disposedValue = true;
            }
        }

        #endregion
    }
}
