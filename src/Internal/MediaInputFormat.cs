using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    public unsafe partial class MediaInputFormat
    {
        /// <summary>
        /// Be careful!!!
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
        /// Direct access to the underlying struct fields.
        /// WARNING: Be careful when modifying. Tracks AVInputFormat struct evolution upstream.
        /// </summary>
        public ref AVInputFormat Ref => ref *pInputFormat;
    }
}
