using System;
using FFmpeg.AutoGen;

namespace FFmpegSharp
{
    public unsafe partial class OutputFormat
    {
        /// <summary>
        /// Be careful!!!
        /// </summary>
        protected AVOutputFormat* pOutputFormat = null;

        /// <summary>
        /// const AVOutputFormat*
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVOutputFormat*(OutputFormat value)
        {
            return value == null ? null : value.pOutputFormat;
        }

        public OutputFormat(AVOutputFormat* pAVOutputFormat)
        {
            pOutputFormat = pAVOutputFormat;
        }

        public OutputFormat(IntPtr pAVOutputFormat)
            : this((AVOutputFormat*)pAVOutputFormat)
        { }

        public AVOutputFormat Const => *pOutputFormat;

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
