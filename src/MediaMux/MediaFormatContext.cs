using System;
using FFmpeg.AutoGen;
using FFmpegSharp.Internal;
namespace FFmpegSharp
{
    public unsafe class MediaFormatContext : MediaFormatContextBase, IDisposable
    {
        public MediaFormatContext(AVFormatContext* pAVCodecContext, bool isDisposeByOwner = true)
            : base(pAVCodecContext)
        {
            disposedValue = !isDisposeByOwner;
        }

        public MediaFormatContext()
                    : this(ffmpeg.avformat_alloc_context(), true)
        { }



        #region IDisposable
        protected bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                ffmpeg.avio_close(pFormatContext->pb);
                ffmpeg.avformat_free_context(pFormatContext);
                disposedValue = true;
            }
        }

        ~MediaFormatContext()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
