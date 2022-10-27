using FFmpeg.AutoGen;
namespace FFmpegSharp.Internal
{
    public abstract unsafe partial class MediaPacketBase
    {
        protected AVPacket* pPacket = null;

        public static implicit operator AVPacket*(MediaPacketBase value)
        {
            if (value == null) return null;
            return value.pPacket;
        }

        public MediaPacketBase(AVPacket* value)
        {
            pPacket = value;
        }

        public AVPacket Ref => *pPacket;

        public long Pts
        {
            get => pPacket->pts;
            set => pPacket->pts = value;
        }

        public long Dts
        {
            get => pPacket->dts;
            set => pPacket->dts = value;
        }

        public int Size
        {
            get => pPacket->size;
            set => pPacket->size = value;
        }

        public int StreamIndex
        {
            get => pPacket->stream_index;
            set => pPacket->stream_index = value;
        }

        public int Flags
        {
            get => pPacket->flags;
            set => pPacket->flags = value;
        }

        public int SideDataElems
        {
            get => pPacket->side_data_elems;
            set => pPacket->side_data_elems = value;
        }

        public long Duration
        {
            get => pPacket->duration;
            set => pPacket->duration = value;
        }

        public long Pos
        {
            get => pPacket->pos;
            set => pPacket->pos = value;
        }

        public AVRational TimeBase
        {
            get => pPacket->time_base;
            set => pPacket->time_base = value;
        }

    }
}
