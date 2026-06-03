using System;

using FFmpeg.AutoGen;

using OpenCvSharp;

namespace FFmpegSharp.OpenCvSharp4
{
    /// <summary>
    /// Conversion helpers between a raw FFmpeg <see cref="AVFrame"/> and an OpenCvSharp <see cref="Mat"/>.
    /// Supported packed pixel formats: <see cref="AVPixelFormat.AV_PIX_FMT_GRAY8"/> (CV_8UC1),
    /// <see cref="AVPixelFormat.AV_PIX_FMT_BGR24"/> (CV_8UC3) and <see cref="AVPixelFormat.AV_PIX_FMT_BGRA"/> (CV_8UC4).
    /// Any other pixel format is converted through <c>libswscale</c> to BGR24.
    /// </summary>
    public unsafe static class Extensions
    {
        /// <summary>
        /// Convert an OpenCvSharp <see cref="Mat"/> to a packed video <see cref="AVFrame"/>.
        /// The returned frame owns ref-counted buffers; the caller must release them with
        /// <see cref="ffmpeg.av_frame_unref(AVFrame*)"/> when finished.
        /// </summary>
        /// <param name="mat">8-bit, 1/3/4 channel image (CV_8UC1 / CV_8UC3 / CV_8UC4).</param>
        public static AVFrame ToFrame(this Mat mat)
        {
            if (mat == null) throw new ArgumentNullException(nameof(mat));
            if (mat.Empty()) throw new ArgumentException("Mat is empty.", nameof(mat));
            if (mat.Depth() != MatType.CV_8U)
                throw new NotSupportedException($"Only 8-bit (CV_8U) Mat is supported, got depth {mat.Depth()}.");

            AVPixelFormat srcFormat;
            switch (mat.Channels())
            {
                case 1: srcFormat = AVPixelFormat.AV_PIX_FMT_GRAY8; break;
                case 3: srcFormat = AVPixelFormat.AV_PIX_FMT_BGR24; break;
                case 4: srcFormat = AVPixelFormat.AV_PIX_FMT_BGRA; break;
                default: throw new NotSupportedException($"Unsupported channel count: {mat.Channels()}.");
            }

            var frame = new AVFrame
            {
                format = (int)srcFormat,
                width = mat.Width,
                height = mat.Height,
            };
            ThrowIfError(ffmpeg.av_frame_get_buffer(&frame, 0), nameof(ffmpeg.av_frame_get_buffer));

            var srcLineSize = (int)mat.Step();
            var dstLineSize = frame.linesize[0];
            ffmpeg.av_image_copy_plane(
                frame.data[0], dstLineSize,
                (byte*)mat.Data, srcLineSize,
                Math.Min(srcLineSize, dstLineSize), mat.Height);
            return frame;
        }

        /// <summary>
        /// Convert a video <see cref="AVFrame"/> to an OpenCvSharp <see cref="Mat"/>.
        /// GRAY8/BGR24/BGRA frames are copied directly; any other pixel format is first converted to BGR24.
        /// The caller owns the returned <see cref="Mat"/> and must dispose it.
        /// </summary>
        public static Mat ToMat(this AVFrame frame)
        {
            var format = (AVPixelFormat)frame.format;
            int width = frame.width;
            int height = frame.height;
            if (width <= 0 || height <= 0)
                throw new ArgumentException("AVFrame is not a valid video frame (width/height must be positive).", nameof(frame));

            MatType matType;
            switch (format)
            {
                case AVPixelFormat.AV_PIX_FMT_GRAY8: matType = MatType.CV_8UC1; break;
                case AVPixelFormat.AV_PIX_FMT_BGR24: matType = MatType.CV_8UC3; break;
                case AVPixelFormat.AV_PIX_FMT_BGRA: matType = MatType.CV_8UC4; break;
                default:
                    return ConvertToBgr24Mat(frame, width, height, format);
            }

            var mat = new Mat(height, width, matType);
            var srcLineSize = frame.linesize[0];
            var dstLineSize = (int)mat.Step();
            ffmpeg.av_image_copy_plane(
                (byte*)mat.Data, dstLineSize,
                frame.data[0], srcLineSize,
                Math.Min(srcLineSize, dstLineSize), height);
            return mat;
        }

        private static Mat ConvertToBgr24Mat(AVFrame src, int width, int height, AVPixelFormat srcFormat)
        {
            var sws = ffmpeg.sws_getContext(
                width, height, srcFormat,
                width, height, AVPixelFormat.AV_PIX_FMT_BGR24,
                (int)SwsFlags.SWS_BILINEAR, null, null, null);
            if (sws == null)
                throw new ApplicationException($"sws_getContext failed for {srcFormat} -> BGR24 ({width}x{height}).");

            var dst = new AVFrame
            {
                format = (int)AVPixelFormat.AV_PIX_FMT_BGR24,
                width = width,
                height = height,
            };
            try
            {
                ThrowIfError(ffmpeg.av_frame_get_buffer(&dst, 0), nameof(ffmpeg.av_frame_get_buffer));
                ThrowIfError(ffmpeg.sws_scale(sws,
                    src.data, src.linesize, 0, height,
                    dst.data, dst.linesize), nameof(ffmpeg.sws_scale));
                return dst.ToMat();
            }
            finally
            {
                ffmpeg.av_frame_unref(&dst);
                ffmpeg.sws_freeContext(sws);
            }
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
