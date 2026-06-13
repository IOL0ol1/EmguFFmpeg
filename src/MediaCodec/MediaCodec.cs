using System;
using System.Collections.Generic;
using System.Linq;

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


        public AVCodecID Id => pCodec->id;
        public string Name => ((IntPtr)pCodec->name).PtrToStringUTF8();
        public string LongName => ((IntPtr)pCodec->long_name).PtrToStringUTF8();
        public string WrapperName => ((IntPtr)pCodec->wrapper_name).PtrToStringUTF8();
        public bool IsDecoder => ffmpeg.av_codec_is_decoder(pCodec) != 0;
        public bool IsEncoder => ffmpeg.av_codec_is_encoder(pCodec) != 0;

        /// <summary>
        /// Get all supported codec
        /// </summary>
        public static IEnumerable<MediaCodec> GetCodecs()
            => NativeIterate.Cursor(o => (IntPtr)ffmpeg.av_codec_iterate(o), p => new MediaCodec(p));

        #region Supported

        public IEnumerable<KeyValuePair<int, string>> GetProfiles()
            => NativeIterate.Indexed(
                    i => pCodec->profiles == null ? IntPtr.Zero : (IntPtr)(pCodec->profiles + i),
                    p => *(AVProfile*)p)
                .TakeWhile(_ => _.profile != ffmpeg.AV_PROFILE_UNKNOWN)
                .Select(_ => new KeyValuePair<int, string>(_.profile, ((IntPtr)_.name).PtrToStringUTF8()));

        public IEnumerable<AVCodecHWConfig> GetHWConfigs()
            => NativeIterate.Indexed(
                i => (IntPtr)ffmpeg.avcodec_get_hw_config(this, i),
                p => *(AVCodecHWConfig*)p);

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
        public AVPixelFormat[] GetPixelFmts()
            => GetSupportedConfig<AVPixelFormat>(AVCodecConfig.AV_CODEC_CONFIG_PIX_FORMAT);

        /// <summary>
        /// List of supported frame rates. Replaces the deprecated <c>AVCodec.supported_framerates</c> field.
        /// </summary>
        public AVRational[] GetSupportedFramerates()
            => GetSupportedConfig<AVRational>(AVCodecConfig.AV_CODEC_CONFIG_FRAME_RATE);

        /// <summary>
        /// List of supported sample formats. Replaces the deprecated <c>AVCodec.sample_fmts</c> field.
        /// </summary>
        public AVSampleFormat[] GetSampleFormats()
            => GetSupportedConfig<AVSampleFormat>(AVCodecConfig.AV_CODEC_CONFIG_SAMPLE_FORMAT);

        /// <summary>
        /// List of supported sample rates. Replaces the deprecated <c>AVCodec.supported_samplerates</c> field.
        /// </summary>
        public int[] GetSupportedSamplerates()
            => GetSupportedConfig<int>(AVCodecConfig.AV_CODEC_CONFIG_SAMPLE_RATE);

        /// <summary>
        /// List of supported channel layouts. Replaces the deprecated <c>AVCodec.ch_layouts</c> field.
        /// </summary>
        public AVChannelLayout[] GetChLayouts()
            => GetSupportedConfig<AVChannelLayout>(AVCodecConfig.AV_CODEC_CONFIG_CHANNEL_LAYOUT);

        #endregion Supported

        public override string ToString()
        {
            return $"[{Name}]{LongName}";
        }
    }
}
