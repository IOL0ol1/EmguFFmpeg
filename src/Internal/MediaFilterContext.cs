using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    public unsafe partial class MediaFilterContext
    {
        /// <summary>
        /// Be careful!!!
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
        /// Direct access to the underlying struct fields.
        /// WARNING: Be careful when modifying. Tracks AVFilterContext struct evolution upstream.
        /// </summary>
        public ref AVFilterContext Ref => ref *pFilterContext;
    }
}
