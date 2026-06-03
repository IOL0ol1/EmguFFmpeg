using System;
using System.Collections.Generic;

using FFmpeg.AutoGen;


namespace FFmpeg.Sharp
{
    public unsafe partial class MediaCodec
    {

        /// <summary>
        /// Get <see cref="MediaCodec"/> by <see cref="ffmpeg.avcodec_find_encoder_by_name(string)"/>
        /// </summary>
        /// <param name="codecName"></param>
        /// <returns></returns>
        public static MediaCodec FindEncoder(string codecName)
        {
            AVCodec* pCodec = ffmpeg.avcodec_find_encoder_by_name(codecName);
            return pCodec == null ? null : new MediaCodec(pCodec);
        }

        /// <summary>
        /// Get <see cref="MediaCodec"/> by <see cref="ffmpeg.avcodec_find_encoder(AVCodecID)"/>
        /// </summary>
        /// <param name="codecId"></param>
        /// <returns></returns>

        public static MediaCodec FindEncoder(AVCodecID codecId)
        {
            AVCodec* pCodec = ffmpeg.avcodec_find_encoder(codecId);
            return pCodec == null ? null : new MediaCodec(pCodec);
        }

        /// <summary>
        /// Get <see cref="MediaCodec"/> by <see cref="ffmpeg.avcodec_find_decoder_by_name(string)"/>
        /// </summary>
        /// <param name="codecName"></param>
        /// <returns></returns>
        public static MediaCodec FindDecoder(string codecName)
        {
            AVCodec* pCodec = ffmpeg.avcodec_find_decoder_by_name(codecName);
            return pCodec == null ? null : new MediaCodec(pCodec);
        }

        /// <summary>
        /// Get <see cref="MediaCodec"/> by <see cref="ffmpeg.avcodec_find_decoder(AVCodecID)"/>
        /// </summary>
        /// <param name="codecId"></param>
        /// <returns></returns>

        public static MediaCodec FindDecoder(AVCodecID codecId)
        {
            AVCodec* pCodec = ffmpeg.avcodec_find_decoder(codecId);
            return pCodec == null ? null : new MediaCodec(pCodec);
        }


        public string Name => ((IntPtr)pCodec->name).PtrToStringUTF8();
        public string LongName => ((IntPtr)pCodec->long_name).PtrToStringUTF8();
        public string WrapperName => ((IntPtr)pCodec->wrapper_name).PtrToStringUTF8();
        public bool IsDecoder => ffmpeg.av_codec_is_decoder(pCodec) != 0;
        public bool IsEncoder => ffmpeg.av_codec_is_encoder(pCodec) != 0;

        protected static IntPtr av_codec_iterate_safe(IntPtrRef opaque)
        {
            fixed (void** pp = &opaque.IntPtr)
                return (IntPtr)ffmpeg.av_codec_iterate(pp);
        }

        /// <summary>
        /// Get all supported codec
        /// </summary>
        public static IEnumerable<MediaCodec> GetCodecs()
        {
            IntPtr pCodec;
            IntPtrRef opaque = new IntPtrRef();
            while ((pCodec = av_codec_iterate_safe(opaque)) != IntPtr.Zero)
            {
                yield return new MediaCodec(pCodec);
            }
        }

        #region Supported

        protected static KeyValuePair<int, string>? av_get_profile_name_safe(MediaCodec codec, int i)
        {
            var ptr = codec.pCodec->profiles + i;
            return ptr != null ?
                new KeyValuePair<int, string>(ptr->profile, ((IntPtr)ptr->name).PtrToStringUTF8()) :
                (KeyValuePair<int, string>?)null;
        }

        public IEnumerable<KeyValuePair<int, string>> GetProfiles()
        {
            KeyValuePair<int, string>? profile;
            for (int i = 0; (profile = av_get_profile_name_safe(this, i)) != null; i++)
            {
                if (profile.Value.Key == ffmpeg.AV_PROFILE_UNKNOWN)
                    yield break;
                else
                    yield return profile.Value;
            }
        }

        protected static AVCodecHWConfig? avcodec_get_hw_config_safe(MediaCodec codec, int i)
        {
            var ptr = ffmpeg.avcodec_get_hw_config(codec, i);
            return ptr != null ? *ptr : (AVCodecHWConfig?)null;
        }

        public IEnumerable<AVCodecHWConfig> GetHWConfigs()
        {
            AVCodecHWConfig? config;
            for (int i = 0; (config = avcodec_get_hw_config_safe(this, i)) != null; i++)
            {
                yield return config.Value;
            }
        }

        /// <summary>
        /// Query supported configs of type <typeparamref name="T"/> via
        /// <see cref="ffmpeg.avcodec_get_supported_config"/>. Returns an empty array when
        /// the codec accepts any value (FFmpeg sets out_configs to NULL in that case) or
        /// when the call fails.
        /// </summary>
        protected T[] GetSupportedConfig<T>(AVCodecConfig config) where T : unmanaged
        {
            void* outConfigs;
            int numConfigs;
            int ret = ffmpeg.avcodec_get_supported_config(null, pCodec, config, 0, &outConfigs, &numConfigs);
            if (ret < 0 || outConfigs == null || numConfigs <= 0)
                return Array.Empty<T>();
            var arr = new T[numConfigs];
            var p = (T*)outConfigs;
            for (int i = 0; i < numConfigs; i++)
                arr[i] = p[i];
            return arr;
        }

        /// <summary>
        /// List of supported pixel formats. Replaces the deprecated <c>AVCodec.pix_fmts</c> field.
        /// </summary>
        public IEnumerable<AVPixelFormat> GetPixelFmts()
            => GetSupportedConfig<AVPixelFormat>(AVCodecConfig.AV_CODEC_CONFIG_PIX_FORMAT);

        /// <summary>
        /// List of supported frame rates. Replaces the deprecated <c>AVCodec.supported_framerates</c> field.
        /// </summary>
        public IEnumerable<AVRational> GetSupportedFramerates()
            => GetSupportedConfig<AVRational>(AVCodecConfig.AV_CODEC_CONFIG_FRAME_RATE);

        /// <summary>
        /// List of supported sample formats. Replaces the deprecated <c>AVCodec.sample_fmts</c> field.
        /// </summary>
        public IEnumerable<AVSampleFormat> GetSampelFmts()
            => GetSupportedConfig<AVSampleFormat>(AVCodecConfig.AV_CODEC_CONFIG_SAMPLE_FORMAT);

        /// <summary>
        /// List of supported sample rates. Replaces the deprecated <c>AVCodec.supported_samplerates</c> field.
        /// </summary>
        public IEnumerable<int> GetSupportedSamplerates()
            => GetSupportedConfig<int>(AVCodecConfig.AV_CODEC_CONFIG_SAMPLE_RATE);

        /// <summary>
        /// List of supported channel layouts. Replaces the deprecated <c>AVCodec.ch_layouts</c> field.
        /// </summary>
        public IEnumerable<AVChannelLayout> GetChLayouts()
            => GetSupportedConfig<AVChannelLayout>(AVCodecConfig.AV_CODEC_CONFIG_CHANNEL_LAYOUT);

        #endregion Supported

        public override string ToString()
        {
            return $"[{Name}]{LongName}";
        }
    }
}
