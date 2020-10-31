using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    public unsafe class MediaFilterContext
    {
        protected AVFilterContext* pFilterContext;

        public MediaFilterContext(IntPtr pAVFilterContext)
        {
            if (pAVFilterContext == IntPtr.Zero)
                throw new FFmpegException(FFmpegException.NullReference);
            pFilterContext = (AVFilterContext*)pAVFilterContext;
            Filter = new MediaFilter(pFilterContext->filter);
        }

        internal MediaFilterContext(AVFilterContext* p)
            : this((IntPtr)p) { }

        public static implicit operator AVFilterContext*(MediaFilterContext value)
        {
            if (value == null) return null;
            return value.pFilterContext;
        }

        public MediaFilter Filter { get; private set; }

        public void Init(string options)
        {
            ffmpeg.avfilter_init_str(pFilterContext, options).ThrowIfError();
        }

        public void Init(MediaDictionary options)
        {
            ffmpeg.avfilter_init_dict(pFilterContext, options).ThrowIfError();
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

        public uint NbOutputs => pFilterContext->nb_outputs;

        public uint NbInputs => pFilterContext->nb_inputs;

        public string Name => ((IntPtr)pFilterContext->name).PtrToStringUTF8();

        public void WriteFrame(MediaFrame frame, BufferSrcFlags flags = BufferSrcFlags.KeepRef)
        {
            AddFrame(frame, flags).ThrowIfError();
        }

        public int AddFrame(MediaFrame frame, BufferSrcFlags flags = BufferSrcFlags.KeepRef)
        {
            unsafe
            {
                return ffmpeg.av_buffersrc_add_frame_flags(pFilterContext, frame, (int)flags);
            }
        }

        public int GetFrame(MediaFrame frame)
        {
            return ffmpeg.av_buffersink_get_frame(pFilterContext, frame);
        }

        public IEnumerable<MediaFrame> ReadFrame()
        {
            using (MediaFrame frame = new VideoFrame())
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
        }

        public AVFilterContext AVFilterContext { get { unsafe { return *pFilterContext; } } }

        public override bool Equals(object obj)
        {
            if (obj is MediaFilterContext filterContext)
                return filterContext.pFilterContext == pFilterContext;
            return false;
        }

        public override int GetHashCode()
        {
            return ((IntPtr)pFilterContext).ToInt32();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
