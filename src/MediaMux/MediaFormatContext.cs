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
                    : this(ffmpeg.avformat_alloc_context())
        { }

        #region IDisposable
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (pFormatContext != null)
                {
                    if (pFormatContext->iformat != null)
                    {
                        fixed (AVFormatContext** ppFormatContext = &pFormatContext)
                            ffmpeg.avformat_close_input(ppFormatContext);
                    }
                    else
                    {
                        if ((pFormatContext->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
                            ffmpeg.avio_close(pFormatContext->pb);
                        ffmpeg.avformat_free_context(pFormatContext);
                    }
                    pFormatContext = null;
                }
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
