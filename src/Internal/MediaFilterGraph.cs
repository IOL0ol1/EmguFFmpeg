using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    public unsafe partial class MediaFilterGraph
    {
        /// <summary>
        /// Be careful!!!
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
        /// Direct access to the underlying struct fields.
        /// WARNING: Be careful when modifying. Tracks AVFilterGraph struct evolution upstream.
        /// </summary>
        public ref AVFilterGraph Ref => ref *pFilterGraph;
    }
}
