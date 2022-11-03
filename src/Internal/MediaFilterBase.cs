using FFmpeg.AutoGen;
namespace FFmpegSharp.Internal
{
    public abstract unsafe partial class MediaFilterBase
    {
        /// <summary>
        /// Be careful!!!
        /// </summary>
        protected internal AVFilter* pFilter = null;

        /// <summary>
        /// const AVFilter*
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVFilter*(MediaFilterBase value)
        {
            if (value == null) return null;
            return value.pFilter;
        }

        public MediaFilterBase(AVFilter* value)
        {
            pFilter = value;
        }

        public AVFilter Ref => *pFilter;

        public int Flags
        {
            get => pFilter->flags;
            set => pFilter->flags = value;
        }

        public byte NbInputs
        {
            get => pFilter->nb_inputs;
            set => pFilter->nb_inputs = value;
        }

        public byte NbOutputs
        {
            get => pFilter->nb_outputs;
            set => pFilter->nb_outputs = value;
        }

        public byte FormatsState
        {
            get => pFilter->formats_state;
            set => pFilter->formats_state = value;
        }

        public AVFilter_formats Formats
        {
            get => pFilter->formats;
            set => pFilter->formats = value;
        }

        public int PrivSize
        {
            get => pFilter->priv_size;
            set => pFilter->priv_size = value;
        }

        public int FlagsInternal
        {
            get => pFilter->flags_internal;
            set => pFilter->flags_internal = value;
        }

    }
}
