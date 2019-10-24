using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;

namespace FFmpegManaged
{
    public unsafe class MediaFilter : IDisposable
    {
        protected AVFilter* pFilter;
        protected AVFilterContext* pFilterContext;
        //protected AVFilterContext* pFilterContext2;
        //protected AVFilterInOut* pFilterOutputs;
        //protected AVFilterInOut* pFilterInputs;
        //protected AVFilterGraph* pFilterGraph;
        //protected AVFilterGraph* pFilterGraph;

        public MediaFilter(AVFilter* filter)
        {
            pFilter = filter;
        }

        public MediaFilter(string name) : this(ffmpeg.avfilter_get_by_name(name))
        {
        }

        public static implicit operator AVFilter*(MediaFilter value)
        {
            if (value == null) return null;
            return value.pFilter;
        }

        public static implicit operator AVFilterContext**(MediaFilter value)
        {
            if (value == null) return null;
            fixed (AVFilterContext** result = &value.pFilterContext)
                return result;
        }

        //public void Initialize(string name)
        //{
        //    pFilterInputs = ffmpeg.avfilter_inout_alloc();
        //    pFilterGraph = ffmpeg.avfilter_graph_alloc();
        //    fixed (AVFilterContext** ppFilterContext = &pFilterContext)
        //        ffmpeg.avfilter_graph_create_filter(ppFilterContext, pFilter, name, null, null, pFilterGraph);

        //    pFilterInputs->name = (byte*)Marshal.StringToHGlobalAnsi(name);
        //    pFilterInputs->filter_ctx = pFilterContext;
        //    pFilterInputs->pad_idx = 0;
        //    pFilterInputs->next = null;
        //    ffmpeg.avfilter_graph_config(pFilterGraph, null).ThrowExceptionIfError();
        //}

        public void SetFilter<T>(string key, T value) where T : struct
        {
            ffmpeg.av_opt_set(pFilterContext, key, value.ToString(), ffmpeg.AV_OPT_SEARCH_CHILDREN).ThrowExceptionIfError();
        }

        public static IReadOnlyList<MediaFilter> GetFilters(string name = null)
        {
            List<MediaFilter> result = new List<MediaFilter>();
            void* p = null;
            AVFilter* pFilter;
            while ((pFilter = ffmpeg.av_filter_iterate(&p)) != null)
            {
                MediaFilter filter = new MediaFilter(pFilter);
                if (string.IsNullOrEmpty(name) || filter.Name == name.ToLower())
                    result.Add(filter);
            }
            return result;
        }

        public AVFilter Filter => *pFilter;

        public string Name => ((IntPtr)pFilter->name).PtrToStringUTF8();
        public string Description => ((IntPtr)pFilter->description).PtrToStringUTF8();

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                //fixed (AVFilterInOut** ppFilterInOut = &pFilterInputs)
                //    ffmpeg.avfilter_inout_free(ppFilterInOut);

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~MediaFilter()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    public unsafe class MediaFilterGraph
    {
        private MediaFilter buffer;
        private MediaFilter buffersink;

        private AVFilterInOut* outputs;
        private AVFilterInOut* inputs;
        private AVFilterGraph* filterGraph;

        public MediaFilterGraph()
        {
            buffer = new MediaFilter("buffer");
            buffersink = new MediaFilter("buffersink");
            outputs = ffmpeg.avfilter_inout_alloc();
            inputs = ffmpeg.avfilter_inout_alloc();
            filterGraph = ffmpeg.avfilter_graph_alloc();
        }

        public void AddFilter(string name, string args)
        {
            ffmpeg.avfilter_graph_create_filter(buffer, buffer, name, args, null, filterGraph);
        }
    }
}