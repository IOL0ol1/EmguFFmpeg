using System;
using FFmpeg.AutoGen;

namespace FFmpegSharp
{
    public unsafe partial class MediaCodec
    {
        /// <summary>
        /// Be careful!!!
        /// </summary>
        protected internal AVCodec* pCodec = null;

        /// <summary>
        /// const AVCodec*
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVCodec*(MediaCodec value)
        {
            return value == null ? null : value.pCodec;
        }

        public MediaCodec(AVCodec* pAVCodec)
        {
            pCodec = pAVCodec;
        }

        public MediaCodec(IntPtr pAVCodec)
            : this((AVCodec*)pAVCodec)
        { }

        public AVCodec Const => *pCodec;

        public AVMediaType Type
        {
            get => pCodec->type;
            set => pCodec->type = value;
        }

        public AVCodecID Id
        {
            get => pCodec->id;
            set => pCodec->id = value;
        }

        public int Capabilities
        {
            get => pCodec->capabilities;
            set => pCodec->capabilities = value;
        }

        public byte MaxLowres
        {
            get => pCodec->max_lowres;
            set => pCodec->max_lowres = value;
        }

    }
}
