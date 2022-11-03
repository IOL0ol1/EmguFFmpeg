using FFmpeg.AutoGen;
namespace FFmpegSharp.Internal
{
    public abstract unsafe partial class MediaFilterGraphBase
    {
        /// <summary>
        /// Be careful!!!
        /// </summary>
        protected internal AVFilterGraph* pFilterGraph = null;

        /// <summary>
        /// const AVFilterGraph*
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVFilterGraph*(MediaFilterGraphBase value)
        {
            if (value == null) return null;
            return value.pFilterGraph;
        }

        public MediaFilterGraphBase(AVFilterGraph* value)
        {
            pFilterGraph = value;
        }

        public AVFilterGraph Ref => *pFilterGraph;

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

        public int SinkLinksCount
        {
            get => pFilterGraph->sink_links_count;
            set => pFilterGraph->sink_links_count = value;
        }

        public uint DisableAutoConvert
        {
            get => pFilterGraph->disable_auto_convert;
            set => pFilterGraph->disable_auto_convert = value;
        }

    }
}
