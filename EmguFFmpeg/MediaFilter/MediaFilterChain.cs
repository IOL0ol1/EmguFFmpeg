using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmguFFmpeg
{
    public unsafe class MediaFilterChain
    {
        protected AVFilterLink* pFilterLink;
        protected AVFilterInOut* pFilterInOut;

        public MediaFilterChain()
        {
        }
    }
}