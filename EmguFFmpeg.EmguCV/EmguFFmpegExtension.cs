using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Emgu.CV;
using Emgu.CV.Structure;
using EmguFFmpeg;
using FFmpeg.AutoGen;

namespace EmguFFmpeg.EmguCV
{
    public static partial class EmguFFmpegExtension
    {
        /// <summary>
        /// convert to bgr24 image
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static Image<Bgr, byte> ToImage(this MediaFrame frame)
        {
            if (frame == null)
                throw new FFmpegException(FFmpegMessage.NullReference);
            if (frame.Height <= 0 || frame.Width <= 0)
                throw new FFmpegException(FFmpegMessage.InvalidVideoFrame);

            if ((AVPixelFormat)frame.Format != AVPixelFormat.AV_PIX_FMT_BGR24)
            {
                using (VideoFrame dstFrame = new VideoFrame(AVPixelFormat.AV_PIX_FMT_BGR24, frame.Width, frame.Height))
                using (PixelConverter converter = new PixelConverter(dstFrame))
                {
                    return ToBgrMat(converter.ConvertFrame(frame));
                }
            }
            return ToBgrMat(frame);
        }

        /// <summary>
        /// convert to video frame
        /// </summary>
        /// <param name="image"></param>
        /// <param name="dstFormat">video frame format</param>
        /// <returns></returns>
        public static VideoFrame ToVideoFrame(this Image<Bgr, byte> image, AVPixelFormat dstFormat = AVPixelFormat.AV_PIX_FMT_BGR24)
        {
            if (dstFormat != AVPixelFormat.AV_PIX_FMT_BGR24)
            {
                using (PixelConverter converter = new PixelConverter(dstFormat, image.Width, image.Height))
                {
                    return converter.ConvertFrame(ToBgrFrame(image));
                }
            }
            return ToBgrFrame(image);


        }

        public static AudioFrame ToAudioFrame(this DenseHistogram histogram)
        {
            // TODO
            return null;
        }


        private static VideoFrame ToBgrFrame(Image<Bgr, byte> image)
        {
            VideoFrame frame = new VideoFrame(AVPixelFormat.AV_PIX_FMT_BGR24, image.Width, image.Height);
            int stride = image.Width * image.NumberOfChannels;
            for (int i = 0; i < frame.Height; i++)
            {
                Marshal.Copy(image.Bytes, i * stride, IntPtr.Add(frame.Data[0], frame.Linesize[0]), stride);
            }
            return frame;
        }

        private static Image<Bgr, byte> ToBgrMat(MediaFrame frame)
        {
            Image<Bgr, byte> image = new Image<Bgr, byte>(frame.Width, frame.Height);
            int stride = image.MIplImage.WidthStep;
            byte[] plane = frame.GetData()[0];
            for (int i = 0; i < frame.Height; i++)
            {
                Marshal.Copy(plane, i * frame.Linesize[0], IntPtr.Add(image.Mat.DataPointer, i * stride), stride);
            }
            return image;
        }
    }

    internal static class FFmpegMessage
    {
        public const string NullReference = "null reference";
        public const string InvalidVideoFrame = "invalid video frame";
    }
}