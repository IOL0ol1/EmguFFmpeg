using FFmpeg.AutoGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmguFFmpeg
{
    public unsafe class MediaFilterGraph : IDisposable, IReadOnlyList<MediaFilter>
    {
        private AVFilterGraph* pFilterGraph;
        private AVFilterInOut* pFilterInput;
        private AVFilterInOut* pFilterOutput;

        private List<MediaFilter> filters
        {
            get
            {
                List<MediaFilter> results = new List<MediaFilter>();
                if (pFilterGraph != null)
                {
                    for (int i = 0; i < pFilterGraph->nb_filters; i++)
                    {
                        results.Add(new MediaFilter(pFilterGraph->filters[i]));
                    }
                }
                return results;
            }
        }

        private List<MediaFilterInOut> outputs = new List<MediaFilterInOut>();
        private List<MediaFilterInOut> inputs = new List<MediaFilterInOut>();

        public IReadOnlyList<MediaFilterInOut> Outputs => outputs;
        public IReadOnlyList<MediaFilterInOut> Inputs => inputs;

        public int Count => filters.Count;

        public MediaFilter this[int index] => filters[index];

        public MediaFilterGraph()
        {
            pFilterGraph = ffmpeg.avfilter_graph_alloc();
        }

        public void Initialize()
        {
            ffmpeg.avfilter_graph_config(pFilterGraph, null).ThrowExceptionIfError();
        }

        public static MediaFilterGraph CreateMediaFilterGraph(string graphDesc)
        {
            MediaFilterGraph filterGraph = new MediaFilterGraph();
            fixed (AVFilterInOut** pIn = &filterGraph.pFilterInput)
            fixed (AVFilterInOut** pOut = &filterGraph.pFilterOutput)
            {
                ffmpeg.avfilter_graph_parse2(filterGraph, graphDesc, pIn, pOut).ThrowExceptionIfError();
            }
            for (int i = 0; i < filterGraph.pFilterGraph->nb_filters; i++)
            {
                filterGraph.filters.Add(new MediaFilter(filterGraph.pFilterGraph->filters[i]));
            }
            AVFilterInOut* pInOut = filterGraph.pFilterInput;
            while (pInOut != null)
            {
                filterGraph.inputs.Add(new MediaFilterInOut(pInOut));
                pInOut = pInOut->next;
            }
            pInOut = filterGraph.pFilterOutput;
            while (pInOut != null)
            {
                filterGraph.outputs.Add(new MediaFilterInOut(pInOut));
                pInOut = pInOut->next;
            }
            return filterGraph;
        }

        public void AddFilter(MediaFilter filter, MediaDictionary options, string contextName = null)
        {
            AVFilterContext* pFilterContext = filter;
            if (pFilterContext != null && pFilterContext->graph != pFilterGraph)
                throw new FFmpegException(FFmpegException.FilterHasInit);
            else if (pFilterGraph == null)
                filter.Initialize(this, options, contextName);
        }

        public IEnumerator<MediaFilter> GetEnumerator()
        {
            return filters.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return filters.GetEnumerator();
        }

        public static implicit operator AVFilterGraph*(MediaFilterGraph value)
        {
            if (value == null) return null;
            return value.pFilterGraph;
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                fixed (AVFilterGraph** pp = &pFilterGraph)
                    ffmpeg.avfilter_graph_free(pp);

                disposedValue = true;
            }
        }

        ~MediaFilterGraph()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}