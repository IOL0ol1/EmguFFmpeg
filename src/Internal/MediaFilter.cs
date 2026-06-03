using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    public unsafe partial class MediaFilter
    {
        /// <summary>
        /// Be careful!!!
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
        /// Direct access to the underlying struct fields.
        /// WARNING: Be careful when modifying. Tracks AVFilter struct evolution upstream.
        /// </summary>
        public ref AVFilter Ref => ref *pFilter;
    }
}
