using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    public unsafe partial class MediaFrame
    {
        /// <summary>
        /// Be careful!!!
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
        /// Direct access to the underlying struct fields.
        /// WARNING: Be careful when modifying. Tracks AVFrame struct evolution upstream.
        /// </summary>
        public ref AVFrame Ref => ref *pFrame;
    }
}
