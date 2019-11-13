using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmguFFmpeg
{
    public unsafe class MediaFilterGraph : IDisposable
    {
        private AVFilterGraph* pFilterGraph;
        private AVFilterInOut* pFilterInput;
        private AVFilterInOut* pFilterOutput;

        public MediaFilterGraph()
        {
            pFilterGraph = ffmpeg.avfilter_graph_alloc();
        }

        public static MediaFilterGraph CreateMediaFilterGraph(string graphDesc)
        {
            MediaFilterGraph filterGraph = new MediaFilterGraph();
            fixed (AVFilterInOut** pIn = &filterGraph.pFilterInput)
            fixed (AVFilterInOut** pOut = &filterGraph.pFilterOutput)
            {
                ffmpeg.avfilter_graph_parse2(filterGraph, graphDesc, pIn, pOut).ThrowExceptionIfError();
            }
            return filterGraph;
        }

        public void Initialize()
        {
            ffmpeg.avfilter_graph_config(pFilterGraph, null).ThrowExceptionIfError();
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