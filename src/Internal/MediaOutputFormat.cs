using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    public unsafe partial class MediaOutputFormat
    {
        /// <summary>
        /// Be careful!!!
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
        /// Direct access to the underlying struct fields.
        /// WARNING: Be careful when modifying. Tracks AVOutputFormat struct evolution upstream.
        /// </summary>
        public ref AVOutputFormat Ref => ref *pOutputFormat;
    }
}
