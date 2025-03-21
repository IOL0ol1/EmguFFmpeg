using System;
using FFmpeg.AutoGen.Abstractions;

namespace FFmpegSharp
{
    public unsafe partial class MediaCodec
    {
        /// <summary>
        /// Pointer to the underlying FFmpeg structure.
        /// WARNING: Be careful when accessing or modifying this field directly.
        /// </summary>
        protected AVCodec* pCodec = null;

        /// <summary>
        /// const AVCodec*
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVCodec*(MediaCodec value)
        {
            return value == null ? null : value.pCodec;
        }

        public MediaCodec(AVCodec* pAVCodec)
        {
            pCodec = pAVCodec;
        }

        public MediaCodec(IntPtr pAVCodec)
            : this((AVCodec*)pAVCodec)
        { }

        /// <summary>
        /// WARNING: Be careful when modifying this field directly.
        /// </summary>
        public ref AVCodec Ref => ref *pCodec;

    }
}
