using System;
using System.IO;
using FFmpeg.AutoGen;

namespace FFmpegSharp
{
    public unsafe class MediaIOContext : Stream
    {
        protected AVIOContext* _pIOContext;

        public static implicit operator AVIOContext*(MediaIOContext value)
        {
            if (value == null) return null;
            return value._pIOContext;
        }

        public MediaIOContext(AVIOContext* pIOContext, bool isDisposeByOwner = true)
        {
            if (pIOContext == null) throw new NullReferenceException();
            _pIOContext = pIOContext;
            disposedValue = !isDisposeByOwner;
        }

        public MediaIOContext(
            Func<IntPtr, IntPtr, int, int> read,
            Func<IntPtr, IntPtr, int, int> write = null,
            Func<IntPtr, long, int, long> seek = null,
            int bufferSize = 4096)
        {
            var _buffer = (byte*)ffmpeg.av_malloc((ulong)bufferSize);
            if (_buffer == null) throw new OutOfMemoryException();
            var _readfunc = read != null ? (avio_alloc_context_read_packet_func)((o, b, s) => read.Invoke((IntPtr)o, (IntPtr)b, s)) : null;
            var _writefunc = write != null ? (avio_alloc_context_write_packet)((o, b, s) => write.Invoke((IntPtr)o, (IntPtr)b, s)) : null;
            var _seekfunc = seek != null ? (avio_alloc_context_seek)((o, b, s) => seek.Invoke((IntPtr)o, b, s)) : null;
            _pIOContext = ffmpeg.avio_alloc_context(_buffer, bufferSize, _writefunc != null ? 1 : 0, null, _readfunc, _writefunc, _seekfunc);
            if (_pIOContext == null) throw new NullReferenceException();
        }

        public static MediaIOContext Open(string url, int flags, MediaDictionary options = null)
        {
            AVIOContext* pIOContext = null;
            ffmpeg.avio_open2(&pIOContext, url, flags, null, options).ThrowIfError();
            return new MediaIOContext(pIOContext);
        }

        public static MediaIOContext Open(string url, int flags, AVIOInterruptCB interrupt, MediaDictionary options = null)
        {
            AVIOContext* pIOContext = null;
            ffmpeg.avio_open2(&pIOContext, url, flags, &interrupt, options).ThrowIfError();
            return new MediaIOContext(pIOContext, true);
        }

        public override bool CanRead => _pIOContext->read_packet.Pointer != IntPtr.Zero;

        public override bool CanSeek => _pIOContext->seekable != 0;

        public override bool CanWrite => _pIOContext->write_flag != 0;

        public override long Length => ffmpeg.avio_size(_pIOContext);

        public override long Position { get => ffmpeg.avio_tell(_pIOContext).ThrowIfError(); set => Seek(value, SeekOrigin.Begin); }

        public override void Flush() => ffmpeg.avio_flush(_pIOContext);

        public override int Read(byte[] buffer, int offset, int count)
        {
            fixed (byte* ptr = buffer)
            {
                var ret = ffmpeg.avio_read(_pIOContext, ptr + offset, count);
                if (ret < 0)
                {
                    if (ret == ffmpeg.AVERROR_EOF) return 0;
                    ret.ThrowIfError();
                }
                return ret;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var whence = 0;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    whence = 0;
                    break;
                case SeekOrigin.Current:
                    whence = 1;
                    break;
                case SeekOrigin.End:
                    whence = 2;
                    break;
                default:
                    break;
            }
            return ffmpeg.avio_seek(_pIOContext, offset, whence).ThrowIfError();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            fixed (byte* ptr = buffer)
            {
                ffmpeg.avio_write(_pIOContext, ptr + offset, count);
            }
        }

        private bool disposedValue;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (_pIOContext != null)
                {
                    ffmpeg.avio_close(_pIOContext);
                    _pIOContext = null;
                }
                disposedValue = true;
            }
            base.Dispose(disposing);
        }
    }
}
