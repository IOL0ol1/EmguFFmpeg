using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    public unsafe static class FFmpegUtil
    {
        /// <summary>
        /// Batch copy a 2-D image plane from unmanaged source to unmanaged destination.
        /// </summary>
        public static void CopyPlane(IntPtr src, int srcByteLineSize, IntPtr dst, int dstByteLineSize, int byteWidth, int height)
        {
            ffmpeg.av_image_copy_plane((byte*)dst, dstByteLineSize, (byte*)src, srcByteLineSize, byteWidth, height);
        }

        /// <summary>
        /// Get the buffer size required by <see cref="ffmpeg.av_image_copy_to_buffer"/> for the given format and dimensions.
        /// </summary>
        public static int GetImageBufferSize(AVPixelFormat pixelFormat, int width, int height, int align = 1)
            => ffmpeg.av_image_get_buffer_size(pixelFormat, width, height, align).ThrowIfError();

        /// <summary>
        /// Copy an image to <paramref name="dst"/> in packed layout, matching <see cref="GetImageBufferSize(AVPixelFormat, int, int, int)"/>.
        /// </summary>
        public static int CopyImageToBuffer(Span<byte> dst, MediaFrame frame, int align = 1)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            AVFrame* f = frame;
            var srcData = new byte_ptrArray4();
            srcData.UpdateFrom(f->data);
            var srcLine = new int_array4();
            srcLine.UpdateFrom(f->linesize);
            fixed (byte* p = dst)
            {
                return ffmpeg.av_image_copy_to_buffer(p, dst.Length, srcData, srcLine,
                    (AVPixelFormat)f->format, f->width, f->height, align).ThrowIfError();
            }
        }

        /// <summary>
        /// Allocate and populate <paramref name="frame"/>'s data/linesize arrays from <paramref name="src"/> using
        /// the frame's existing width/height/format.
        /// </summary>
        public static int FillFrameFromBuffer(MediaFrame frame, ReadOnlySpan<byte> src, int align = 1)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            AVFrame* f = frame;
            var data4 = new byte_ptrArray4();
            var line4 = new int_array4();
            int ret;
            fixed (byte* p = src)
            {
                ret = ffmpeg.av_image_fill_arrays(
                    ref data4, ref line4,
                    p, (AVPixelFormat)f->format, f->width, f->height, align).ThrowIfError();
            }
            // Mirror the 4-entry results back into the 8-entry AVFrame arrays (planes 4..7 are zeroed by AVFrame).
            for (uint i = 0; i < 4; i++)
            {
                f->data[i] = data4[i];
                f->linesize[i] = line4[i];
            }
            return ret;
        }
    }
}
