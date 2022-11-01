using System;
using FFmpeg.AutoGen;

namespace FFmpegSharp
{
    public unsafe static class FFmpegUtil
    {
        /// <summary>
        /// Batch copy <paramref name="src"/> unmanaged memory to <paramref name="dst"/> unmanaged memory.
        /// <para>
        /// Copy "<paramref name="height"/>" number of lines in a row,"<paramref name="byteWidth"/>" bytes each.
        /// The <paramref name="dst"/> address increments by <paramref name="dstByteLineSize"/> bytes per line.
        /// The <paramref name="src"/> address increments by <paramref name="srcByteLineSize"/> bytes per line.
        /// </para>
        /// </summary>
        /// <param name="src">source address.</param>
        /// <param name="srcByteLineSize">linesize for the image plane in src.</param>
        /// <param name="dst">destination address.</param>
        /// <param name="dstByteLineSize">linesize for the image plane in dst.</param>
        /// <param name="byteWidth">the number of bytes copied per line.</param>
        /// <param name="height">the number of rows.</param>
        public static void CopyPlane(IntPtr src, int srcByteLineSize, IntPtr dst, int dstByteLineSize, int byteWidth, int height)
        {
            ffmpeg.av_image_copy_plane((byte*)dst, dstByteLineSize, (byte*)src, srcByteLineSize, byteWidth, height);
        }
    }
}
