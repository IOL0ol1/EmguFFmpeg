using System;
using FFmpeg.AutoGen.Abstractions;

namespace FFmpegSharp
{
    public unsafe partial class MediaFilter
    {
        /// <summary>
        /// Pointer to the underlying FFmpeg structure.
        /// WARNING: Be careful when accessing or modifying this field directly.
        /// </summary>
        protected AVFilter* pFilter = null;

        /// <summary>
        /// const AVFilter*
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVFilter*(MediaFilter value)
        {
            return value == null ? null : value.pFilter;
        }

        public MediaFilter(AVFilter* pAVFilter)
        {
            pFilter = pAVFilter;
        }

        public MediaFilter(IntPtr pAVFilter)
            : this((AVFilter*)pAVFilter)
        { }

        /// <summary>
        /// WARNING: Be careful when modifying this field directly.
        /// </summary>
        public ref AVFilter Ref => ref *pFilter;

    }
}
