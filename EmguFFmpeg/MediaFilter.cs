using FFmpeg.AutoGen;

using System;
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

        public MediaFilter(string name) : this(ffmpeg.avfilter_get_by_name(name))
        {
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

        public void Initialize(MediaFilterGraph filterGraph, string name, MediaDictionary options)
        {
            pFilterContext = ffmpeg.avfilter_graph_alloc_filter(filterGraph, pFilter, name);
            ffmpeg.avfilter_init_dict(pFilterContext, options).ThrowExceptionIfError();
        }

        public void Initialize(MediaFilterGraph filterGraph, string name, string options)
        {
            pFilterContext = ffmpeg.avfilter_graph_alloc_filter(filterGraph, pFilter, name);
            ffmpeg.avfilter_init_str(pFilterContext, options).ThrowExceptionIfError();
        }

        public void Initialize(MediaFilterGraph filterGraph, string name, Action<MediaFilter> option)
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

        public void Link(MediaFilter dstFilter, uint dstPad, uint srcPad)
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

        public void WriteFrame(MediaFrame frame, int flags = ffmpeg.AV_BUFFERSINK_FLAG_PEEK)
        {
            ffmpeg.av_buffersrc_add_frame_flags(pFilterContext, frame, flags).ThrowExceptionIfError();
        }

        public int GetFrame(MediaFrame frame)
        {
            return ffmpeg.av_buffersink_get_frame(pFilterContext, frame);
        }

        public IEnumerable<MediaFrame> ReadFrame(MediaFrame frame)
        {
            while (true)
            {
                int ret = GetFrame(frame);
                if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
                    break;
                ret.ThrowExceptionIfError();
                yield return frame;
                frame.Clear();
            }
        }

        public AVFilter AVFilter => *pFilter;
        public AVFilterContext AVFilterContext => *pFilterContext;

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
}