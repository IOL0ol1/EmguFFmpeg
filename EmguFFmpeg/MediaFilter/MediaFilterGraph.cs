using FFmpeg.AutoGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmguFFmpeg
{
    public unsafe class MediaFilterGraph : IDisposable, IReadOnlyList<MediaFilterContext>
    {
        private AVFilterGraph* pFilterGraph;

        public MediaFilterGraph()
        {
            pFilterGraph = ffmpeg.avfilter_graph_alloc();
        }

        public MediaFilterContext AddFilter(MediaFilter filter, string options = null, string contextName = null)
        {
            AVFilterContext* p = ffmpeg.avfilter_graph_alloc_filter(pFilterGraph, filter, contextName);
            ffmpeg.avfilter_init_str(p, options).ThrowExceptionIfError();
            return CreateAndUpdate(p);
        }

        public MediaFilterContext AddFilter(MediaFilter filter, Action<MediaFilterContext> options, string contextName = null)
        {
            AVFilterContext* p = ffmpeg.avfilter_graph_alloc_filter(pFilterGraph, filter, contextName);
            if (options != null)
                options.Invoke(new MediaFilterContext(p));
            ffmpeg.avfilter_init_str(p, null).ThrowExceptionIfError();
            return CreateAndUpdate(p);
        }

        public MediaFilterContext AddFilter(MediaFilter filter, MediaDictionary options, string contextName = null)
        {
            AVFilterContext* p = ffmpeg.avfilter_graph_alloc_filter(pFilterGraph, filter, contextName);
            ffmpeg.avfilter_init_dict(p, options).ThrowExceptionIfError();
            return CreateAndUpdate(p);
        }

        private MediaFilterContext CreateAndUpdate(AVFilterContext* pFilterContext)
        {
            MediaFilterContext filterContext = new MediaFilterContext(pFilterContext);

            if (filterContext.NbInputs == 0)
            {
                inputs.Add(filterContext);
            }
            else if (filterContext.NbOutputs == 0)
            {
                outputs.Add(filterContext);
            }
            return filterContext;
        }

        private List<MediaFilterContext> inputs = new List<MediaFilterContext>();
        private List<MediaFilterContext> outputs = new List<MediaFilterContext>();
        public IReadOnlyList<MediaFilterContext> Outputs => outputs;
        public IReadOnlyList<MediaFilterContext> Inputs => inputs;

        private List<MediaFilterContext> GetFilterContexts()
        {
            List<MediaFilterContext> filterContexts = new List<MediaFilterContext>();
            for (int i = 0; i < pFilterGraph->nb_filters; i++)
            {
                filterContexts.Add(new MediaFilterContext(pFilterGraph->filters[i]));
            }
            return filterContexts;
        }

        public int Count => GetFilterContexts().Count;

        public MediaFilterContext this[int index] => GetFilterContexts()[index];

        public IEnumerator<MediaFilterContext> GetEnumerator()
        {
            return GetFilterContexts().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Initialize()
        {
            ffmpeg.avfilter_graph_config(pFilterGraph, null).ThrowExceptionIfError();
        }

        public static MediaFilterGraph CreateMediaFilterGraph(string graphDesc)
        {
            MediaFilterGraph filterGraph = new MediaFilterGraph();
            AVFilterInOut* inputs;
            AVFilterInOut* outputs;
            ffmpeg.avfilter_graph_parse2(filterGraph, graphDesc, &inputs, &outputs).ThrowExceptionIfError();
            AVFilterInOut* cur = inputs;
            for (cur = inputs; cur != null; cur = cur->next)
            {
                filterGraph.inputs.Add(new MediaFilterContext(cur->filter_ctx));
            }
            for (cur = outputs; cur != null; cur = cur->next)
            {
                filterGraph.outputs.Add(new MediaFilterContext(cur->filter_ctx));
            }
            var a = filterGraph[3].AVFilterContext.input_pads;
            string bb = ffmpeg.avfilter_pad_get_name(a, 1);
            // TODO: Link
            filterGraph.Initialize();
            ffmpeg.avfilter_inout_free(&inputs);
            ffmpeg.avfilter_inout_free(&outputs);
            return filterGraph;
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