using System;
using FFmpeg.AutoGen;

namespace FFmpegSharp
{
    public unsafe partial class MediaFilter
    {
        /// <summary>
        /// Be careful!!!
        /// </summary>
        protected internal AVFilter* pFilter = null;

        /// <summary>
        /// const AVFilter*
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVFilter*(MediaFilter value)
        {
            return value == null ? null : value.pFilter;
        }

        public MediaFilter(AVFilter* pAVFilter)
        {
            pFilter = pAVFilter;
        }

        public MediaFilter(IntPtr pAVFilter)
            : this((AVFilter*)pAVFilter)
        { }

        public AVFilter Const => *pFilter;

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
