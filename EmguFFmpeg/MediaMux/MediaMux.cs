using FFmpeg.AutoGen;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace EmguFFmpeg
{
    /// <summary>
    /// <see cref="AVFormatContext"/> wapper
    /// </summary>
    public abstract class MediaMux : IDisposable, IReadOnlyList<MediaStream>
    {
        protected unsafe AVFormatContext* pFormatContext;

        public AVFormatContext AVFormatContext { get { unsafe { return *pFormatContext; } } }

        public string Url { get { unsafe { return ((IntPtr)pFormatContext->url).PtrToStringUTF8(); } } }

        public MediaFormat Format { get; protected set; }

        public unsafe static implicit operator AVFormatContext*(MediaMux value)
        {
            if (value == null) return null;
            return value.pFormatContext;
        }

        #region Stream Support

        protected Stream baseStream;
        protected const int bufferLength = 4096;
        protected readonly byte[] buffer = new byte[bufferLength];
        protected avio_alloc_context_read_packet avio_Alloc_Context_Read_Packet;
        protected avio_alloc_context_write_packet avio_Alloc_Context_Write_Packet;
        protected avio_alloc_context_seek avio_Alloc_Context_Seek;

        [AllowReversePInvokeCalls]
        protected unsafe int WriteFunc(void* opaque, byte* buf, int buf_size)
        {
            buf_size = Math.Min(buf_size, bufferLength);
            Marshal.Copy((IntPtr)buf, buffer, 0, buf_size);
            baseStream.Write(buffer, 0, buf_size);
            return buf_size;
        }

        [AllowReversePInvokeCalls]
        protected unsafe int ReadFunc(void* opaque, byte* buf, int buf_size)
        {
            buf_size = Math.Min(buf_size, bufferLength);
            int length = baseStream.Read(buffer, 0, buf_size);
            Marshal.Copy(buffer, 0, (IntPtr)buf, length);
            return length;
        }

        [AllowReversePInvokeCalls]
        protected unsafe long SeekFunc(void* opaque, long offset, int whence)
        {
            if (whence == ffmpeg.AVSEEK_SIZE)
            {
                return baseStream.Length;
            }
            else if (whence < 3)
            {
                return baseStream.Seek(offset, (SeekOrigin)whence);
            }
            else
            {
                return -1;
            }
        }

        #endregion

        public abstract void DumpInfo();

        #region IReadOnlyList<MediaStream>

        protected List<MediaStream> streams = new List<MediaStream>();

        public int Count => streams.Count;

        public MediaStream this[int index] => streams[index];

        public IEnumerator<MediaStream> GetEnumerator()
        {
            return streams.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IDisposable Support

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MediaMux()
        {
            Dispose(false);
        }

        protected abstract void Dispose(bool disposing);

        #endregion
    }
}