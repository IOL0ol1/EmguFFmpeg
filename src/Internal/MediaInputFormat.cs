using System;
using FFmpeg.AutoGen.Abstractions;

namespace FFmpegSharp
{
    public unsafe partial class MediaInputFormat
    {
        /// <summary>
        /// Pointer to the underlying FFmpeg structure.
        /// WARNING: Be careful when accessing or modifying this field directly.
        /// </summary>
        protected AVInputFormat* pInputFormat = null;

        /// <summary>
        /// const AVInputFormat*
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVInputFormat*(MediaInputFormat value)
        {
            return value == null ? null : value.pInputFormat;
        }

        public MediaInputFormat(AVInputFormat* pAVInputFormat)
        {
            pInputFormat = pAVInputFormat;
        }

        public MediaInputFormat(IntPtr pAVInputFormat)
            : this((AVInputFormat*)pAVInputFormat)
        { }

        /// <summary>
        /// WARNING: Be careful when modifying this field directly.
        /// </summary>
        public ref AVInputFormat Ref => ref *pInputFormat;

    }
}
