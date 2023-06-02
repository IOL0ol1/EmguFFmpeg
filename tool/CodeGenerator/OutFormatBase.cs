using FFmpeg.AutoGen;
namespace FFmpegSharp.Internal
{
    public abstract unsafe partial class OutFormatBase
    {
        /// <summary>
        /// Be careful!!!
        /// </summary>
        protected internal AVOutputFormat* pOutputFormat = null;

        /// <summary>
        /// const AVOutputFormat*
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVOutputFormat*(OutFormatBase value)
        {
            if (value == null) return null;
            return value.pOutputFormat;
        }

        public OutFormatBase(AVOutputFormat* value)
        {
            pOutputFormat = value;
        }

        public AVOutputFormat Ref => *pOutputFormat;

        public AVCodecID AudioCodec
        {
            get => pOutputFormat->audio_codec;
            set => pOutputFormat->audio_codec = value;
        }

        public AVCodecID VideoCodec
        {
            get => pOutputFormat->video_codec;
            set => pOutputFormat->video_codec = value;
        }

        public AVCodecID SubtitleCodec
        {
            get => pOutputFormat->subtitle_codec;
            set => pOutputFormat->subtitle_codec = value;
        }

        public int Flags
        {
            get => pOutputFormat->flags;
            set => pOutputFormat->flags = value;
        }

    }
}
