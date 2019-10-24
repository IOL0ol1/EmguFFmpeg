using FFmpeg.AutoGen;

using System;

namespace EmguFFmpeg
{
    public unsafe class MediaPacket : IDisposable
    {
        protected AVPacket* pPacket;

        public MediaPacket()
        {
            pPacket = ffmpeg.av_packet_alloc();
        }

        public AVPacket AVPacket => *pPacket;

        public long ConvergenceDuration
        {
            get => pPacket->convergence_duration;
            set => pPacket->convergence_duration = value;
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

        public int Flags
        {
            get => pPacket->flags;
            set => pPacket->flags = value;
        }

        public long Pos
        {
            get => pPacket->pos;
            set => pPacket->pos = value;
        }

        public long Pts
        {
            get => pPacket->pts;
            set => pPacket->pts = value;
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

        public static implicit operator AVPacket*(MediaPacket value)
        {
            if (value == null) return null;
            return value.pPacket;
        }

        /// <summary>
        /// <see cref="ffmpeg.av_packet_unref(AVPacket*)"/>
        /// </summary>
        public void Wipe()
        {
            ffmpeg.av_packet_unref(pPacket);
        }

        #region IDisposable Support

        public void Dispose()
        {
            fixed (AVPacket** ppPacket = &pPacket)
            {
                ffmpeg.av_packet_free(ppPacket);
            }
        }

        #endregion
    }
}