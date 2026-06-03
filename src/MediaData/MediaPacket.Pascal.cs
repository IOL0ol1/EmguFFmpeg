using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    /// <summary>
    /// PascalCase accessors for the most-used <see cref="AVPacket"/> fields.
    /// </summary>
    public unsafe partial class MediaPacket
    {
        /// <summary>Stream index this packet belongs to.</summary>
        public int StreamIndex
        {
            get => pPacket->stream_index;
            set => pPacket->stream_index = value;
        }

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

        public int Flags
        {
            get => pPacket->flags;
            set => pPacket->flags = value;
        }

        public int Size => pPacket->size;

        public bool IsKeyFrame => (pPacket->flags & ffmpeg.AV_PKT_FLAG_KEY) != 0;
    }
}
