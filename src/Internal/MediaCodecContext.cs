using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    public unsafe partial class MediaCodecContext
    {
        /// <summary>
        /// Be careful!!!
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
        /// Direct access to the underlying struct fields.
        /// WARNING: Be careful when modifying. Tracks AVCodecContext struct evolution upstream.
        /// </summary>
        public ref AVCodecContext Ref => ref *pCodecContext;
    }
}
