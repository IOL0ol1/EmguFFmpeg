using System;
using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    public unsafe abstract class MediaDecoder
    {
        /// <summary>
        /// Create <see cref="AVCodecContext"/> by <see cref="AVCodecParameters"/>.
        /// <para>
        /// <seealso cref="MediaCodecContext.Open(Action{MediaCodecContextSettings}, MediaCodec, MediaDictionary)"/>
        /// </para>
        /// <para>
        /// <seealso cref="ffmpeg.avcodec_parameters_to_context(AVCodecContext*, AVCodecParameters*)"/>
        /// </para>
        /// </summary>
        /// <param name="codecParameters"></param>
        /// <returns></returns>
        public static MediaCodecContext CreateDecodecContext(AVCodecParameters codecParameters)
        {
            var codec = MediaCodec.GetDecoder(codecParameters.codec_id);
            AVCodecParameters* pCodecParameters = &codecParameters;
            // If codec_id is AV_CODEC_ID_NONE return null
            return codec == null
                ? null
                : new MediaCodecContext(codec)
                    .Open(_ => ffmpeg.avcodec_parameters_to_context(_, pCodecParameters).ThrowIfError());
        }
 
        /// <summary>
        /// sameler as <see cref="CreateDecodecContext(AVCodecParameters)"/>
        /// </summary>
        /// <param name="pCodecParameters"><see cref="AVCodecContext"/>*</param>
        /// <returns></returns>
        public static MediaCodecContext CreateDecoderByCodecpar(IntPtr pCodecParameters)
        {
            return CreateDecodecContext(*(AVCodecParameters*)pCodecParameters);
        }
    }
}
