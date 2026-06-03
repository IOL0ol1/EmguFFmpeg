using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    public unsafe partial class MediaFormatContext
    {
        /// <summary>
        /// Be careful!!!
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
        /// Direct access to the underlying struct fields.
        /// WARNING: Be careful when modifying. Tracks AVFormatContext struct evolution upstream.
        /// </summary>
        public ref AVFormatContext Ref => ref *pFormatContext;
    }
}
