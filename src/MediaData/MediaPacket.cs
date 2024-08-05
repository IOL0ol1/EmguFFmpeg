using System;
using FFmpeg.AutoGen;


namespace FFmpegSharp
{
    public unsafe partial class MediaPacket : IDisposable, ICloneable
    {
        public MediaPacket(AVPacket* pAVPacket, bool leaveOpen)
            : this(pAVPacket)
        {
            disposedValue = leaveOpen;
        }


        public MediaPacket()
            : this(ffmpeg.av_packet_alloc(), false)
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
            return new MediaPacket(ffmpeg.av_packet_clone(this));
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        #region IDisposable Support

        private bool disposedValue = true;

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
