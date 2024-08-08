using System;
using FFmpeg.AutoGen;

namespace FFmpegSharp
{
    public unsafe partial class InputFormat
    {
        /// <summary>
        /// Be careful!!!
        /// </summary>
        protected internal AVInputFormat* pInputFormat = null;

        /// <summary>
        /// const AVInputFormat*
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVInputFormat*(InputFormat value)
        {
            return value == null ? null : value.pInputFormat;
        }

        public InputFormat(AVInputFormat* pAVInputFormat)
        {
            pInputFormat = pAVInputFormat;
        }

        public InputFormat(IntPtr pAVInputFormat)
            : this((AVInputFormat*)pAVInputFormat)
        { }

        public AVInputFormat Const => *pInputFormat;

        public int Flags
        {
            get => pInputFormat->flags;
            set => pInputFormat->flags = value;
        }

    }
}
