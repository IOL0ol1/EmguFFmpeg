using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    public unsafe partial class MediaStream
    {
        /// <summary>
        /// Be careful!!!
        /// </summary>
        protected AVStream* pStream = null;

        /// <summary>
        /// const AVStream*
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVStream*(MediaStream value)
        {
            return value == null ? null : value.pStream;
        }

        public MediaStream(AVStream* pAVStream)
        {
            pStream = pAVStream;
        }

        public MediaStream(IntPtr pAVStream)
            : this((AVStream*)pAVStream)
        { }

        /// <summary>
        /// Direct access to the underlying struct fields.
        /// WARNING: Be careful when modifying. Tracks AVStream struct evolution upstream.
        /// </summary>
        public ref AVStream Ref => ref *pStream;
    }
}
