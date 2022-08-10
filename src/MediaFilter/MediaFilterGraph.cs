using FFmpeg.AutoGen;

using System;
using System.Collections;
using System.Collections.Generic;

namespace EmguFFmpeg
{
    public unsafe class MediaFilterGraph : IDisposable, IReadOnlyList<MediaFilterContext>
    {
        private AVFilterGraph* pFilterGraph;

        public MediaFilterGraph()
        {
            pFilterGraph = ffmpeg.avfilter_graph_alloc();
        }

        /// <summary>
        /// command line: $"width={width}:height={height}:pix_fmt={format}:time_base={timebase.num}/{timebase.den}:pixel_aspect={aspect.num}/{aspect.den}:frame_rate={framerate.num}/{framerate.den}:sws_param={swsparam}";
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="format"></param>
        /// <param name="timebase"></param>
        /// <param name="aspect"></param>
        /// <param name="framerate"></param>
        /// <param name="swsparam"></param>
        /// <param name="contextName"></param>
        /// <returns></returns>
        public MediaFilterContext AddVideoSrcFilter(MediaFilter filter, int width, int height, AVPixelFormat format, AVRational timebase, AVRational aspect, AVRational framerate = default, string swsparam = null, string contextName = null)
        {
            MediaFilterContext filterContext = AddFilter(filter, _ =>
             {
                 ffmpeg.av_opt_set_int(_, "width", width, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                 ffmpeg.av_opt_set_int(_, "height", height, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                 ffmpeg.av_opt_set(_, "pix_fmt", ffmpeg.av_get_pix_fmt_name(format), ffmpeg.AV_OPT_SEARCH_CHILDREN);
                 ffmpeg.av_opt_set_q(_, "pixel_aspect", aspect, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                 ffmpeg.av_opt_set_q(_, "time_base", timebase, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                 if (framerate.den != 0) // if is default value(0/0), not set frame_rate.
                     ffmpeg.av_opt_set_q(_, "frame_rate", framerate, ffmpeg.AV_OPT_SEARCH_CHILDREN); // not set is 0/1
                 if (swsparam != null)
                     ffmpeg.av_opt_set(_, "sws_param", swsparam, ffmpeg.AV_OPT_SEARCH_CHILDREN);
             }, contextName);
            if (filterContext.NbInputs > 0)
                throw new FFmpegException(FFmpegException.NotSourcesFilter);
            if (ffmpeg.avfilter_pad_get_type(filterContext.AVFilterContext.output_pads, 0) != AVMediaType.AVMEDIA_TYPE_VIDEO)
                throw new FFmpegException(FFmpegException.FilterTypeError);
            return filterContext;
        }

        public MediaFilterContext AddVideoSinkFilter(MediaFilter filter, AVPixelFormat[] formats = null, string contextName = null)
        {
            MediaFilterContext filterContext = AddFilter(filter, _ =>
            {
                if (formats != null)
                {
                    fixed (void* pixelFmts = formats)
                    {
                        ffmpeg.av_opt_set_bin(_, "pixel_fmts", (byte*)pixelFmts, sizeof(AVPixelFormat) * formats.Length, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                    }
                }
            }, contextName);
            if (filterContext.NbOutputs > 0)
                throw new FFmpegException(FFmpegException.NotSinksFilter);
            if (ffmpeg.avfilter_pad_get_type(filterContext.AVFilterContext.input_pads, 0) != AVMediaType.AVMEDIA_TYPE_VIDEO)
                throw new FFmpegException(FFmpegException.FilterTypeError);
            return filterContext;
        }

        public MediaFilterContext AddAudioSrcFilter(MediaFilter filter, AVChannelLayout channelLayout, int samplerate, AVSampleFormat format, string contextName = null)
        {
            MediaFilterContext filterContext = AddFilter(filter, _ =>
            {
                var c = channelLayout;
                fixed (byte* p = new byte[64])
                {
                    ffmpeg.av_channel_layout_describe(&c, p, 64);
                    ffmpeg.av_opt_set(_, "channel_layout", ((IntPtr)p).PtrToStringUTF8(), ffmpeg.AV_OPT_SEARCH_CHILDREN);
                    ffmpeg.av_opt_set(_, "sample_fmt", ffmpeg.av_get_sample_fmt_name(format), ffmpeg.AV_OPT_SEARCH_CHILDREN);
                    ffmpeg.av_opt_set_q(_, "time_base", new AVRational() { num = 1, den = samplerate }, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                    ffmpeg.av_opt_set_int(_, "sample_rate", samplerate, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                }
            }, contextName);
            if (filterContext.NbOutputs > 0)
                throw new FFmpegException(FFmpegException.NotSourcesFilter);
            if (ffmpeg.avfilter_pad_get_type(filterContext.AVFilterContext.input_pads, 0) != AVMediaType.AVMEDIA_TYPE_AUDIO)
                throw new FFmpegException(FFmpegException.FilterTypeError);
            return filterContext;
        }

        public MediaFilterContext AddAudioSinkFilter(MediaFilter filter, AVSampleFormat[] formats = null, int[] sampleRates = null, ulong[] channelLayouts = null, int[] channelCounts = null, int allChannelCounts = 0, string contextName = null)
        {
            MediaFilterContext filterContext = AddFilter(filter, _ =>
            {
                fixed (void* pfmts = formats)
                fixed (void* pSampleRates = sampleRates)
                fixed (void* pChLayouts = channelLayouts)
                fixed (void* pChCounts = channelCounts)
                {
                    ffmpeg.av_opt_set_bin(_, "sample_fmts", (byte*)pfmts, sizeof(AVSampleFormat) * formats.Length, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                    ffmpeg.av_opt_set_bin(_, "sample_rates", (byte*)pSampleRates, sizeof(int) * sampleRates.Length, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                    ffmpeg.av_opt_set_bin(_, "channel_layouts", (byte*)pChLayouts, sizeof(ulong) * channelLayouts.Length, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                    ffmpeg.av_opt_set_bin(_, "channel_counts", (byte*)pChCounts, sizeof(int) * channelCounts.Length, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                    ffmpeg.av_opt_set_int(_, "all_channel_counts", allChannelCounts, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                }
            }, contextName);
            if (filterContext.NbOutputs > 0)
                throw new FFmpegException(FFmpegException.NotSinksFilter);
            if (ffmpeg.avfilter_pad_get_type(filterContext.AVFilterContext.input_pads, 0) != AVMediaType.AVMEDIA_TYPE_AUDIO)
                throw new FFmpegException(FFmpegException.FilterTypeError);
            return filterContext;
        }

        public MediaFilterContext AddFilter(MediaFilter filter, string options = null, string contextName = null)
        {
            AVFilterContext* p = ffmpeg.avfilter_graph_alloc_filter(pFilterGraph, filter, contextName);
            ffmpeg.avfilter_init_str(p, options).ThrowIfError();
            return CreateAndUpdate(p);
        }

        public MediaFilterContext AddFilter(MediaFilter filter, Action<MediaFilterContext> options, string contextName = null)
        {
            AVFilterContext* p = ffmpeg.avfilter_graph_alloc_filter(pFilterGraph, filter, contextName);
            if (options != null)
                options.Invoke(new MediaFilterContext(p));
            ffmpeg.avfilter_init_str(p, null).ThrowIfError();
            return CreateAndUpdate(p);
        }

        public MediaFilterContext AddFilter(MediaFilter filter, MediaDictionary options, string contextName = null)
        {
            AVFilterContext* p = ffmpeg.avfilter_graph_alloc_filter(pFilterGraph, filter, contextName);
            ffmpeg.avfilter_init_dict(p, options).ThrowIfError();
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
        public MediaFilterContext[] Outputs => outputs.ToArray();
        public MediaFilterContext[] Inputs => inputs.ToArray();

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
            ffmpeg.avfilter_graph_config(pFilterGraph, null).ThrowIfError();
        }

        public static MediaFilterGraph CreateMediaFilterGraph(string graphDesc)
        {
            MediaFilterGraph filterGraph = new MediaFilterGraph();
            AVFilterInOut* inputs;
            AVFilterInOut* outputs;
            ffmpeg.avfilter_graph_parse2(filterGraph, graphDesc, &inputs, &outputs).ThrowIfError();
            AVFilterInOut* cur = inputs;
            for (cur = inputs; cur != null; cur = cur->next)
            {
                ffmpeg.av_log(null, (int)LogLevel.Debug, $"{((IntPtr)cur->name).PtrToStringUTF8()}{Environment.NewLine}");
                filterGraph.inputs.Add(new MediaFilterContext(cur->filter_ctx));
            }
            for (cur = outputs; cur != null; cur = cur->next)
            {
                ffmpeg.av_log(null, (int)LogLevel.Debug, $"{((IntPtr)cur->name).PtrToStringUTF8()}{Environment.NewLine}");
                filterGraph.outputs.Add(new MediaFilterContext(cur->filter_ctx));
            }

            foreach (var item in filterGraph)
            {
                ffmpeg.av_log(null, (int)LogLevel.Debug, $"{item.Name}{Environment.NewLine}");
                for (int i = 0; i < item.NbInputs; i++)
                {
                    ffmpeg.av_log(null, (int)LogLevel.Debug, $"{ffmpeg.avfilter_pad_get_name(item.AVFilterContext.input_pads, i)}{Environment.NewLine}");
                }
                for (int i = 0; i < item.NbOutputs; i++)
                {
                    ffmpeg.av_log(null, (int)LogLevel.Debug, $"{ffmpeg.avfilter_pad_get_name(item.AVFilterContext.output_pads, i)}{Environment.NewLine}");
                }
            }
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
