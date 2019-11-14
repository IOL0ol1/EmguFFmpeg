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
        }

        public string Name => ((IntPtr)pFilterInOut->name).PtrToStringUTF8();

        public int PadIdx
        {
            get => pFilterInOut->pad_idx;
            set => pFilterInOut->pad_idx = value;
        }

        public MediaFilterInOut Next => pFilterInOut->next == null ? null : new MediaFilterInOut(pFilterInOut->next);
    }
}