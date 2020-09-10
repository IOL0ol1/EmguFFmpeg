using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;
using System.Reflection.Emit;

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
        public string WrapperName => pCodec == null ? null : ((IntPtr)pCodec->wrapper_name).PtrToStringUTF8();
        public bool IsDecoder => pCodec == null ? false : ffmpeg.av_codec_is_decoder(pCodec) > 0;
        public bool IsEncoder => pCodec == null ? false : ffmpeg.av_codec_is_encoder(pCodec) > 0;

        #region safe wapper for IEnumerable
        protected static IntPtr av_codec_iterate_safe(IntPtr2Ptr opaque)
        {
            return (IntPtr)ffmpeg.av_codec_iterate(opaque);
        }

        protected static bool av_codec_is_decoder_safe(IntPtr codec)
        {
            return ffmpeg.av_codec_is_decoder((AVCodec*)codec) != 0;
        }

        protected static bool av_codec_is_encoder_safe(IntPtr codec)
        {
            return ffmpeg.av_codec_is_encoder((AVCodec*)codec) != 0;
        }
        #endregion

        #region Supported
        private void ThrowIfNull()
        {
            if (pCodec == null) throw new FFmpegException(FFmpegException.NullReference);
        }

        private static string av_get_profile_name_safe(MediaCodec codec, int i)
        {
            return ffmpeg.av_get_profile_name(codec, i);
        }


        public IEnumerable<string> Profiles
        {
            get
            {
                ThrowIfNull();
                string profile;
                for (int i = 0; (profile = av_get_profile_name_safe(this, i)) != null; i++)
                {
                    yield return profile;
                }
            }
        }

        private static AVCodecHWConfig? avcodec_get_hw_config_safe(MediaCodec codec, int i)
        {
            var ptr = ffmpeg.avcodec_get_hw_config(codec, i);
            return ptr != null ? *ptr : (AVCodecHWConfig?)null;
        }

        public IEnumerable<AVCodecHWConfig> SupportedHardware
        {
            get
            {
                ThrowIfNull();
                AVCodecHWConfig? config;
                for (int i = 0; (config = avcodec_get_hw_config_safe(this, i)) != null; i++)
                {
                    yield return config.Value;
                }
            }
        }

        private static AVPixelFormat? pix_fmts_next_safe(MediaCodec codec, int i)
        {
            var ptr = codec.pCodec->pix_fmts + i;
            return ptr != null ? *ptr : (AVPixelFormat?)null;
        }

        public IEnumerable<AVPixelFormat> SupportedPixelFmts
        {
            get
            {
                ThrowIfNull();
                AVPixelFormat? p;
                for (int i = 0; (p = pix_fmts_next_safe(this, i)) != null; i++)
                {
                    if (p == AVPixelFormat.AV_PIX_FMT_NONE)
                        yield break;
                    else
                        yield return p.Value;
                }
            }
        }

        private AVRational? supported_framerates_next_safe(MediaCodec codec, int i)
        {
            var ptr = codec.pCodec->supported_framerates + i;
            return ptr != null ? *ptr : (AVRational?)null;
        }

        public IEnumerable<AVRational> SupportedFrameRates
        {
            get
            {
                ThrowIfNull();
                AVRational? p;
                for (int i = 0; (p = supported_framerates_next_safe(this, i)) != null; i++)
                {
                    if (p.Value.num != 0)
                        yield return p.Value;
                    else
                        yield break;
                }
            }
        }

        private AVSampleFormat? sample_fmts_next_safe(MediaCodec codec, int i)
        {
            var ptr = codec.pCodec->sample_fmts + i;
            return ptr != null ? *ptr : (AVSampleFormat?)null;
        }


        public IEnumerable<AVSampleFormat> SupportedSampelFmts
        {
            get
            {
                ThrowIfNull();
                AVSampleFormat? p;
                for (int i = 0; (p = sample_fmts_next_safe(this, i)) != null; i++)
                {
                    if (p == AVSampleFormat.AV_SAMPLE_FMT_NONE)
                        yield break;
                    else
                        yield return p.Value;
                }
            }
        }

        private int? supported_samplerates_next_safe(MediaCodec codec, int i)
        {
            var ptr = codec.pCodec->supported_samplerates + i;
            return ptr != null ? *ptr : (int?)null;
        }

        public IEnumerable<int> SupportedSampleRates
        {
            get
            {
                ThrowIfNull();
                int? p;
                for (int i = 0; (p = supported_samplerates_next_safe(this, i)) != null; i++)
                {
                    if (p == 0)
                        yield break;
                    else
                        yield return p.Value;
                }
            }
        }
        private ulong? channel_layouts_next_safe(MediaCodec codec, int i)
        {
            var ptr = codec.pCodec->channel_layouts + i;
            return ptr != null ? *ptr : (ulong?)null;
        }

        public IEnumerable<ulong> SupportedChannelLayout
        {
            get
            {
                ThrowIfNull();
                ulong? p;
                for (int i = 0; (p = channel_layouts_next_safe(this, i)) != null; i++)
                {
                    if (p == 0)
                        yield break;
                    else
                        yield return p.Value;
                }
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
