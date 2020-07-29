using FFmpeg.AutoGen;

using System;

namespace EmguFFmpeg
{
    public class MediaPacket : IDisposable, ICloneable
    {
        protected unsafe AVPacket* pPacket;

        public MediaPacket()
        {
            unsafe
            {
                pPacket = ffmpeg.av_packet_alloc();
            }
        }

        public AVPacket AVPacket { get { unsafe { return *pPacket; } } }

        public long ConvergenceDuration
        {
            get { unsafe { return pPacket->convergence_duration; } }
            set { unsafe { pPacket->convergence_duration = value; } }
        }

        public long Dts
        {
            get { unsafe { return pPacket->dts; } }
            set { unsafe { pPacket->dts = value; } }
        }

        public long Duration
        {
            get { unsafe { return pPacket->duration; } }
            set { unsafe { pPacket->duration = value; } }
        }

        public int Flags
        {
            get { unsafe { return pPacket->flags; } }
            set { unsafe { pPacket->flags = value; } }
        }

        public long Pos
        {
            get { unsafe { return pPacket->pos; } }
            set { unsafe { pPacket->pos = value; } }
        }

        public long Pts
        {
            get { unsafe { return pPacket->pts; } }
            set { unsafe { pPacket->pts = value; } }
        }

        public int Size
        {
            get { unsafe { return pPacket->size; } }
            set { unsafe { pPacket->size = value; } }
        }

        public int StreamIndex
        {
            get { unsafe { return pPacket->stream_index; } }
            set { unsafe { pPacket->stream_index = value; } }
        }

        public unsafe static implicit operator AVPacket*(MediaPacket value)
        {
            if (value == null) return null;
            return value.pPacket;
        }

        /// <summary>
        /// <see cref="ffmpeg.av_packet_unref(AVPacket*)"/>
        /// </summary>
        public void Clear()
        {
            unsafe
            {
                ffmpeg.av_packet_unref(pPacket);
            }
        }

        /// <summary>
        /// Deep copy
        /// <para><see cref="ffmpeg.av_packet_ref(AVPacket*, AVPacket*)"/></para>
        /// <para><see cref="ffmpeg.av_packet_copy_props(AVPacket*, AVPacket*)"/></para>
        /// </summary>
        /// <exception cref="FFmpegException"/>
        /// <returns></returns>
        public MediaPacket Clone()
        {
            unsafe
            {
                MediaPacket packet = new MediaPacket();
                AVPacket* dstpkt = packet;
                int ret;
                if ((ret = ffmpeg.av_packet_ref(dstpkt, pPacket)) < 0)
                {
                    ffmpeg.av_packet_free(&dstpkt);
                    throw new FFmpegException(ret);
                }
                ffmpeg.av_packet_copy_props(dstpkt, pPacket).ThrowExceptionIfError();
                return packet;
            }
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            unsafe
            {
                if (!disposedValue)
                {
                    fixed (AVPacket** ppPacket = &pPacket)
                    {
                        ffmpeg.av_packet_free(ppPacket);
                    }

                    disposedValue = true;
                }
            }
        }

        ~MediaPacket()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}