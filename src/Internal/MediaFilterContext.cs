using System;
using FFmpeg.AutoGen;

namespace FFmpegSharp
{
    public unsafe partial class MediaFilterContext
    {
        /// <summary>
        /// Be careful!!!
        /// </summary>
        protected internal AVFilterContext* pFilterContext = null;

        /// <summary>
        /// const AVFilterContext*
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVFilterContext*(MediaFilterContext value)
        {
            return value == null ? null : value.pFilterContext;
        }

        public MediaFilterContext(AVFilterContext* pAVFilterContext)
        {
            pFilterContext = pAVFilterContext;
        }

        public MediaFilterContext(IntPtr pAVFilterContext)
            : this((AVFilterContext*)pAVFilterContext)
        { }

        public AVFilterContext Const => *pFilterContext;

        public uint NbInputs
        {
            get => pFilterContext->nb_inputs;
            set => pFilterContext->nb_inputs = value;
        }

        public uint NbOutputs
        {
            get => pFilterContext->nb_outputs;
            set => pFilterContext->nb_outputs = value;
        }

        public int ThreadType
        {
            get => pFilterContext->thread_type;
            set => pFilterContext->thread_type = value;
        }

        public int NbThreads
        {
            get => pFilterContext->nb_threads;
            set => pFilterContext->nb_threads = value;
        }

        public int IsDisabled
        {
            get => pFilterContext->is_disabled;
            set => pFilterContext->is_disabled = value;
        }

        public uint Ready
        {
            get => pFilterContext->ready;
            set => pFilterContext->ready = value;
        }

        public int ExtraHwFrames
        {
            get => pFilterContext->extra_hw_frames;
            set => pFilterContext->extra_hw_frames = value;
        }

    }
}
