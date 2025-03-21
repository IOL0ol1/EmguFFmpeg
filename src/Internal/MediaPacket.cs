using System;
using FFmpeg.AutoGen.Abstractions;

namespace FFmpegSharp
{
    public unsafe partial class MediaPacket
    {
        /// <summary>
        /// Pointer to the underlying FFmpeg structure.
        /// WARNING: Be careful when accessing or modifying this field directly.
        /// </summary>
        protected AVPacket* pPacket = null;

        /// <summary>
        /// const AVPacket*
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVPacket*(MediaPacket value)
        {
            return value == null ? null : value.pPacket;
        }

        public MediaPacket(AVPacket* pAVPacket)
        {
            pPacket = pAVPacket;
        }

        public MediaPacket(IntPtr pAVPacket)
            : this((AVPacket*)pAVPacket)
        { }

        /// <summary>
        /// WARNING: Be careful when modifying this field directly.
        /// </summary>
        public ref AVPacket Ref => ref *pPacket;

    }
}
