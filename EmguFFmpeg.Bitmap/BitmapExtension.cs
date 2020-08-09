using FFmpeg.AutoGen;

using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace EmguFFmpeg
{

    /// <summary>
    /// TODO: test
    /// </summary>
    public static class BitmapExtension
    {
        private static Bitmap BgraToMat(VideoFrame frame)
        {
            int width = frame.Width;
            int height = frame.Height;
            Bitmap bitmap = new Bitmap(width, height, (AVPixelFormat)frame.AVFrame.format == AVPixelFormat.AV_PIX_FMT_BGRA ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            var bytewidth = Math.Min(bitmapData.Stride, frame.Linesize[0]);
            FFmpegHelper.CopyPlane(frame.Data[0], frame.Linesize[0], bitmapData.Scan0, bitmapData.Stride, bytewidth, height);
            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        public static Bitmap ToBitmap(this VideoFrame frame)
        {
            if ((AVPixelFormat)frame.AVFrame.format == AVPixelFormat.AV_PIX_FMT_BGRA || (AVPixelFormat)frame.AVFrame.format == AVPixelFormat.AV_PIX_FMT_BGR24)
                return BgraToMat(frame);
            using (VideoFrame dstFrame = new VideoFrame(frame.AVFrame.width, frame.AVFrame.height, AVPixelFormat.AV_PIX_FMT_BGRA))
            using (PixelConverter converter = new PixelConverter(dstFrame))
            {
                return BgraToMat(converter.ConvertFrame(frame));
            }
        }

        public static VideoFrame ToVideoFrame(this Bitmap bitmap)
        {
            if (bitmap.PixelFormat != PixelFormat.Format24bppRgb && bitmap.PixelFormat != PixelFormat.Format32bppArgb)
                throw new FFmpegException(FFmpegException.NotSupportFormat);
            int width = bitmap.Width;
            int height = bitmap.Height;
            VideoFrame frame = new VideoFrame(width, height, bitmap.PixelFormat == PixelFormat.Format24bppRgb ? AVPixelFormat.AV_PIX_FMT_BGR24 : AVPixelFormat.AV_PIX_FMT_BGRA);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            // do not use frame.Data[0] = bitmapData.Scan0
            // frame.Linesize[0] may not be equal bitmapData.Stride and both of them may not be equal to width * 3 (BGR24),
            // because of memory alignment.
            var bytewidth = Math.Min(bitmapData.Stride, frame.Linesize[0]);
            FFmpegHelper.CopyPlane(bitmapData.Scan0, bitmapData.Stride, frame.Data[0], frame.Linesize[0], bytewidth, height);
            bitmap.UnlockBits(bitmapData);
            return frame;
        }
    }
}
