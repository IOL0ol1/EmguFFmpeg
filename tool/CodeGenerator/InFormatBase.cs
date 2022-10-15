using FFmpeg.AutoGen;
namespace FFmpegSharp.Internal
{
    public abstract unsafe partial class InFormatBase
    {
        protected AVInputFormat* pInputFormat = null;

        public static implicit operator AVInputFormat*(InFormatBase value)
        {
            if(value == null) return null;
            return value.pInputFormat;
        }

        public InFormatBase(AVInputFormat* value)
        {
            pInputFormat = value;
        }

        public AVInputFormat Ref => *pInputFormat;

        public int Flags
        {
            get=> pInputFormat->flags;
            set=> pInputFormat->flags = value;
        }

        public int RawCodecId
        {
            get=> pInputFormat->raw_codec_id;
            set=> pInputFormat->raw_codec_id = value;
        }

        public int PrivDataSize
        {
            get=> pInputFormat->priv_data_size;
            set=> pInputFormat->priv_data_size = value;
        }

        public int FlagsInternal
        {
            get=> pInputFormat->flags_internal;
            set=> pInputFormat->flags_internal = value;
        }

    }
}
