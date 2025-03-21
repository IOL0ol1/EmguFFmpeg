using System;
using FFmpeg.AutoGen.Abstractions;

namespace FFmpegSharp
{
    public unsafe partial class MediaFormatContext
    {
        /// <summary>
        /// Pointer to the underlying FFmpeg structure.
        /// WARNING: Be careful when accessing or modifying this field directly.
        /// </summary>
        protected AVFormatContext* pFormatContext = null;

        /// <summary>
        /// const AVFormatContext*
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVFormatContext*(MediaFormatContext value)
        {
            return value == null ? null : value.pFormatContext;
        }

        public MediaFormatContext(AVFormatContext* pAVFormatContext)
        {
            pFormatContext = pAVFormatContext;
        }

        public MediaFormatContext(IntPtr pAVFormatContext)
            : this((AVFormatContext*)pAVFormatContext)
        { }

        /// <summary>
        /// WARNING: Be careful when modifying this field directly.
        /// </summary>
        public ref AVFormatContext Ref => ref *pFormatContext;

    }
}
