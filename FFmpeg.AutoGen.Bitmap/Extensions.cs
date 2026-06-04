using System;
using System.Drawing;
using System.Drawing.Imaging;

using FFmpeg.AutoGen;

namespace FFmpeg.Bitmap
{
    /// <summary>
    /// Conversion helpers between a raw FFmpeg <see cref="AVFrame"/> and a GDI+ <see cref="System.Drawing.Bitmap"/>.
    /// Supported pixel formats for direct copy: BGR24 (<see cref="PixelFormat.Format24bppRgb"/>)
    /// and BGRA (<see cref="PixelFormat.Format32bppArgb"/>).
    /// Any other video pixel format is converted to BGRA via <c>libswscale</c>.
    /// </summary>
    public static unsafe class Extensions
    {
        /// <summary>
        /// Convert a video <see cref="AVFrame"/> to a <see cref="System.Drawing.Bitmap"/>.
        /// BGR24/BGRA frames are copied directly; all other formats are converted to BGRA first.
        /// The caller owns the returned bitmap and must dispose it.
        /// </summary>
        public static System.Drawing.Bitmap ToBitmap(this AVFrame frame)
        {
            int width = frame.width;
            int height = frame.height;
            if (width <= 0 || height <= 0)
                throw new ArgumentException("AVFrame is not a valid video frame (width/height must be positive).", nameof(frame));

            var fmt = (AVPixelFormat)frame.format;

            if (fmt == AVPixelFormat.AV_PIX_FMT_BGR24 || fmt == AVPixelFormat.AV_PIX_FMT_BGRA)
                return CopyToBitmap(frame.data[0], frame.linesize[0], width, height,
                    fmt == AVPixelFormat.AV_PIX_FMT_BGRA ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb);

            // Convert to BGRA via swscale then recurse.
            var dst = new AVFrame
            {
                format = (int)AVPixelFormat.AV_PIX_FMT_BGRA,
                width = width,
                height = height,
            };
            var sws = ffmpeg.sws_getContext(
                width, height, fmt,
                width, height, AVPixelFormat.AV_PIX_FMT_BGRA,
                ffmpeg.SWS_BILINEAR, null, null, null);
            if (sws == null)
                throw new ApplicationException($"sws_getContext failed for {fmt} -> BGRA ({width}x{height}).");
            try
            {
                ThrowIfError(ffmpeg.av_frame_get_buffer(&dst, 0), nameof(ffmpeg.av_frame_get_buffer));
                ThrowIfError(ffmpeg.sws_scale(sws, frame.data, frame.linesize, 0, height, dst.data, dst.linesize),
                    nameof(ffmpeg.sws_scale));
                return dst.ToBitmap();
            }
            finally
            {
                ffmpeg.av_frame_unref(&dst);
                ffmpeg.sws_freeContext(sws);
            }
        }

        /// <summary>
        /// Convert a <see cref="System.Drawing.Bitmap"/> to a packed video <see cref="AVFrame"/>.
        /// Only <see cref="PixelFormat.Format24bppRgb"/> (→ BGR24) and
        /// <see cref="PixelFormat.Format32bppArgb"/> (→ BGRA) are supported.
        /// The returned frame owns ref-counted buffers; release with
        /// <see cref="ffmpeg.av_frame_unref(AVFrame*)"/> when finished.
        /// </summary>
        public static AVFrame ToFrame(this System.Drawing.Bitmap bitmap)
        {
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));

            AVPixelFormat pixFmt;
            PixelFormat lockFmt;
            switch (bitmap.PixelFormat)
            {
                case PixelFormat.Format24bppRgb:
                    pixFmt = AVPixelFormat.AV_PIX_FMT_BGR24;
                    lockFmt = PixelFormat.Format24bppRgb;
                    break;
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                    pixFmt = AVPixelFormat.AV_PIX_FMT_BGRA;
                    lockFmt = PixelFormat.Format32bppArgb;
                    break;
                default:
                    throw new NotSupportedException(
                        $"Unsupported PixelFormat {bitmap.PixelFormat}. Use Format24bppRgb or Format32bppArgb.");
            }

            int width = bitmap.Width;
            int height = bitmap.Height;

            var frame = new AVFrame
            {
                format = (int)pixFmt,
                width = width,
                height = height,
            };
            ThrowIfError(ffmpeg.av_frame_get_buffer(&frame, 0), nameof(ffmpeg.av_frame_get_buffer));

            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, lockFmt);
            try
            {
                ffmpeg.av_image_copy_plane(
                    frame.data[0], frame.linesize[0],
                    (byte*)bitmapData.Scan0, bitmapData.Stride,
                    Math.Min(Math.Abs(bitmapData.Stride), frame.linesize[0]),
                    height);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            return frame;
        }

        private static System.Drawing.Bitmap CopyToBitmap(byte* srcData, int srcStride, int width, int height, PixelFormat pixelFormat)
        {
            var bitmap = new System.Drawing.Bitmap(width, height, pixelFormat);
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, pixelFormat);
            try
            {
                ffmpeg.av_image_copy_plane(
                    (byte*)bitmapData.Scan0, bitmapData.Stride,
                    srcData, srcStride,
                    Math.Min(srcStride, Math.Abs(bitmapData.Stride)),
                    height);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
            return bitmap;
        }

        private static int ThrowIfError(int error, string func)
        {
            if (error < 0)
            {
                var buffer = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE];
                ffmpeg.av_strerror(error, buffer, (ulong)ffmpeg.AV_ERROR_MAX_STRING_SIZE);
                var message = new string((sbyte*)buffer);
                throw new ApplicationException($"{func} failed ({error}): {message}");
            }
            return error;
        }
    }
}

