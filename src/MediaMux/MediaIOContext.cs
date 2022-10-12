using System;
using System.IO;
using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    public unsafe class MediaIOContext : Stream
    {
        protected AVIOContext* _pIOContext;

        protected bool _createByOpen;
        protected avio_alloc_context_read_packet _readfunc;
        protected avio_alloc_context_write_packet _writefunc;
        protected avio_alloc_context_seek _seekfunc;

        private Func<IntPtr, IntPtr, int, int> _read;
        private Func<IntPtr, IntPtr, int, int> _write;
        private Func<IntPtr, long, int, long> _seek;

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
            if (_buffer == null) throw new NullReferenceException();
            _read = read;
            _write = write;
            _seek = seek;

            _readfunc = read != null ? (o, b, s) => _read.Invoke((IntPtr)o, (IntPtr)b, s) : (avio_alloc_context_read_packet)null;
            _writefunc = write != null ? (o, b, s) => _write.Invoke((IntPtr)o, (IntPtr)b, s) : (avio_alloc_context_write_packet)null;
            _seekfunc = seek != null ? (o, b, s) => _seek.Invoke((IntPtr)o, b, s) : (avio_alloc_context_seek)null;
            _pIOContext = ffmpeg.avio_alloc_context(_buffer, bufferSize, _writefunc != null ? 1 : 0, null, _readfunc, _writefunc, _seekfunc);
            if (_pIOContext == null) throw new NullReferenceException();
        }

        public static MediaIOContext Open(string url, int flags, MediaDictionary options = null)
        {
            AVIOContext* pIOContext = null;
            ffmpeg.avio_open2(&pIOContext, url, flags, null, options).ThrowIfError();
            return new MediaIOContext(pIOContext, true) { _createByOpen = true };
        }

        public static MediaIOContext Link(Stream stream, int bufferSize = 4096)
        {
            Func<IntPtr, IntPtr, int, int> WriteFunc = (IntPtr opaque, IntPtr buf, int buf_size) =>
            {
#if NETSTANDARD2_0
                var buffer = new byte[buf_size];
                System.Runtime.InteropServices.Marshal.Copy((IntPtr)buf, buffer, 0, buf_size);
                stream.Write(buffer, 0, buf_size);
#else
                var buffer = new Span<byte>((void*)buf, buf_size);
                stream.Write(buffer);
#endif
                return buf_size;
            };

            Func<IntPtr, IntPtr, int, int> ReadFunc = (IntPtr opaque, IntPtr buf, int buf_size) =>
            {
#if NETSTANDARD2_0
                var buffer = new byte[buf_size];
                var count = stream.Read(buffer, 0, buf_size);
                System.Runtime.InteropServices.Marshal.Copy(buffer, 0, (IntPtr)buf, count);
#else
                var buffer = new Span<byte>((void*)buf, buf_size);
                var count = stream.Read(buffer);

#endif
                return count == 0 ? ffmpeg.AVERROR_EOF : count;
            };

            Func<IntPtr, long, int, long> SeekFunc = (IntPtr opaque, long offset, int whence) =>
            {
                if (whence == ffmpeg.AVSEEK_SIZE)
                {
                    return stream.Length;
                }
                else if (whence < 3)
                {
                    return stream.Seek(offset, (SeekOrigin)whence);
                }
                else
                {
                    return -1;
                }
            };
            return new MediaIOContext(stream.CanRead ? ReadFunc : null, stream.CanWrite ? WriteFunc : null, stream.CanSeek ? SeekFunc : null, bufferSize);
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
            Flush();
            if (!disposedValue)
            {
                if (_createByOpen)
                    ffmpeg.avio_close(_pIOContext);
                else
                {
                    ffmpeg.av_free(_pIOContext->buffer);
                    fixed (AVIOContext** ptr = &_pIOContext)
                    {
                        ffmpeg.avio_context_free(ptr);
                    }
                }
                disposedValue = true;
            }
            base.Dispose(disposing);
        }
    }
}
