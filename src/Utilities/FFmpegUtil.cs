using System;
using FFmpeg.AutoGen;

namespace FFmpegSharp
{
    public unsafe static class FFmpegUtil
    {
        /// <summary>
        /// Copy <paramref name="src"/> unmanaged memory to <paramref name="dst"/> unmanaged memory.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="count"></param>
        public static void CopyMemory(IntPtr src, IntPtr dst, int count)
        {
            CopyMemory((byte*)src, (byte*)dst, count);
        }

        /// <summary>
        /// [Unsafe] Copy <paramref name="src"/> unmanaged memory to <paramref name="dst"/> unmanaged memory.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="count"></param>
        public static void CopyMemory(void* src, void* dst, int count)
        {
            ffmpeg.av_image_copy_plane((byte*)dst, count, (byte*)src, count, count, 1);
        }

        /// <summary>
        /// Batch copy <paramref name="src"/> unmanaged memory to <paramref name="dst"/> unmanaged memory.
        /// <para>
        /// Copy "<paramref name="height"/>" number of lines in a row,"<paramref name="byteWidth"/>" bytes each.
        /// The <paramref name="dst"/> address increments by <paramref name="dstLineSize"/> bytes per line.
        /// The <paramref name="src"/> address increments by <paramref name="srcLineSize"/> bytes per line.
        /// </para>
        /// </summary>
        /// <param name="src">source address.</param>
        /// <param name="srcLineSize">linesize for the image plane in src.</param>
        /// <param name="dst">destination address.</param>
        /// <param name="dstLineSize">linesize for the image plane in dst.</param>
        /// <param name="byteWidth">the number of bytes copied per line.</param>
        /// <param name="height">the number of rows.</param>
        public static void CopyPlane(IntPtr src, int srcLineSize, IntPtr dst, int dstLineSize, int byteWidth, int height)
        {
            ffmpeg.av_image_copy_plane((byte*)dst, dstLineSize, (byte*)src, srcLineSize, byteWidth, height);
        }


    }
}
