using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EmguFFmpeg.Example
{
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
                Marshal.Copy(data, 0, IntPtr.Add(bitmapData.Scan0, i * bitmapData.Stride), data.Length);
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
            VideoFrame frame = new VideoFrame(AVPixelFormat.AV_PIX_FMT_BGR24, width, height);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            // do not use frame.Data[0] = bitmapData.Scan0
            // frame.Linesize[0] may not be equal bitmapData.Stride and both of them may not be equal to width * 3,
            // because of memory alignment
            for (int i = 0; i < height; i++)
            {
                byte[] tmp = new byte[bitmapData.Width * 3];
                Marshal.Copy(IntPtr.Add(bitmapData.Scan0, i * bitmapData.Stride), tmp, 0, tmp.Length);
                Marshal.Copy(tmp, 0, IntPtr.Add(frame.Data[0], i * frame.Linesize[0]), tmp.Length);
            }
            bitmap.UnlockBits(bitmapData);
            return frame;
        }
    }
}