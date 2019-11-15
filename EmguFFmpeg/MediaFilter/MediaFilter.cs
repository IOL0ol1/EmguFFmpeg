using FFmpeg.AutoGen;

using System;
using System.Collections;
using System.Collections.Generic;

namespace EmguFFmpeg
{
    public unsafe class MediaFilter : IDisposable
    {
        protected AVFilter* pFilter;
        protected AVFilterContext* pFilterContext;

        internal MediaFilter(AVFilter* filter)
        {
            pFilter = filter;
        }

        internal MediaFilter(AVFilterContext* filterContext)
        {
            if (filterContext == null)
                throw new FFmpegException(FFmpegException.NullReference);
            pFilter = filterContext->filter;
            pFilterContext = filterContext;
        }

        public MediaFilter(string name) : this(ffmpeg.avfilter_get_by_name(name))
        {
            if (pFilter == null)
                throw new FFmpegException(ffmpeg.AVERROR_FILTER_NOT_FOUND);
        }

        public static implicit operator AVFilter*(MediaFilter value)
        {
            if (value == null) return null;
            return value.pFilter;
        }

        public static implicit operator AVFilterContext*(MediaFilter value)
        {
            if (value == null) return null;
            return value.pFilterContext;
        }

        public void Initialize(MediaFilterGraph filterGraph, MediaDictionary options, string name = null)
        {
            pFilterContext = ffmpeg.avfilter_graph_alloc_filter(filterGraph, pFilter, name);
            ffmpeg.avfilter_init_dict(pFilterContext, options).ThrowExceptionIfError();
        }

        public void Initialize(MediaFilterGraph filterGraph, string options, string name = null)
        {
            pFilterContext = ffmpeg.avfilter_graph_alloc_filter(filterGraph, pFilter, name);
            ffmpeg.avfilter_init_str(pFilterContext, options).ThrowExceptionIfError();
        }

        public void Initialize(MediaFilterGraph filterGraph, Action<MediaFilter> option, string name = null)
        {
            pFilterContext = ffmpeg.avfilter_graph_alloc_filter(filterGraph, pFilter, name);
            if (option != null)
                option.Invoke(this);
            ffmpeg.avfilter_init_str(pFilterContext, null).ThrowExceptionIfError();
        }

        #region Set filter

        public void SetFilter(string key, string value)
        {
            ffmpeg.av_opt_set(pFilterContext, key, value, ffmpeg.AV_OPT_SEARCH_CHILDREN).ThrowExceptionIfError();
        }

        public void SetFilter(string key, long value)
        {
            ffmpeg.av_opt_set_int(pFilterContext, key, value, ffmpeg.AV_OPT_SEARCH_CHILDREN).ThrowExceptionIfError();
        }

        public void SetFilter(string key, AVRational value)
        {
            ffmpeg.av_opt_set_q(pFilterContext, key, value, ffmpeg.AV_OPT_SEARCH_CHILDREN).ThrowExceptionIfError();
        }

        #endregion

        public void Link(uint srcPad, MediaFilter dstFilter, uint dstPad)
        {
            ffmpeg.avfilter_link(pFilterContext, srcPad, dstFilter, dstPad).ThrowExceptionIfError();
        }

        public static IReadOnlyList<MediaFilter> Filters
        {
            get
            {
                List<MediaFilter> result = new List<MediaFilter>();
                void* p = null;
                AVFilter* pFilter;
                while ((pFilter = ffmpeg.av_filter_iterate(&p)) != null)
                {
                    result.Add(new MediaFilter(pFilter));
                }
                return result;
            }
        }

        public int AddFrame(MediaFrame frame, int flags = ffmpeg.AV_BUFFERSINK_FLAG_PEEK)
        {
            return ffmpeg.av_buffersrc_add_frame_flags(pFilterContext, frame, flags);
        }

        public int GetFrame(MediaFrame frame)
        {
            return ffmpeg.av_buffersink_get_frame(pFilterContext, frame);
        }

        public AVFilter AVFilter => *pFilter;
        public AVFilterContext AVFilterContext => *pFilterContext;
        public string Name => ((IntPtr)pFilter->name).PtrToStringUTF8();
        public string Description => ((IntPtr)pFilter->description).PtrToStringUTF8();

        #region IDisposable Support

        private bool disposedValue = false;

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

        ~MediaFilter()
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