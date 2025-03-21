using System;
using FFmpeg.AutoGen.Abstractions;

namespace FFmpegSharp
{
    public unsafe partial class MediaFilterContext
    {
        /// <summary>
        /// Pointer to the underlying FFmpeg structure.
        /// WARNING: Be careful when accessing or modifying this field directly.
        /// </summary>
        protected AVFilterContext* pFilterContext = null;

        /// <summary>
        /// const AVFilterContext*
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVFilterContext*(MediaFilterContext value)
        {
            return value == null ? null : value.pFilterContext;
        }

        public MediaFilterContext(AVFilterContext* pAVFilterContext)
        {
            pFilterContext = pAVFilterContext;
        }

        public MediaFilterContext(IntPtr pAVFilterContext)
            : this((AVFilterContext*)pAVFilterContext)
        { }

        /// <summary>
        /// WARNING: Be careful when modifying this field directly.
        /// </summary>
        public ref AVFilterContext Ref => ref *pFilterContext;

    }
}
