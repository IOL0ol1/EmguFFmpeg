using System;
using System.Collections;
using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    public unsafe partial class MediaFilterGraph : IDisposable, IReadOnlyList<MediaFilterContext>
    {


        public MediaFilterGraph() : this(ffmpeg.avfilter_graph_alloc())
        { }


        public MediaFilterContext CreateFilter(MediaFilter filter, string name, string args)
        {
            AVFilterContext* p = null;
            ffmpeg.avfilter_graph_create_filter(&p, filter, name, args, null, pFilterGraph).ThrowIfError();
            return p == null ? null : new MediaFilterContext(p);
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
                 MediaOptions.SetInt(_, "width", width, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                 MediaOptions.SetInt(_, "height", height, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                 MediaOptions.Set(_, "pix_fmt", ffmpeg.av_get_pix_fmt_name(format), ffmpeg.AV_OPT_SEARCH_CHILDREN);
                 ffmpeg.av_opt_set_q(_, "pixel_aspect", aspect, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                 ffmpeg.av_opt_set_q(_, "time_base", timebase, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                 if (framerate.den != 0) // if is default value(0/0), not set frame_rate.
                     ffmpeg.av_opt_set_q(_, "frame_rate", framerate, ffmpeg.AV_OPT_SEARCH_CHILDREN); // not set is 0/1
                 if (swsparam != null)
                     MediaOptions.Set(_, "sws_param", swsparam, ffmpeg.AV_OPT_SEARCH_CHILDREN);
             }, contextName);
            if (filterContext.Ref.nb_inputs > 0)
                throw new FFmpegException("FFmpegException.NotSourcesFilter");
            if (ffmpeg.avfilter_pad_get_type(filterContext.Ref.output_pads, 0) != AVMediaType.AVMEDIA_TYPE_VIDEO)
                throw new FFmpegException("FFmpegException.FilterTypeError");
            return filterContext;
        }

        public MediaFilterContext AddVideoSrcFilter(MediaFilter filter, string options = null, string contextName = null)
        {
            MediaFilterContext filterContext = AddFilter(filter, options, contextName);
            if (filterContext.Ref.nb_inputs > 0)
                throw new FFmpegException("FFmpegException.NotSourcesFilter");
            if (ffmpeg.avfilter_pad_get_type(filterContext.Ref.output_pads, 0) != AVMediaType.AVMEDIA_TYPE_VIDEO)
                throw new FFmpegException("FFmpegException.FilterTypeError");
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
            if (filterContext.Ref.nb_outputs > 0)
                throw new FFmpegException("FFmpegException.NotSinksFilter");
            if (ffmpeg.avfilter_pad_get_type(filterContext.Ref.input_pads, 0) != AVMediaType.AVMEDIA_TYPE_VIDEO)
                throw new FFmpegException("FFmpegException.FilterTypeError");
            return filterContext;
        }

        public MediaFilterContext AddAudioSrcFilter(MediaFilter filter, AVChannelLayout channelLayout, int samplerate, AVSampleFormat format, string contextName = null)
            => AddAudioSrcFilter(filter, channelLayout, new AVRational { num = 1, den = samplerate }, samplerate, format, contextName);

        /// <summary>
        /// Add an abuffer source filter with an explicit <paramref name="timeBase"/> (use when the stream
        /// time_base is not <c>1/samplerate</c>, e.g. <c>1/90000</c> container timestamps).
        /// The <paramref name="channelLayout"/> is normalised from <c>AV_CHANNEL_ORDER_UNSPEC</c> automatically.
        /// </summary>
        public MediaFilterContext AddAudioSrcFilter(MediaFilter filter, AVChannelLayout channelLayout, AVRational timeBase, int samplerate, AVSampleFormat format, string contextName = null)
        {
            // Normalise unspecified order so av_channel_layout_describe produces a valid string.
            if (channelLayout.order == AVChannelOrder.AV_CHANNEL_ORDER_UNSPEC)
                ffmpeg.av_channel_layout_default(&channelLayout, channelLayout.nb_channels);
            // Pre-compute the layout string before the lambda so we never take the address of a
            // captured local (which the C# compiler forbids — CS1686).
            string chLayoutStr;
            fixed (byte* p = new byte[64])
            {
                ffmpeg.av_channel_layout_describe(&channelLayout, p, 64);
                chLayoutStr = ((IntPtr)p).PtrToStringUTF8();
            }
            string sampleFmtStr = ffmpeg.av_get_sample_fmt_name(format);
            MediaFilterContext filterContext = AddFilter(filter, _ =>
            {
                MediaOptions.Set(_, "channel_layout", chLayoutStr, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                MediaOptions.Set(_, "sample_fmt", sampleFmtStr, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                ffmpeg.av_opt_set_q(_, "time_base", timeBase, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                MediaOptions.SetInt(_, "sample_rate", samplerate, ffmpeg.AV_OPT_SEARCH_CHILDREN);
            }, contextName);
            if (filterContext.Ref.nb_inputs > 0)
                throw new FFmpegException("FFmpegException.NotSourcesFilter");
            if (ffmpeg.avfilter_pad_get_type(filterContext.Ref.output_pads, 0) != AVMediaType.AVMEDIA_TYPE_AUDIO)
                throw new FFmpegException("FFmpegException.FilterTypeError");
            return filterContext;
        }

        public MediaFilterContext AddAudioSrcFilter(MediaFilter filter, string options = null, string contextName = null)
        {
            MediaFilterContext filterContext = AddFilter(filter, options, contextName);
            if (filterContext.Ref.nb_inputs > 0)
                throw new FFmpegException("FFmpegException.NotSourcesFilter");
            if (ffmpeg.avfilter_pad_get_type(filterContext.Ref.output_pads, 0) != AVMediaType.AVMEDIA_TYPE_AUDIO)
                throw new FFmpegException("FFmpegException.FilterTypeError");
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
                    MediaOptions.SetInt(_, "all_channel_counts", allChannelCounts, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                }
            }, contextName);
            if (filterContext.Ref.nb_outputs > 0)
                throw new FFmpegException("FFmpegException.NotSinksFilter");
            if (ffmpeg.avfilter_pad_get_type(filterContext.Ref.input_pads, 0) != AVMediaType.AVMEDIA_TYPE_AUDIO)
                throw new FFmpegException("FFmpegException.FilterTypeError");
            return filterContext;
        }

        /// <summary>
        /// Add an abuffersink filter constrained to the given formats. Uses string-based option setting
        /// compatible with FFmpeg 5+ (where <c>ch_layouts</c> is a string, not a binary mask array).
        /// Pass <see langword="null"/> for any parameter to leave that constraint unconstrained.
        /// </summary>
        public MediaFilterContext AddAudioSinkFilter(MediaFilter filter, AVSampleFormat[] formats, int[] sampleRates, AVChannelLayout[] channelLayouts, string contextName = null)
        {
            MediaFilterContext filterContext = AddFilter(filter, _ =>
            {
                if (formats != null)
                {
                    fixed (AVSampleFormat* p = formats)
                        ffmpeg.av_opt_set_bin(_, "sample_fmts", (byte*)p, sizeof(AVSampleFormat) * formats.Length, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                }
                if (sampleRates != null)
                {
                    fixed (int* p = sampleRates)
                        ffmpeg.av_opt_set_bin(_, "sample_rates", (byte*)p, sizeof(int) * sampleRates.Length, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                }
                if (channelLayouts != null)
                {
                    // Build a pipe-separated layout string: "stereo|mono|5.1" etc.
                    var sb = new System.Text.StringBuilder();
                    for (int li = 0; li < channelLayouts.Length; li++)
                    {
                        if (li > 0) sb.Append('|');
                        var cl = channelLayouts[li];
                        fixed (byte* p = new byte[64])
                        {
                            ffmpeg.av_channel_layout_describe(&cl, p, 64);
                            sb.Append(((IntPtr)p).PtrToStringUTF8());
                        }
                    }
                    MediaOptions.Set(_, "ch_layouts", sb.ToString(), ffmpeg.AV_OPT_SEARCH_CHILDREN);
                }
            }, contextName);
            if (filterContext.Ref.nb_outputs > 0)
                throw new FFmpegException("FFmpegException.NotSinksFilter");
            if (ffmpeg.avfilter_pad_get_type(filterContext.Ref.input_pads, 0) != AVMediaType.AVMEDIA_TYPE_AUDIO)
                throw new FFmpegException("FFmpegException.FilterTypeError");
            return filterContext;
        }

        public MediaFilterContext AddFilter(MediaFilter filter, string options = null, string contextName = null)
        {
            var context = new MediaFilterContext(ffmpeg.avfilter_graph_alloc_filter(pFilterGraph, filter, contextName));
            ffmpeg.avfilter_init_str(context, options).ThrowIfError();
            return context;
        }

        public MediaFilterContext AddFilter(MediaFilter filter, Action<MediaFilterContext> options, string contextName = null)
        {
            var context = new MediaFilterContext(ffmpeg.avfilter_graph_alloc_filter(pFilterGraph, filter, contextName));
            options?.Invoke(context);
            ffmpeg.avfilter_init_str(context, null).ThrowIfError();
            return context;
        }

        public MediaFilterContext AddFilter(MediaFilter filter, MediaDictionary options, string contextName = null)
        {
            var context = new MediaFilterContext(ffmpeg.avfilter_graph_alloc_filter(pFilterGraph, filter, contextName));
            fixed (AVDictionary** opts = &options.pDictionary)
                ffmpeg.avfilter_init_dict(context, opts).ThrowIfError();
            return context;
        }

        public MediaFilterContext GetFilter(string name)
        {
            var f = ffmpeg.avfilter_graph_get_filter(pFilterGraph, name);
            return f == null ? null : new MediaFilterContext(f);
        }

        private List<MediaFilterContext> GetFilterContexts()
        {
            List<MediaFilterContext> filterContexts = new List<MediaFilterContext>();
            for (int i = 0; i < pFilterGraph->nb_filters; i++)
            {
                filterContexts.Add(new MediaFilterContext(pFilterGraph->filters[i]));
            }
            return filterContexts;
        }

        public int Count => (int)pFilterGraph->nb_filters;

        public MediaFilterContext this[int index] => new MediaFilterContext(pFilterGraph->filters[index]);

        public IEnumerator<MediaFilterContext> GetEnumerator()
        {
            return GetFilterContexts().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Parse a filtergraph description and connect it between <paramref name="srcCtx"/> (named "in")
        /// and <paramref name="sinkCtx"/> (named "out"). Call <see cref="Initialize"/> afterwards.
        /// </summary>
        public void ParseGraph(string filterSpec, MediaFilterContext srcCtx, MediaFilterContext sinkCtx)
            => ParseGraph(filterSpec, srcCtx, "in", sinkCtx, "out");

        /// <summary>
        /// Parse a filtergraph description with custom endpoint names.
        /// Useful for complex graphs with non-default pad labels (e.g. <c>"[src]..."</c>).
        /// Call <see cref="Initialize"/> afterwards.
        /// </summary>
        public void ParseGraph(string filterSpec, MediaFilterContext srcCtx, string srcPadName, MediaFilterContext sinkCtx, string sinkPadName)
        {
            AVFilterInOut* outputs = ffmpeg.avfilter_inout_alloc();
            AVFilterInOut* inputs  = ffmpeg.avfilter_inout_alloc();
            try
            {
                outputs->name       = ffmpeg.av_strdup(srcPadName);
                outputs->filter_ctx = srcCtx;
                outputs->pad_idx    = 0;
                outputs->next       = null;

                inputs->name        = ffmpeg.av_strdup(sinkPadName);
                inputs->filter_ctx  = sinkCtx;
                inputs->pad_idx     = 0;
                inputs->next        = null;

                ffmpeg.avfilter_graph_parse_ptr(pFilterGraph, filterSpec, &inputs, &outputs, null).ThrowIfError();
            }
            finally
            {
                ffmpeg.avfilter_inout_free(&inputs);
                ffmpeg.avfilter_inout_free(&outputs);
            }
        }

        public void Initialize()
        {
            ffmpeg.avfilter_graph_config(pFilterGraph, null).ThrowIfError();
        }

        /// <summary>
        /// Worker thread count for parallel filters that support it. Set BEFORE <see cref="Initialize"/>.
        /// 0 = auto.
        /// </summary>
        public int ThreadCount
        {
            get => pFilterGraph->nb_threads;
            set => pFilterGraph->nb_threads = value;
        }

        /// <summary>
        /// Propagate a HW device context to all filters in this graph (e.g. for scale_cuda, hwupload, hwdownload).
        /// Must be called BEFORE <see cref="Initialize"/>. The buffer is internally av_buffer_ref'd by each filter.
        /// </summary>
        public void SetHWDevice(AVBufferRef* deviceRef)
        {
            if (deviceRef == null) throw new ArgumentNullException(nameof(deviceRef));
            for (uint i = 0; i < pFilterGraph->nb_filters; i++)
            {
                var fc = pFilterGraph->filters[i];
                if (fc->hw_device_ctx != null) ffmpeg.av_buffer_unref(&fc->hw_device_ctx);
                fc->hw_device_ctx = ffmpeg.av_buffer_ref(deviceRef);
            }
        }

        /// <summary>
        /// Get the hw_frames_ctx attached to a buffersink filter (i.e. the output of a HW filter chain). Returns null when the sink is SW.
        /// Useful when feeding a HW encoder created via <see cref="MediaEncoder.CreateHWVideoEncoder(MediaCodec, int, int, AVRational, AVPixelFormat, AVPixelFormat, AVHWDeviceType, string, AVBufferRef*, AVBufferRef*, int, Action{MediaCodecContext}, MediaDictionary)"/>.
        /// </summary>
        public static AVBufferRef* GetSinkHWFramesCtx(MediaFilterContext sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            return ffmpeg.av_buffersink_get_hw_frames_ctx(sink);
        }

        /// <summary>
        /// Add a buffer src filter pre-configured for HW frames via <see cref="AVBufferSrcParameters"/>.
        /// </summary>
        public MediaFilterContext AddHWVideoSrcFilter(MediaFilter filter, AVBufferRef* hwFramesCtx, int width, int height, AVPixelFormat hwPixelFormat, AVRational timebase, AVRational framerate = default, string contextName = null)
        {
            if (hwFramesCtx == null) throw new ArgumentNullException(nameof(hwFramesCtx));
            var ctx = AddFilter(filter, _ =>
            {
                var p = ffmpeg.av_buffersrc_parameters_alloc();
                try
                {
                    p->width = width;
                    p->height = height;
                    p->format = (int)hwPixelFormat;
                    p->time_base = timebase;
                    p->frame_rate = framerate;
                    p->hw_frames_ctx = ffmpeg.av_buffer_ref(hwFramesCtx);
                    ffmpeg.av_buffersrc_parameters_set(_, p).ThrowIfError();
                }
                finally { ffmpeg.av_free(p); }
            }, contextName);
            return ctx;
        }

        public string Dump()
        {
            var str = ffmpeg.avfilter_graph_dump(pFilterGraph, null);
            if (str != null)
            {
                var o = ((IntPtr)str).PtrToStringUTF8();
                ffmpeg.av_free(str);
                return o;
            }
            return null;
        }

        ///// <summary>
        ///// TODO
        ///// </summary>
        ///// <param name="graphDesc"></param>
        ///// <returns></returns>
        //public static MediaFilterGraph CreateMediaFilterGraph(string graphDesc)
        //{
        //    MediaFilterGraph filterGraph = new MediaFilterGraph();
        //    AVFilterInOut* inputs;
        //    AVFilterInOut* outputs;
        //    ffmpeg.avfilter_graph_parse2(filterGraph, graphDesc, &inputs, &outputs).ThrowIfError();
        //    AVFilterInOut* cur = inputs;
        //    for (cur = inputs; cur != null; cur = cur->next)
        //    {
        //        ffmpeg.av_log(null, (int)LogLevel.Debug, $"{((IntPtr)cur->name).PtrToStringUTF8()}{Environment.NewLine}");
        //        //filterGraph.inputs.Add(new MediaFilterContext(cur->filter_ctx));
        //    }
        //    for (cur = outputs; cur != null; cur = cur->next)
        //    {
        //        ffmpeg.av_log(null, (int)LogLevel.Debug, $"{((IntPtr)cur->name).PtrToStringUTF8()}{Environment.NewLine}");
        //        //filterGraph.outputs.Add(new MediaFilterContext(cur->filter_ctx));
        //    }

        //    foreach (var item in filterGraph)
        //    {
        //        ffmpeg.av_log(null, (int)LogLevel.Debug, $"{item.Name}{Environment.NewLine}");
        //        for (int i = 0; i < item.NbInputs; i++)
        //        {
        //            ffmpeg.av_log(null, (int)LogLevel.Debug, $"{ffmpeg.avfilter_pad_get_name(item.Ref.input_pads, i)}{Environment.NewLine}");
        //        }
        //        for (int i = 0; i < item.NbOutputs; i++)
        //        {
        //            ffmpeg.av_log(null, (int)LogLevel.Debug, $"{ffmpeg.avfilter_pad_get_name(item.Ref.output_pads, i)}{Environment.NewLine}");
        //        }
        //    }
        //    // TODO: Link
        //    filterGraph.Initialize();
        //    ffmpeg.avfilter_inout_free(&inputs);
        //    ffmpeg.avfilter_inout_free(&outputs);
        //    return filterGraph;
        //}

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
