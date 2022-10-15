using System;
using FFmpeg.AutoGen;
using FFmpegSharp.Internal;

namespace FFmpegSharp
{
    public unsafe class MediaPacket : MediaPacketBase, IDisposable, ICloneable
    {
        public MediaPacket(AVPacket* pAVPacket, bool isDisposeByOwner = true)
            : base(pAVPacket)
        {
            disposedValue = !isDisposeByOwner;
        }

        public MediaPacket()
            : this(ffmpeg.av_packet_alloc(), true)
        { }

        /// <summary>
        /// <see cref="ffmpeg.av_packet_unref(AVPacket*)"/>
        /// </summary>
        public void Unref()
        {
            ffmpeg.av_packet_unref(pPacket);
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
            MediaPacket packet = new MediaPacket();
            AVPacket* dstpkt = packet;
            int ret;
            if ((ret = ffmpeg.av_packet_ref(dstpkt, pPacket)) < 0)
            {
                ffmpeg.av_packet_free(&dstpkt);
                throw new FFmpegException(ret);
            }
            ffmpeg.av_packet_copy_props(dstpkt, pPacket).ThrowIfError();
            return packet;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
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

        ~MediaPacket()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}
