using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    public unsafe partial class MediaFormatContext : IDisposable
    {
        public MediaFormatContext(AVFormatContext* pAVCodecContext, bool leaveOpen)
            : this(pAVCodecContext)
        {
            disposedValue = leaveOpen;
        }

        public MediaFormatContext()
            : this(ffmpeg.avformat_alloc_context(),false)
        { }

        /// <summary>Metadata dictionary (borrowed — do not Dispose).</summary>
        public MediaDictionary Metadata => pFormatContext->metadata == null
            ? null
            : new MediaDictionary(pFormatContext->metadata, leaveOpen: true);

        /// <summary>
        /// Guess the frame rate of a stream, based on both the container and codec information.
        /// Wraps <c>av_guess_frame_rate</c>.
        /// </summary>
        /// <param name="stream">Stream which the frame is part of.</param>
        /// <param name="frame">Frame currently being decoded; <see langword="null"/> if unavailable.</param>
        /// <returns>The guessed (valid) frame rate, 0/1 if no idea.</returns>
        public AVRational GuessFrameRate(MediaStream stream, MediaFrame frame = null)
        {
            return ffmpeg.av_guess_frame_rate(pFormatContext, stream, frame);
        }

        #region IDisposable
        // Default `false` (owned). The (ptr, leaveOpen) ctor flips to `true` for borrowed pointers.
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
