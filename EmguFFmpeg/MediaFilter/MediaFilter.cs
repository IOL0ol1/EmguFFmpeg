using FFmpeg.AutoGen;

using System;
using System.Collections;
using System.Collections.Generic;

namespace EmguFFmpeg
{
    public unsafe class MediaFilter
    {
        protected AVFilter* pFilter;
        protected AVFilterContext* pFilterContext;

        internal MediaFilter(AVFilter* filter)
        {
            pFilter = filter;
        }

        public static MediaFilter CreateBufferFilter()
        {
            return new MediaFilter("buffer");
        }

        public static MediaFilter CreateBufferSinkFilter()
        {
            return new MediaFilter("buffersink");
        }

        public static MediaFilter CreateAbufferFilter()
        {
            return new MediaFilter("abuffer");
        }

        public static MediaFilter CreateAbufferSinkFilter()
        {
            return new MediaFilter("abuffersink");
        }

        public MediaFilter(string name) : this(ffmpeg.avfilter_get_by_name(name))
        {
            if (pFilter == null)
                throw new FFmpegException(ffmpeg.AVERROR_FILTER_NOT_FOUND);
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

        public void Initialize(MediaFilterGraph filterGraph, AVBufferSrcParameters parameters, string name = null)
        {
            pFilterContext = ffmpeg.avfilter_graph_alloc_filter(filterGraph, pFilter, name);
            ffmpeg.av_buffersrc_parameters_set(pFilterContext, &parameters).ThrowExceptionIfError();
        }

        public void LinkTo(uint srcPad, MediaFilter dstFilter, uint dstPad)
        {
            if (pFilterContext == null)
                throw new FFmpegException(FFmpegException.NeedAddToGraph);
            ffmpeg.avfilter_link(pFilterContext, srcPad, dstFilter, dstPad).ThrowExceptionIfError();
        }

        public uint NbOutputs => pFilterContext != null ? pFilterContext->nb_outputs : throw new FFmpegException(FFmpegException.NullReference);
        public uint NbInputs => pFilterContext != null ? pFilterContext->nb_inputs : throw new FFmpegException(FFmpegException.NullReference);

        public int WriteFrame(MediaFrame frame, int flags = ffmpeg.AV_BUFFERSINK_FLAG_PEEK)
        {
            return ffmpeg.av_buffersrc_add_frame_flags(pFilterContext, frame, flags);
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

        public AVFilterContext AVFilterContext => pFilterContext != null ? *pFilterContext : throw new FFmpegException(FFmpegException.NullReference);

        public string Name => ((IntPtr)pFilter->name).PtrToStringUTF8();

        public string Description => ((IntPtr)pFilter->description).PtrToStringUTF8();

        public override string ToString()
        {
            return Name;
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
    }
}