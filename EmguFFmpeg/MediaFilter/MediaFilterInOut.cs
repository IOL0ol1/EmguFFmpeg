using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmguFFmpeg
{
    public unsafe class MediaFilterInOut
    {
        protected AVFilterInOut* pFilterInOut;

        internal MediaFilterInOut(AVFilterInOut* p)
        {
            pFilterInOut = p;
            Filter = new MediaFilter(p->filter_ctx);
        }

        public MediaFilter Filter { get; }

        public string Name => ((IntPtr)pFilterInOut->name).PtrToStringUTF8();

        public int PadIdx => pFilterInOut->pad_idx;

        //public MediaFilterInOut Next => pFilterInOut->next == null ? null : new MediaFilterInOut(pFilterInOut->next);

        public void WriteFrame(MediaFrame frame, int flags = ffmpeg.AV_BUFFERSINK_FLAG_PEEK)
        {
            Filter.AddFrame(frame, flags).ThrowExceptionIfError();
        }

        public IEnumerable<MediaFrame> ReadFrame(MediaFrame frame)
        {
            while (true)
            {
                int ret = Filter.GetFrame(frame);
                if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
                    break;
                ret.ThrowExceptionIfError();
                yield return frame;
                frame.Clear();
            }
        }
    }
}