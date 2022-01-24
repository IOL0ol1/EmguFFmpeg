using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    public unsafe partial class MediaIOContext2 : Stream
    {
        protected AVIOContext* pIOContext;
        protected byte[] _buffer;    // Either allocated internally or externally.
        protected readonly int _origin;       // For user-provided arrays, start at this origin
        protected int _position;     // read/write head.
        protected int _length;       // Number of bytes within the memory stream
        // Note that _capacity == _buffer.Length for non-user-provided byte[]'s

        protected bool _writable;    // Can user write to this stream?
        protected readonly bool _exposable;   // Whether the array can be returned to the user.
        protected bool _isOpen;      // Is this stream open or closed?

        protected const int MemStreamMaxLength = int.MaxValue;
        protected const int ArrayMaxLength = 0X7FFFFFC7; // magic number

        protected avio_alloc_context_read_packet avio_Alloc_Context_Read_Packet;
        protected avio_alloc_context_write_packet avio_Alloc_Context_Write_Packet;
        protected avio_alloc_context_seek avio_Alloc_Context_Seek;

        [AllowReversePInvokeCalls]
        protected int WriteFunc(void* opaque, byte* buf, int buf_size)
        {
            return buf_size;
        }

        [AllowReversePInvokeCalls]
        protected int ReadFunc(void* opaque, byte* buf, int buf_size)
        {
            buf_size = Math.Min(buf_size, bufferSize);
            int length = Read(buffer, 0, buf_size);
            Marshal.Copy(buffer, 0, (IntPtr)buf, length);
            return length;
        }

        [AllowReversePInvokeCalls]
        protected long SeekFunc(void* opaque, long offset, int whence)
        {
            if (whence == ffmpeg.AVSEEK_SIZE)
            {
                return Length;
            }
            else if (whence < 3)
            {
                return Seek(offset, (SeekOrigin)whence);
            }
            else
            {
                return -1;
            }
        }


        public static implicit operator AVIOContext*(MediaIOContext value)
        {
            if (value == null) return null;
            return value._pIOContext;
        }

        private void InitIOContext(byte[] buffer, bool writeable, IntPtr opaque)
        {
            fixed (byte* pbuffer = buffer)
            {
                pIOContext = ffmpeg.avio_alloc_context(pbuffer, buffer.Length, writeable ? 1 : 0, (void*)opaque, null, null, null);
            }
        }

        public MediaIOContext2()
            : this(1024)
        { }

        public MediaIOContext2(int bufferSize)
        {
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NegativeCapacity);

            _buffer = bufferSize != 0 ? new byte[bufferSize] : new byte[1024];
            _length = _buffer.Length;
            _writable = true;
            _exposable = true;
            _isOpen = true;
            InitIOContext(_buffer, _writable, IntPtr.Zero);
        }

        public MediaIOContext2(byte[] buffer)
            : this(buffer, true)
        {
        }

        public MediaIOContext2(byte[] buffer, bool writable)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);

            _buffer = buffer;
            _length = buffer.Length;
            _writable = writable;
            _isOpen = true;
            InitIOContext(_buffer, _writable, IntPtr.Zero);
        }

        public MediaIOContext2(byte[] buffer, int index, int count)
            : this(buffer, index, count, true, false)
        {
        }

        public MediaIOContext2(byte[] buffer, int index, int count, bool writable)
            : this(buffer, index, count, writable, false)
        {
        }



        public MediaIOContext2(byte[] buffer, int index, int count, bool writable, bool publiclyVisible)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (buffer.Length - index < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            _buffer = buffer;
            _origin = _position = index;
            _length = index + count;
            _writable = writable;
            _exposable = publiclyVisible;  // Can TryGetBuffer/GetBuffer return the array?
            _isOpen = true;
            InitIOContext(_buffer, _writable, IntPtr.Zero);
        }

        public override bool CanRead => _isOpen;

        public override bool CanSeek => _isOpen;

        public override bool CanWrite => _writable;

        private void EnsureNotClosed()
        {
            if (!_isOpen)
                throw new ObjectDisposedException(SR.ObjectDisposed_StreamClosed);
        }

        private void EnsureWriteable()
        {
            if (!CanWrite)
                throw new NotSupportedException(SR.NotSupported_UnwritableStream);
        }

        /// <summary>
        /// returns a bool saying whether we allocated a new array.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool EnsureCapacity(int value)
        {
            // Check for overflow
            if (value < 0)
                throw new IOException(SR.IO_StreamTooLong);

            return false;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    _isOpen = false;
                    _writable = false;
#if !NET40
                    // Don't set buffer to null - allow TryGetBuffer, GetBuffer & ToArray to work.
                    _lastReadTask = default;
#endif
                }
            }
            finally
            {
                // Call base.Close() to cleanup async IO resources
                base.Dispose(disposing);
            }
        }

        public override void Flush()
        {
        }

        public virtual byte[] GetBuffer()
        {
            if (!_exposable)
                throw new UnauthorizedAccessException(SR.UnauthorizedAccess_MemStreamBuffer);
            return _buffer;
        }

        public virtual bool TryGetBuffer(out ArraySegment<byte> buffer)
        {
            if (!_exposable)
            {
                buffer = default;
                return false;
            }

            buffer = new ArraySegment<byte>(_buffer, offset: _origin, count: _length - _origin);
            return true;
        }

        /// <summary>
        /// PERF: Get actual length of bytes available for read; do sanity checks; shift position - i.e. everything except actual copying bytes
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        internal int InternalEmulateRead(int count)
        {
            EnsureNotClosed();

            int n = _length - _position;
            if (n > count)
                n = count;
            if (n < 0)
                n = 0;

            Debug.Assert(_position + n >= 0, "_position + n >= 0");  // len is less than 2^31 -1.
            _position += n;
            return n;
        }

        public override long Length
        {
            get
            {
                EnsureNotClosed();
                return _length - _origin;
            }
        }

        public override long Position
        {
            get
            {
                EnsureNotClosed();
                return _position - _origin;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_NeedNonNegNum);

                EnsureNotClosed();

                if (value > MemStreamMaxLength)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_StreamLength);
                _position = _origin + (int)value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            EnsureNotClosed();

            int n = _length - _position;
            if (n > count)
                n = count;
            if (n <= 0)
                return 0;

            Debug.Assert(_position + n >= 0, "_position + n >= 0");  // len is less than 2^31 -1.

            if (n <= 8)
            {
                int byteCount = n;
                while (--byteCount >= 0)
                    buffer[offset + byteCount] = _buffer[_position + byteCount];
            }
            else
                Buffer.BlockCopy(_buffer, _position, buffer, offset, n);
            _position += n;

            return n;
        }

        public override int ReadByte()
        {
            EnsureNotClosed();

            if (_position >= _length)
                return -1;

            return _buffer[_position++];
        }

        public override long Seek(long offset, SeekOrigin loc)
        {
            EnsureNotClosed();

            if (offset > MemStreamMaxLength)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_StreamLength);

            switch (loc)
            {
                case SeekOrigin.Begin:
                    {
                        int tempPosition = unchecked(_origin + (int)offset);
                        if (offset < 0 || tempPosition < _origin)
                            throw new IOException(SR.IO_SeekBeforeBegin);
                        _position = tempPosition;
                        break;
                    }
                case SeekOrigin.Current:
                    {
                        int tempPosition = unchecked(_position + (int)offset);
                        if (unchecked(_position + offset) < _origin || tempPosition < _origin)
                            throw new IOException(SR.IO_SeekBeforeBegin);
                        _position = tempPosition;
                        break;
                    }
                case SeekOrigin.End:
                    {
                        int tempPosition = unchecked(_length + (int)offset);
                        if (unchecked(_length + offset) < _origin || tempPosition < _origin)
                            throw new IOException(SR.IO_SeekBeforeBegin);
                        _position = tempPosition;
                        break;
                    }
                default:
                    throw new ArgumentException(SR.Argument_InvalidSeekOrigin);
            }

            Debug.Assert(_position >= 0, "_position >= 0");
            return _position;
        }

        public override void SetLength(long value)
        {
            if (value < 0 || value > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_StreamLength);

            EnsureWriteable();

            // Origin wasn't publicly exposed above.
            Debug.Assert(MemStreamMaxLength == int.MaxValue);  // Check parameter validation logic in this method if this fails.
            if (value > (int.MaxValue - _origin))
                throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_StreamLength);

            int newLength = _origin + (int)value;
            bool allocatedNewArray = EnsureCapacity(newLength);
            if (!allocatedNewArray && newLength > _length)
                Array.Clear(_buffer, _length, newLength - _length);
            _length = newLength;
            if (_position > newLength)
                _position = newLength;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            EnsureNotClosed();
            EnsureWriteable();

            int i = _position + count;
            // Check for overflow
            if (i < 0)
                throw new IOException(SR.IO_StreamTooLong);

            if (i > _length)
            {
                bool mustZero = _position > _length;
                if (i > _capacity)
                {
                    bool allocatedNewArray = EnsureCapacity(i);
                    if (allocatedNewArray)
                    {
                        mustZero = false;
                    }
                }
                if (mustZero)
                {
                    Array.Clear(_buffer, _length, i - _length);
                }
                _length = i;
            }
            if ((count <= 8) && (buffer != _buffer))
            {
                int byteCount = count;
                while (--byteCount >= 0)
                {
                    _buffer[_position + byteCount] = buffer[offset + byteCount];
                }
            }
            else
            {
                Buffer.BlockCopy(buffer, offset, _buffer, _position, count);
            }
            _position = i;
        }

        public override void WriteByte(byte value)
        {
            EnsureNotClosed();
            EnsureWriteable();

            if (_position >= _length)
            {
                int newLength = _position + 1;
                bool mustZero = _position > _length;
                if (newLength >= _capacity)
                {
                    bool allocatedNewArray = EnsureCapacity(newLength);
                    if (allocatedNewArray)
                    {
                        mustZero = false;
                    }
                }
                if (mustZero)
                {
                    Array.Clear(_buffer, _length, _position - _length);
                }
                _length = newLength;
            }
            _buffer[_position++] = value;
        }

        public virtual void WriteTo(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream), SR.ArgumentNull_Stream);

            EnsureNotClosed();

            stream.Write(_buffer, _origin, _length - _origin);
        }

        private void ValidateBufferArguments(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer", SR.ArgumentNull_Buffer);
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (buffer.Length - offset < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }
        }

        private class SR
        {
            internal const string ArgumentOutOfRange_NeedNonNegNum = "NeedNonNegNum";
            internal const string ArgumentOutOfRange_NeedPosNum = "NeedPosNum";
            internal const string ArgumentOutOfRange_NegativeCapacity = "NegativeCapacity";
            internal const string ArgumentOutOfRange_SmallCapacity = "SmallCapacity";
            internal const string ArgumentOutOfRange_StreamLength = "StreamLength";
            internal const string ArgumentNull_Buffer = "Buffer";
            internal const string ArgumentNull_Stream = "Stream";
            internal const string Argument_InvalidOffLen = "InvalidOffLen";
            internal const string Argument_InvalidSeekOrigin = "InvalidSeekOrigin";
            internal const string UnauthorizedAccess_MemStreamBuffer = "MemStreamBuffer";
            internal const string NotSupported_UnreadableStream = "UnreadableStream";
            internal const string NotSupported_UnwritableStream = "UnwritableStream";
            internal const string NotSupported_MemStreamNotExpandable = "MemStreamNotExpandable";
            internal const string IO_StreamTooLong = "StreamTooLong";
            internal const string IO_SeekBeforeBegin = "SeekBeforeBegin";
            internal const string ObjectDisposed_StreamClosed = "StreamClosed";
        }
    }
}
