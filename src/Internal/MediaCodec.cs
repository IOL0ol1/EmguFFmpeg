using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    public unsafe partial class MediaCodec
    {
        /// <summary>
        /// Be careful!!!
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
        /// Direct access to the underlying struct fields.
        /// WARNING: Be careful when modifying. Tracks AVCodec struct evolution upstream.
        /// </summary>
        public ref AVCodec Ref => ref *pCodec;
    }
}
