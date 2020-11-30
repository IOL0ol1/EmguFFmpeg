using System;
using System.IO;

using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    public unsafe class MediaIOContext : Stream, IDisposable
    {
        protected AVIOContext* pIOContext;

        public static implicit operator AVIOContext*(MediaIOContext value)
        {
            if (value == null) return null;
            return value.pIOContext;
        }

        public MediaIOContext()
        {
            pIOContext = ffmpeg.avio_alloc_context()
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => throw new NotImplementedException();

        public override bool CanSeek => throw new NotImplementedException();

        public override bool CanWrite => throw new NotImplementedException();

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private bool disposedValue;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {

                if (pIOContext != null)
                    ffmpeg.av_freep(&pIOContext->buffer);
                fixed (AVIOContext** p = &pIOContext)
                    ffmpeg.avio_context_free(p);
                base.Dispose(disposing);
                disposedValue = true;
            }
        }
    }
}
