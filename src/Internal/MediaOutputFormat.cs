using System;
using FFmpeg.AutoGen.Abstractions;

namespace FFmpegSharp
{
    public unsafe partial class MediaOutputFormat
    {
        /// <summary>
        /// Pointer to the underlying FFmpeg structure.
        /// WARNING: Be careful when accessing or modifying this field directly.
        /// </summary>
        protected AVOutputFormat* pOutputFormat = null;

        /// <summary>
        /// const AVOutputFormat*
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVOutputFormat*(MediaOutputFormat value)
        {
            return value == null ? null : value.pOutputFormat;
        }

        public MediaOutputFormat(AVOutputFormat* pAVOutputFormat)
        {
            pOutputFormat = pAVOutputFormat;
        }

        public MediaOutputFormat(IntPtr pAVOutputFormat)
            : this((AVOutputFormat*)pAVOutputFormat)
        { }

        /// <summary>
        /// WARNING: Be careful when modifying this field directly.
        /// </summary>
        public ref AVOutputFormat Ref => ref *pOutputFormat;

    }
}
