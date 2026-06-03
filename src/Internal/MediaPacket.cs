using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    public unsafe partial class MediaPacket
    {
        /// <summary>
        /// Be careful!!!
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
        /// Direct access to the underlying struct fields.
        /// WARNING: Be careful when modifying. Tracks AVPacket struct evolution upstream.
        /// </summary>
        public ref AVPacket Ref => ref *pPacket;
    }
}
