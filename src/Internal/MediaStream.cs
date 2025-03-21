using System;
using FFmpeg.AutoGen.Abstractions;

namespace FFmpegSharp
{
    public unsafe partial class MediaStream
    {
        /// <summary>
        /// Pointer to the underlying FFmpeg structure.
        /// WARNING: Be careful when accessing or modifying this field directly.
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
        /// WARNING: Be careful when modifying this field directly.
        /// </summary>
        public ref AVStream Ref => ref *pStream;

    }
}
