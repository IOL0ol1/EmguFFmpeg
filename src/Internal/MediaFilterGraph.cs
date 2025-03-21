using System;
using FFmpeg.AutoGen.Abstractions;

namespace FFmpegSharp
{
    public unsafe partial class MediaFilterGraph
    {
        /// <summary>
        /// Pointer to the underlying FFmpeg structure.
        /// WARNING: Be careful when accessing or modifying this field directly.
        /// </summary>
        protected AVFilterGraph* pFilterGraph = null;

        /// <summary>
        /// const AVFilterGraph*
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVFilterGraph*(MediaFilterGraph value)
        {
            return value == null ? null : value.pFilterGraph;
        }

        public MediaFilterGraph(AVFilterGraph* pAVFilterGraph)
        {
            pFilterGraph = pAVFilterGraph;
        }

        public MediaFilterGraph(IntPtr pAVFilterGraph)
            : this((AVFilterGraph*)pAVFilterGraph)
        { }

        /// <summary>
        /// WARNING: Be careful when modifying this field directly.
        /// </summary>
        public ref AVFilterGraph Ref => ref *pFilterGraph;

    }
}
