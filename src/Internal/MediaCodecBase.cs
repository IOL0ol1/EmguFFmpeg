using FFmpeg.AutoGen;
namespace FFmpegSharp.Internal
{
    public abstract unsafe partial class MediaCodecBase
    {
        protected AVCodec* pCodec = null;

        public static implicit operator AVCodec*(MediaCodecBase value)
        {
            if(value == null) return null;
            return value.pCodec;
        }

        public MediaCodecBase(AVCodec* value)
        {
            pCodec = value;
        }

        public AVCodec Ref => *pCodec;

        public AVMediaType Type
        {
            get=> pCodec->type;
            set=> pCodec->type = value;
        }

        public AVCodecID Id
        {
            get=> pCodec->id;
            set=> pCodec->id = value;
        }

        public int Capabilities
        {
            get=> pCodec->capabilities;
            set=> pCodec->capabilities = value;
        }

        public byte MaxLowres
        {
            get=> pCodec->max_lowres;
            set=> pCodec->max_lowres = value;
        }

    }
}
