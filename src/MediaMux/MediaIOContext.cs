using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    /// <summary>
    /// Bridges a managed <see cref="Stream"/> to FFmpeg's <see cref="AVIOContext"/>.
    /// <para>
    /// All callbacks are exception-safe: managed exceptions thrown by the underlying stream are caught and
    /// translated into negative AVERROR codes — they NEVER cross the native FFmpeg frame.
    /// The last managed exception is preserved on <see cref="LastError"/> and re-thrown by the next call from
    /// managed code (Demuxer/Muxer Read/Write/Seek).
    /// </para>
    /// </summary>
    public unsafe class MediaIOContext : Stream
    {
        protected AVIOContext* _pIOContext;
        // Delegates must be pinned for the lifetime of the AVIOContext.
        // We keep field references AND GCHandles — fields alone are not enough because the JIT can elide
        // them, and the GC is allowed to collect/move callable trampolines around.
        private avio_alloc_context_read_packet _read;
        private avio_alloc_context_write_packet _write;
        private avio_alloc_context_seek _seek;
        private GCHandle _readHandle;
        private GCHandle _writeHandle;
        private GCHandle _seekHandle;
        private Stream stream;
        private readonly bool _leaveStreamOpen;

        /// <summary>
        /// Last managed exception thrown by the bridged <see cref="Stream"/>.
        /// </summary>
        public Exception LastError { get; private set; }

        public static implicit operator AVIOContext*(MediaIOContext value)
        {
            if (value == null) return null;
            return value._pIOContext;
        }

        public MediaIOContext(AVIOContext* pIOContext, bool leaveOpen)
        {
            if (pIOContext == null) throw new ArgumentNullException(nameof(pIOContext));
            _pIOContext = pIOContext;
            disposedValue = leaveOpen;
            _leaveStreamOpen = true; // we don't own a managed stream in this overload
        }

        /// <summary>
        /// Wrap a managed <see cref="Stream"/> as an FFmpeg I/O context.
        /// </summary>
        /// <param name="stream">Underlying stream. MUST remain open while this <see cref="MediaIOContext"/> is in use.</param>
        /// <param name="bufferSize">Internal FFmpeg buffer size, in bytes. Default matches libavformat's own IO buffer (32 KiB).</param>
        /// <param name="leaveOpen">
        /// When <see langword="true"/> (the default and recommended), this context will NOT dispose <paramref name="stream"/>
        /// on its own dispose. Set to <see langword="false"/> only if you want the context to take ownership.
        /// </param>
        public MediaIOContext(Stream stream, int bufferSize = 32768, bool leaveOpen = true)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));

            this.stream = stream;
            this._leaveStreamOpen = leaveOpen;

            var _buffer = (byte*)ffmpeg.av_malloc((ulong)bufferSize);
            if (_buffer == null) throw new OutOfMemoryException();
            try
            {
                _read = ReadCallback;
                _write = WriteCallback;
                _seek = SeekCallback;
                _readHandle = GCHandle.Alloc(_read);
                _writeHandle = GCHandle.Alloc(_write);
                _seekHandle = GCHandle.Alloc(_seek);

                _pIOContext = ffmpeg.avio_alloc_context(
                    _buffer, bufferSize, stream.CanWrite ? 1 : 0, null,
                    stream.CanRead ? _read : null,
                    stream.CanWrite ? _write : null,
                    stream.CanSeek ? _seek : null);
                if (_pIOContext == null)
                {
                    ffmpeg.av_free(_buffer);
                    if (_readHandle.IsAllocated) _readHandle.Free();
                    if (_writeHandle.IsAllocated) _writeHandle.Free();
                    if (_seekHandle.IsAllocated) _seekHandle.Free();
                    throw new OutOfMemoryException("avio_alloc_context returned null");
                }
                disposedValue = false;
            }
            catch
            {
                // _buffer freed (or absorbed into the context which we then free) above; nothing else to do.
                throw;
            }
        }

        // ---- Native callbacks ----
        // Contract per FFmpeg: return number of bytes read/written, or a negative AVERROR on failure.
        // We must NOT let managed exceptions propagate across the native/managed boundary.

        private int WriteCallback(void* opaque, byte* buf, int buf_size)
        {
            try
            {
#if NETSTANDARD2_0
                var pooled = ArrayPool<byte>.Shared.Rent(buf_size);
                try
                {
                    Marshal.Copy((IntPtr)buf, pooled, 0, buf_size);
                    stream.Write(pooled, 0, buf_size);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(pooled);
                }
#else
                stream.Write(new ReadOnlySpan<byte>(buf, buf_size));
#endif
                return buf_size;
            }
            catch (Exception ex)
            {
                LastError = ex;
                return ffmpeg.AVERROR_EXTERNAL;
            }
        }

        private int ReadCallback(void* opaque, byte* buf, int buf_size)
        {
            try
            {
#if NETSTANDARD2_0
                var pooled = ArrayPool<byte>.Shared.Rent(buf_size);
                int count;
                try
                {
                    count = stream.Read(pooled, 0, buf_size);
                    if (count > 0)
                        Marshal.Copy(pooled, 0, (IntPtr)buf, count);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(pooled);
                }
#else
                int count = stream.Read(new Span<byte>(buf, buf_size));
#endif
                return count == 0 ? ffmpeg.AVERROR_EOF : count;
            }
            catch (Exception ex)
            {
                LastError = ex;
                return ffmpeg.AVERROR_EXTERNAL;
            }
        }

        private long SeekCallback(void* opaque, long offset, int whence)
        {
            try
            {
                if (whence == ffmpeg.AVSEEK_SIZE)
                {
                    return stream.Length;
                }
                else if (whence < 3)
                {
                    return stream.Seek(offset, (SeekOrigin)whence);
                }
                return -1;
            }
            catch (Exception ex)
            {
                LastError = ex;
                return ffmpeg.AVERROR_EXTERNAL;
            }
        }

        public static MediaIOContext Open(string url, int flags, MediaDictionary options = null)
        {
            AVIOContext* pIOContext = null;
            if (options == null)
            {
                ffmpeg.avio_open2(&pIOContext, url, flags, null, null).ThrowIfError();
            }
            else
            {
                fixed (AVDictionary** pOptions = &options.pDictionary)
                    ffmpeg.avio_open2(&pIOContext, url, flags, null, pOptions).ThrowIfError();
            }
            return new MediaIOContext(pIOContext, false);
        }

        public static MediaIOContext Open(string url, int flags, AVIOInterruptCB interrupt, MediaDictionary options = null)
        {
            AVIOContext* pIOContext = null;
            if (options == null)
            {
                ffmpeg.avio_open2(&pIOContext, url, flags, &interrupt, null).ThrowIfError();
            }
            else
            {
                fixed (AVDictionary** pOptions = &options.pDictionary)
                    ffmpeg.avio_open2(&pIOContext, url, flags, &interrupt, pOptions).ThrowIfError();
            }
            return new MediaIOContext(pIOContext, false);
        }

        /// <summary>
        /// Accept and allocate a client context on a server context. Wraps <c>avio_accept</c>.
        /// </summary>
        /// <param name="client">
        /// On success (ret &gt;= 0) the accepted client context, OWNED by the caller — disposing it closes the
        /// client connection. On failure (ret &lt; 0) set to <see langword="null"/>.
        /// </param>
        /// <returns>The raw FFmpeg return code: &gt;= 0 on success, a negative AVERROR on failure. Callers loop and break on error.</returns>
        public int Accept(out MediaIOContext client)
        {
            AVIOContext* pClient = null;
            var ret = ffmpeg.avio_accept(_pIOContext, &pClient);
            client = ret >= 0 ? new MediaIOContext(pClient, false) : null;
            return ret;
        }

        /// <summary>
        /// Perform one step of the protocol handshake to accept a new client (on a context returned by
        /// <see cref="Accept"/>). Wraps <c>avio_handshake</c>.
        /// </summary>
        /// <returns>
        /// The raw FFmpeg return code: &gt; 0 means the handshake is in progress and this method must be called
        /// again, 0 means the handshake completed successfully, a negative AVERROR means it failed.
        /// </returns>
        public int Handshake()
        {
            return ffmpeg.avio_handshake(_pIOContext);
        }

        public override bool CanRead => _pIOContext->read_packet.Pointer != IntPtr.Zero;

        public override bool CanSeek => _pIOContext->seekable != 0;

        public override bool CanWrite => _pIOContext->write_flag != 0;

        public override long Length => ffmpeg.avio_size(_pIOContext).ThrowIfError();

        public override long Position { get => ffmpeg.avio_tell(_pIOContext).ThrowIfError(); set => Seek(value, SeekOrigin.Begin); }

        public override void Flush()
        {
            ffmpeg.avio_flush(_pIOContext);
            ThrowIfManagedError();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int ret;
            fixed (byte* ptr = buffer)
            {
                ret = ffmpeg.avio_read(_pIOContext, ptr + offset, count);
            }
            if (ret < 0)
            {
                ThrowIfManagedError();
                if (ret == ffmpeg.AVERROR_EOF) return 0;
                ret.ThrowIfError();
            }
            return ret;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            int whence;
            switch (origin)
            {
                case SeekOrigin.Begin: whence = 0; break;
                case SeekOrigin.Current: whence = 1; break;
                case SeekOrigin.End: whence = 2; break;
                default: whence = 0; break;
            }
            var ret = ffmpeg.avio_seek(_pIOContext, offset, whence);
            ThrowIfManagedError();
            return ret.ThrowIfError();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            fixed (byte* ptr = buffer)
            {
                ffmpeg.avio_write(_pIOContext, ptr + offset, count);
            }
            ThrowIfManagedError();
        }

        private void ThrowIfManagedError()
        {
            var err = LastError;
            if (err != null)
            {
                LastError = null;
                throw new IOException("Managed I/O callback threw an exception.", err);
            }
        }

        private bool disposedValue;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (_pIOContext != null)
                {
                    fixed (AVIOContext** pp = &_pIOContext)
                        ffmpeg.avio_closep(pp);
                    _pIOContext = null;
                }
                if (!_leaveStreamOpen)
                {
                    stream?.Dispose();
                }
                stream = null;
                if (_readHandle.IsAllocated) _readHandle.Free();
                if (_writeHandle.IsAllocated) _writeHandle.Free();
                if (_seekHandle.IsAllocated) _seekHandle.Free();
                disposedValue = true;
            }
            base.Dispose(disposing);
        }
    }
}
