using System;
using FFmpeg.AutoGen;

namespace FFmpegSharp
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

        public AVFilterGraph Const => *pFilterGraph;

        public uint NbFilters
        {
            get => pFilterGraph->nb_filters;
            set => pFilterGraph->nb_filters = value;
        }

        public int ThreadType
        {
            get => pFilterGraph->thread_type;
            set => pFilterGraph->thread_type = value;
        }

        public int NbThreads
        {
            get => pFilterGraph->nb_threads;
            set => pFilterGraph->nb_threads = value;
        }

    }
}
