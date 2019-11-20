using FFmpeg.AutoGen;

using System;
using System.Collections;
using System.Collections.Generic;

namespace EmguFFmpeg
{
    public unsafe class MediaFilter
    {
        protected AVFilter* pFilter;
        //protected AVFilterContext* pFilterContext;

        internal MediaFilter(AVFilter* filter)
        {
            pFilter = filter;
        }

        public MediaFilter(string name) : this(ffmpeg.avfilter_get_by_name(name))
        {
            if (pFilter == null)
                throw new FFmpegException(ffmpeg.AVERROR_FILTER_NOT_FOUND);
        }

        //public void Initialize(MediaFilterGraph filterGraph, MediaDictionary options, string name = null)
        //{
        //    pFilterContext = ffmpeg.avfilter_graph_alloc_filter(filterGraph, pFilter, name);
        //    ffmpeg.avfilter_init_dict(pFilterContext, options).ThrowExceptionIfError();
        //}

        //public void Initialize(MediaFilterGraph filterGraph, Action<MediaFilter> options, string name = null)
        //{
        //    pFilterContext = ffmpeg.avfilter_graph_alloc_filter(filterGraph, pFilter, name);
        //    if (options != null)
        //        options.Invoke(this);
        //}

        //public void Initialize(MediaFilterGraph filterGraph, string options, string name = null)
        //{
        //    pFilterContext = ffmpeg.avfilter_graph_alloc_filter(filterGraph, pFilter, name);
        //    ffmpeg.avfilter_init_str(pFilterContext, options).ThrowExceptionIfError();
        //}

        //public void Initialize(MediaFilterGraph filterGraph, AVBufferSrcParameters parameters, string name = null)
        //{
        //    pFilterContext = ffmpeg.avfilter_graph_alloc_filter(filterGraph, pFilter, name);
        //    ffmpeg.av_buffersrc_parameters_set(pFilterContext, &parameters).ThrowExceptionIfError();
        //    ffmpeg.avfilter_init_str(pFilterContext, null);
        //}

        //public void LinkTo(uint srcPad, MediaFilter dstFilter, uint dstPad)
        //{
        //    if (pFilterContext == null)
        //        throw new FFmpegException(FFmpegException.NeedAddToGraph);
        //    ffmpeg.avfilter_link(pFilterContext, srcPad, dstFilter, dstPad).ThrowExceptionIfError();
        //}

        //public uint NbOutputs => pFilterContext != null ? pFilterContext->nb_outputs : throw new FFmpegException(FFmpegException.NullReference);
        //public uint NbInputs => pFilterContext != null ? pFilterContext->nb_inputs : throw new FFmpegException(FFmpegException.NullReference);

        //public int WriteFrame(MediaFrame frame, int flags = ffmpeg.AV_BUFFERSINK_FLAG_PEEK)
        //{
        //    return ffmpeg.av_buffersrc_add_frame_flags(pFilterContext, frame, flags);
        //}

        //public int GetFrame(MediaFrame frame)
        //{
        //    return ffmpeg.av_buffersink_get_frame(pFilterContext, frame);
        //}

        //public IEnumerable<MediaFrame> ReadFrame()
        //{
        //    using (MediaFrame frame = new VideoFrame())
        //    {
        //        while (true)
        //        {
        //            int ret = GetFrame(frame);
        //            if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
        //                break;
        //            ret.ThrowExceptionIfError();
        //            yield return frame;
        //            frame.Clear();
        //        }
        //    }
        //}

        //public AVFilterContext AVFilterContext => pFilterContext != null ? *pFilterContext : throw new FFmpegException(FFmpegException.NullReference);

        public AVFilter AVFilter => *pFilter;
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

        //public static implicit operator AVFilterContext*(MediaFilter value)
        //{
        //    if (value == null) return null;
        //    return value.pFilterContext;
        //}

        public override bool Equals(object obj)
        {
            if (obj is MediaFilter filter)
                return filter.pFilter == pFilter;
            return false;
        }

        public override int GetHashCode()
        {
            return ((IntPtr)pFilter).ToInt32();
        }

        public static class VideoSources
        {
            public const string Buffer = "buffer";
            public const string Cellauto = "cellauto";
            public const string Coreimagesrc = "coreimagesrc";
            public const string Mandelbrot = "mandelbrot";
            public const string Mptestsrc = "mptestsrc";
            public const string Frei0r_src = "frei0r_src";
            public const string Life = "life";
            public const string Allrgb = "allrgb";
            public const string Allyuv = "allyuv";
            public const string Color = "color";
            public const string Haldclutsrc = "haldclutsrc";
            public const string Nullsrc = "nullsrc";
            public const string Pal75bars = "pal75bars";
            public const string Pal100bars = "pal100bars";
            public const string Rgbtestsrc = "rgbtestsrc";
            public const string Smptebars = "smptebars";
            public const string Smptehdbars = "smptehdbars";
            public const string Testsrc = "testsrc";
            public const string Testsrc2 = "testsrc2";
            public const string Yuvtestsrc = "yuvtestsrc";
            public const string Openclsrc = "openclsrc";
            public const string Sierpinski = "sierpinski";
        }

        public static class VideoSinks
        {
            public const string Buffersink = "buffersink";
            public const string Nullsink = "nullsink";
        }

        public static class AudioSources
        {
            public const string Abuffer = "abuffer";
            public const string Aevalsrc = "aevalsrc";
            public const string Anullsrc = "anullsrc";
            public const string Flite = "flite";
            public const string Anoisesrc = "anoisesrc";
            public const string Hilbert = "hilbert";
            public const string Sinc = "sinc";
            public const string Sine = "sine";
        }

        public static class AudioSinks
        {
            public const string Abuffersink = "abuffersink";
            public const string Anullsink = "anullsink";
        }
    }
}