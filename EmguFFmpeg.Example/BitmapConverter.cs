using FFmpeg.AutoGen;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace EmguFFmpeg.Example
{
    /// <summary>
    /// Video frames and bitmaps convert to each other
    /// </summary>
    public class BitmapConverter
    {
        public Bitmap ConvertFrom(VideoFrame frame)
        {
            if ((AVPixelFormat)frame.AVFrame.format != AVPixelFormat.AV_PIX_FMT_BGR24)
                throw new Exception("only support AV_PIX_FMT_BGR24 format");
            int width = frame.Width;
            int height = frame.Height;
            byte[] data = frame.GetData()[0];
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            for (int i = 0; i < height; i++)
            {
                Marshal.Copy(data, 0, bitmapData.Scan0 + i * bitmapData.Stride, data.Length);
            }
            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        public VideoFrame ConvertTo(Bitmap bitmap)
        {
            if (bitmap.PixelFormat != PixelFormat.Format24bppRgb)
                throw new Exception("only support Format24bppRgb format");
            int width = bitmap.Width;
            int height = bitmap.Height;
            VideoFrame frame = new VideoFrame(width, height, AVPixelFormat.AV_PIX_FMT_BGR24);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            // do not use frame.Data[0] = bitmapData.Scan0
            // frame.Linesize[0] may not be equal bitmapData.Stride and both of them may not be equal to width * 3,
            // because of memory alignment
            unsafe
            {
                var bytewidth = Math.Min(bitmapData.Stride, frame.Linesize[0]);
                ffmpeg.av_image_copy_plane((byte*)bitmapData.Scan0, bitmapData.Stride, (byte*)frame.Data[0], frame.Linesize[0], bytewidth, height);
            } 
            bitmap.UnlockBits(bitmapData);
            return frame;
        }
    }
}