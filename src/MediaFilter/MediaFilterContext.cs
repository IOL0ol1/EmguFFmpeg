using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;


namespace FFmpeg.Sharp
{
    public unsafe partial class MediaFilterContext  
    {
        #region BufferSrc flags (av_buffersrc.h macros not exposed by AutoGen)
        /// <summary>Do not check for format changes. Only relevant for buffersrc.</summary>
        public const int BufferSrcFlagNoCheckFormat = 1;
        /// <summary>Immediately push the frame to the output (default for buffersrc). Only relevant for buffersrc.</summary>
        public const int BufferSrcFlagPush          = 2;
        /// <summary>
        /// Keep a reference to the frame. Without this flag the source takes ownership of the frame.
        /// Equivalent to the C macro <c>AV_BUFFERSRC_FLAG_KEEP_REF (= 8)</c>.
        /// </summary>
        public const int BufferSrcFlagKeepRef       = 8;
        #endregion


        public void Init(string options)
        {
            ffmpeg.avfilter_init_str(pFilterContext, options).ThrowIfError();
        }

        public void Init(MediaDictionary options)
        {
            fixed (AVDictionary** opts = &options.pDictionary)
            {
                ffmpeg.avfilter_init_dict(pFilterContext, opts).ThrowIfError();
            }
        }

        /// <summary>
        /// link current filter's <paramref name="srcOutPad"/> to <paramref name="dstFilterContext"/>'s <paramref name="dstInPad"/>
        /// </summary>
        /// <param name="srcOutPad"></param>
        /// <param name="dstFilterContext"></param>
        /// <param name="dstInPad"></param>
        /// <returns></returns>
        public MediaFilterContext LinkTo(uint srcOutPad, MediaFilterContext dstFilterContext, uint dstInPad = 0)
        {
            ffmpeg.avfilter_link(pFilterContext, srcOutPad, dstFilterContext, dstInPad).ThrowIfError();
            return dstFilterContext;
        }

        public string Name => ((IntPtr)pFilterContext->name).PtrToStringUTF8();

        #region Src
        public void WriteFrame(MediaFrame frame, int flags, long? pts = null)
        {
            if (frame == null && pts != null)
                ffmpeg.av_buffersrc_close(pFilterContext, pts.Value, (uint)flags).ThrowIfError();
            else
                ffmpeg.av_buffersrc_add_frame_flags(pFilterContext, frame, flags).ThrowIfError();
        }

        /// <summary>
        /// Signal end-of-stream to this buffersrc filter. Equivalent to <c>WriteFrame(null, 0)</c>
        /// but communicates intent clearly. Call once after the last <see cref="WriteFrame"/> to let
        /// downstream filters drain their remaining output.
        /// </summary>
        public void FlushSrc() => ffmpeg.av_buffersrc_add_frame_flags(pFilterContext, null, 0).ThrowIfError();

        public void ParametersSet(Action<AVBufferSrcParameters> set)
        {
            if (set != null)
            {
                var parameters = ffmpeg.av_buffersrc_parameters_alloc();
                set(*parameters);
                ffmpeg.av_buffersrc_parameters_set(pFilterContext, parameters).ThrowIfError();
                ffmpeg.av_free(parameters);
            }
        }
        #endregion

        #region Sink
        public int GetFrame(MediaFrame frame, int flags = 0)
        {
            return ffmpeg.av_buffersink_get_frame_flags(pFilterContext, frame, flags);
        }

        public IEnumerable<MediaFrame> ReadFrame(MediaFrame dstframe = null)
        {
            MediaFrame frame = dstframe ?? new MediaFrame();
            try
            {
                while (true)
                {
                    int ret = GetFrame(frame);
                    if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
                        break;
                    ret.ThrowIfError();
                    yield return frame;
                    frame.Unref();
                }
            }
            finally { if (dstframe == null) frame?.Dispose(); }
        }

        // Buffersink getters — call only on buffersink/abuffersink contexts and only AFTER avfilter_graph_config.
        public AVMediaType BufferSinkGetType() => ffmpeg.av_buffersink_get_type(pFilterContext);
        public AVRational BufferSinkGetTimeBase() => ffmpeg.av_buffersink_get_time_base(pFilterContext);
        public int BufferSinkGetFormat() => ffmpeg.av_buffersink_get_format(pFilterContext);
        public AVRational BufferSinkGetFrameRate() => ffmpeg.av_buffersink_get_frame_rate(pFilterContext);
        public int BufferSinkGetWidth() => ffmpeg.av_buffersink_get_w(pFilterContext);
        public int BufferSinkGetHeight() => ffmpeg.av_buffersink_get_h(pFilterContext);
        public AVRational BufferSinkGetSampleAspectRatio() => ffmpeg.av_buffersink_get_sample_aspect_ratio(pFilterContext);
        public int BufferSinkGetChannels() => ffmpeg.av_buffersink_get_channels(pFilterContext);
        public AVChannelLayout BufferSinkGetChannelLayout()
        {
            var layout = new AVChannelLayout();
            ffmpeg.av_buffersink_get_ch_layout(pFilterContext, &layout).ThrowIfError();
            return layout;
        }
        public int BufferSinkGetSampleRate() => ffmpeg.av_buffersink_get_sample_rate(pFilterContext);
        public AVBufferRef* BufferSinkGetHwFramesCtx() => ffmpeg.av_buffersink_get_hw_frames_ctx(pFilterContext);
        #endregion
    }
}
