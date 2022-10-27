using FFmpeg.AutoGen;
namespace FFmpegSharp.Internal
{
    public abstract unsafe partial class OutFormatBase
    {
        protected AVOutputFormat* pOutputFormat = null;

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

        public int PrivDataSize
        {
            get => pOutputFormat->priv_data_size;
            set => pOutputFormat->priv_data_size = value;
        }

        public int FlagsInternal
        {
            get => pOutputFormat->flags_internal;
            set => pOutputFormat->flags_internal = value;
        }

        public AVCodecID DataCodec
        {
            get => pOutputFormat->data_codec;
            set => pOutputFormat->data_codec = value;
        }

    }
}
