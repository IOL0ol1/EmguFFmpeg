using System;
using FFmpeg.AutoGen.Abstractions;

namespace FFmpegSharp
{
    public unsafe partial class OutputFormat
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

        /// <summary>
        /// WARNING: Be careful when modifying this field directly.
        /// </summary>
        public ref AVOutputFormat Ref => ref *pOutputFormat;

    }
}
