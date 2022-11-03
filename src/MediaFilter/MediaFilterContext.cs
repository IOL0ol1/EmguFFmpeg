using FFmpeg.AutoGen;
using FFmpegSharp.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;

namespace FFmpegSharp
{
    public unsafe class MediaFilterContext : MediaFilterContextBase
    {

        internal MediaFilterContext(AVFilterContext* filterContext) : base(filterContext)
        { }


        public MediaFilter Filter => new MediaFilter(pFilterContext->filter);

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
                    if (ret > 0) frame.Unref();
                }
            }
            finally { if (dstframe == null) frame?.Dispose(); }
        }
        // TODO
        /*
         * AVMediaType 	av_buffersink_get_type (const AVFilterContext *ctx)
         * AVRational 	av_buffersink_get_time_base (const AVFilterContext *ctx)
         * int 	av_buffersink_get_format (const AVFilterContext *ctx)
         * AVRational 	av_buffersink_get_frame_rate (const AVFilterContext *ctx)
         * int 	av_buffersink_get_w (const AVFilterContext *ctx)
         * int 	av_buffersink_get_h (const AVFilterContext *ctx)
         * AVRational 	av_buffersink_get_sample_aspect_ratio (const AVFilterContext *ctx)
         * int 	av_buffersink_get_channels (const AVFilterContext *ctx)
         * int 	av_buffersink_get_ch_layout (const AVFilterContext *ctx, AVChannelLayout *ch_layout)
         * int 	av_buffersink_get_sample_rate (const AVFilterContext *ctx)
         * AVBufferRef * 	av_buffersink_get_hw_frames_ctx (const AVFilterContext *ctx)
         */
        #endregion
    }
}
