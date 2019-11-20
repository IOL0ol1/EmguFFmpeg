using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmguFFmpeg
{
    public unsafe class MediaFilterContext
    {
        protected AVFilterContext* pFilterContext;

        internal MediaFilterContext(AVFilterContext* p)
        {
            if (p == null)
                throw new FFmpegException(FFmpegException.NullReference);
            pFilterContext = p;
            Filter = new MediaFilter(p->filter);
        }

        public static implicit operator AVFilterContext*(MediaFilterContext value)
        {
            if (value == null) return null;
            return value.pFilterContext;
        }

        public MediaFilter Filter { get; private set; }

        public void Init(string options)
        {
            ffmpeg.avfilter_init_str(pFilterContext, options).ThrowExceptionIfError();
        }

        public void Init(MediaDictionary options)
        {
            ffmpeg.avfilter_init_dict(pFilterContext, options).ThrowExceptionIfError();
        }

        public MediaFilterContext LinkTo(uint srcPad, MediaFilterContext dstFilterContext, uint dstPad)
        {
            if (pFilterContext == null)
                throw new FFmpegException(FFmpegException.NeedAddToGraph);
            ffmpeg.avfilter_link(pFilterContext, srcPad, dstFilterContext, dstPad).ThrowExceptionIfError();
            return dstFilterContext;
        }

        public uint NbOutputs => pFilterContext->nb_outputs;

        public uint NbInputs => pFilterContext->nb_inputs;

        public string Name => ((IntPtr)pFilterContext->name).PtrToStringUTF8();

        public int WriteFrame(MediaFrame frame, BufferSrcFlags flags = BufferSrcFlags.KeepRef)
        {
            return ffmpeg.av_buffersrc_add_frame_flags(pFilterContext, frame, (int)flags);
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
                    ret.ThrowExceptionIfError();
                    yield return frame;
                    frame.Clear();
                }
            }
        }

        public AVFilterContext AVFilterContext => *pFilterContext;

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