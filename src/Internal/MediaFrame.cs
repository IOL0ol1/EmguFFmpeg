using System;
using FFmpeg.AutoGen.Abstractions;

namespace FFmpegSharp
{
    public unsafe partial class MediaFrame
    {
        /// <summary>
        /// Pointer to the underlying FFmpeg structure.
        /// WARNING: Be careful when accessing or modifying this field directly.
        /// </summary>
        protected AVFrame* pFrame = null;

        /// <summary>
        /// const AVFrame*
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVFrame*(MediaFrame value)
        {
            return value == null ? null : value.pFrame;
        }

        public MediaFrame(AVFrame* pAVFrame)
        {
            pFrame = pAVFrame;
        }

        public MediaFrame(IntPtr pAVFrame)
            : this((AVFrame*)pAVFrame)
        { }

        /// <summary>
        /// WARNING: Be careful when modifying this field directly.
        /// </summary>
        public ref AVFrame Ref => ref *pFrame;

    }
}
