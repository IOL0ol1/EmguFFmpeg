using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    public unsafe class MediaIOContext : Stream
    {
        protected AVIOContext* pIOContext;

        public static implicit operator AVIOContext*(MediaIOContext value)
        {
            if (value == null) return null;
            return value.pIOContext;
        }

        protected int bufferSize;
        protected byte[] buffer;
        protected int writeFlag;
        protected void* opaque;
        protected avio_alloc_context_read_packet avio_Alloc_Context_Read_Packet;
        protected avio_alloc_context_write_packet avio_Alloc_Context_Write_Packet;
        protected avio_alloc_context_seek avio_Alloc_Context_Seek;

        [AllowReversePInvokeCalls]
        protected int WriteFunc(void* opaque, byte* buf, int buf_size)
        {
            buf_size = Math.Min(buf_size, bufferSize);
            Marshal.Copy((IntPtr)buf, buffer, 0, buf_size);
            Write(buffer, 0, buf_size);
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


        //public static AVIOContext* GetIOContext(Stream stream, int bufferSize, int writeFlag, void* opaque)
        //{
        //    var o = new MediaIOContext() { stream = stream, bufferSize = bufferSize, buffer = new byte[bufferSize], writeFlag = writeFlag, opaque = opaque };
        //    o.avio_Alloc_Context_Read_Packet = o.ReadFunc;
        //    o.avio_Alloc_Context_Write_Packet = o.WriteFunc;
        //    o.avio_Alloc_Context_Seek = o.SeekFunc;
        //    fixed (byte* buffer = o.buffer)
        //    {
        //        return ffmpeg.avio_alloc_context(buffer, bufferSize, writeFlag, opaque, o.avio_Alloc_Context_Read_Packet, o.avio_Alloc_Context_Write_Packet, o.avio_Alloc_Context_Seek);
        //    }
        //}

        public static Stream CreateStream(AVIOContext* pIOContext, bool isDisposeByOwner = true)
        {
            return new MediaIOContext() { disposedValue = !isDisposeByOwner, pIOContext = pIOContext };
        }

        public MediaIOContext()
        {
            //pIOContext = ffmpeg.avio_alloc_context()
        }

        public override bool CanRead => pIOContext->read_packet.Pointer != IntPtr.Zero;

        public override bool CanSeek => pIOContext->seekable != 0;

        public override bool CanWrite => pIOContext->write_packet.Pointer != IntPtr.Zero;

        public override long Length => ffmpeg.avio_size(pIOContext);

        public override long Position { get => ffmpeg.avio_tell(pIOContext).ThrowIfError(); set => Seek(value, SeekOrigin.Begin); }

        public override void Flush()
        {
            ffmpeg.avio_flush(pIOContext);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            fixed (byte* ptr = buffer)
            {
                return ffmpeg.avio_read(pIOContext, ptr + offset, count).ThrowIfError();
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
            return ffmpeg.avio_seek(pIOContext, offset, whence).ThrowIfError();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            fixed (byte* ptr = buffer)
            {
                ffmpeg.avio_write(pIOContext, ptr + offset, count);
            }
        }

        private bool disposedValue;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                fixed (AVIOContext** ptr = &pIOContext)
                {
                    ffmpeg.avio_context_free(ptr);
                }
                disposedValue = true;
            }
            base.Dispose(disposing);
        }
    }
}
