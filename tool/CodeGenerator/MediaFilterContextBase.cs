using FFmpeg.AutoGen;
namespace FFmpegSharp.Internal
{
    public abstract unsafe partial class MediaFilterContextBase
    {
        /// <summary>
        /// Be careful!!!
        /// </summary>
        protected internal AVFilterContext* pFilterContext = null;

        /// <summary>
        /// const AVFilterContext*
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVFilterContext*(MediaFilterContextBase value)
        {
            if (value == null) return null;
            return value.pFilterContext;
        }

        public MediaFilterContextBase(AVFilterContext* value)
        {
            pFilterContext = value;
        }

        public AVFilterContext Ref => *pFilterContext;

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

        public int IsDisabled
        {
            get => pFilterContext->is_disabled;
            set => pFilterContext->is_disabled = value;
        }

        public int NbThreads
        {
            get => pFilterContext->nb_threads;
            set => pFilterContext->nb_threads = value;
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
