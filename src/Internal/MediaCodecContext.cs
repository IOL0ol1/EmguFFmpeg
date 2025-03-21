using System;
using FFmpeg.AutoGen.Abstractions;

namespace FFmpegSharp
{
    public unsafe partial class MediaCodecContext
    {
        /// <summary>
        /// Pointer to the underlying FFmpeg structure.
        /// WARNING: Be careful when accessing or modifying this field directly.
        /// </summary>
        protected AVCodecContext* pCodecContext = null;

        /// <summary>
        /// const AVCodecContext*
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVCodecContext*(MediaCodecContext value)
        {
            return value == null ? null : value.pCodecContext;
        }

        public MediaCodecContext(AVCodecContext* pAVCodecContext)
        {
            pCodecContext = pAVCodecContext;
        }

        public MediaCodecContext(IntPtr pAVCodecContext)
            : this((AVCodecContext*)pAVCodecContext)
        { }

        /// <summary>
        /// WARNING: Be careful when modifying this field directly.
        /// </summary>
        public ref AVCodecContext Ref => ref *pCodecContext;

    }
}
