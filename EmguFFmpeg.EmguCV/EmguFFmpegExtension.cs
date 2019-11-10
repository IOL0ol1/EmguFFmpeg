using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using EmguFFmpeg;
using FFmpeg.AutoGen;

namespace EmguFFmpeg.EmguCV
{
    public static partial class EmguFFmpegExtension
    {
        /// <summary>
        /// convert frame to mat
        /// <para>
        /// video frame: convert to AV_PIX_FMT_BGR24 and get new Mat(frame.Height, frame.Width, DepthType.Cv8U, 3)
        /// </para>
        /// <para>
        /// audio frame: convert to AV_SAMPLE_FMT_S16P and get new Mat(frame.Channels, frame.NbSamples, DepthType.Cv16S, 1)
        /// </para>
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static Mat ToMat(this MediaFrame frame)
        {
            if (frame.IsVideoFrame)
                return VideoFrameToMat(frame);
            else if (frame.IsAudioFrame)
                return AudioFrameToMat(frame);
            throw new FFmpegException(FFmpegException.InvalidFrame);
        }

        private static Mat AudioFrameToMat(MediaFrame frame)
        {
            frame = (frame as AudioFrame).ToPlanar();
            switch ((AVSampleFormat)frame.AVFrame.format)
            {
                case AVSampleFormat.AV_SAMPLE_FMT_U8:
                case AVSampleFormat.AV_SAMPLE_FMT_U8P:
                    return PlanarToMat<byte>(frame);

                case AVSampleFormat.AV_SAMPLE_FMT_S16:
                case AVSampleFormat.AV_SAMPLE_FMT_S16P:
                    return PlanarToMat<short>(frame);

                case AVSampleFormat.AV_SAMPLE_FMT_S32:
                case AVSampleFormat.AV_SAMPLE_FMT_S32P:
                    return PlanarToMat<int>(frame);

                case AVSampleFormat.AV_SAMPLE_FMT_FLT:
                case AVSampleFormat.AV_SAMPLE_FMT_FLTP:
                    return PlanarToMat<float>(frame);

                case AVSampleFormat.AV_SAMPLE_FMT_DBL:
                case AVSampleFormat.AV_SAMPLE_FMT_DBLP:
                    return PlanarToMat<double>(frame);

                case AVSampleFormat.AV_SAMPLE_FMT_S64:
                case AVSampleFormat.AV_SAMPLE_FMT_S64P:
                    return PlanarToMat<long>(frame);

                default:
                    throw new FFmpegException(FFmpegException.NotSupportFormat);
            }
        }

        private static Mat PlanarToMat<T>(MediaFrame frame) where T : struct
        {
            Image<Gray, T> image = new Image<Gray, T>(frame.AVFrame.nb_samples, frame.AVFrame.channels);
            int stride = image.MIplImage.WidthStep;
            byte[][] planes = frame.GetData();
            for (int i = 0; i < frame.Channels; i++)
            {
                Marshal.Copy(planes[i], 0, IntPtr.Add(image.Mat.DataPointer, i * stride), stride);
            }
            return image.Mat;
        }

        private static Mat VideoFrameToMat(MediaFrame frame)
        {
            if ((AVPixelFormat)frame.AVFrame.format != AVPixelFormat.AV_PIX_FMT_BGR24)
            {
                using (VideoFrame dstFrame = new VideoFrame(AVPixelFormat.AV_PIX_FMT_BGR24, frame.AVFrame.width, frame.AVFrame.height))
                using (PixelConverter converter = new PixelConverter(dstFrame))
                {
                    return Bgr24ToMat(converter.ConvertFrame(frame));
                }
            }
            return Bgr24ToMat(frame);
        }

        private static Mat Bgr24ToMat(MediaFrame frame)
        {
            Mat mat = new Mat(frame.AVFrame.height, frame.AVFrame.width, DepthType.Cv8U, 3);
            int stride = mat.Step;
            byte[] plane = frame.GetData()[0];
            for (int i = 0; i < frame.AVFrame.height; i++)
            {
                Marshal.Copy(plane, i * frame.AVFrame.linesize[0], IntPtr.Add(mat.DataPointer, i * stride), stride);
            }
            return mat;
        }

        /// <summary>
        /// convert to video frame
        /// </summary>
        /// <param name="image"></param>
        /// <param name="dstFormat">video frame format</param>
        /// <returns></returns>
        public static VideoFrame ToVideoFrame(this Mat image, AVPixelFormat dstFormat = AVPixelFormat.AV_PIX_FMT_BGR24)
        {
            if (dstFormat != AVPixelFormat.AV_PIX_FMT_BGR24)
            {
                using (PixelConverter converter = new PixelConverter(dstFormat, image.Width, image.Height))
                {
                    return converter.ConvertFrame(MatToVideoFrame(image));
                }
            }
            return MatToVideoFrame(image);
        }

        public static AudioFrame ToAudioFrame(this Mat image, AVSampleFormat dstFotmat = AVSampleFormat.AV_SAMPLE_FMT_S16P, int dstSampleRate = 0)
        {
            switch (dstFotmat)
            {
                case AVSampleFormat.AV_SAMPLE_FMT_U8:
                    break;

                case AVSampleFormat.AV_SAMPLE_FMT_U8P:
                    break;

                case AVSampleFormat.AV_SAMPLE_FMT_S16:
                    break;

                case AVSampleFormat.AV_SAMPLE_FMT_S16P:
                    break;

                case AVSampleFormat.AV_SAMPLE_FMT_S32:
                    break;

                case AVSampleFormat.AV_SAMPLE_FMT_S32P:
                    break;

                case AVSampleFormat.AV_SAMPLE_FMT_FLT:
                    break;

                case AVSampleFormat.AV_SAMPLE_FMT_FLTP:
                    break;

                case AVSampleFormat.AV_SAMPLE_FMT_DBL:
                    break;

                case AVSampleFormat.AV_SAMPLE_FMT_DBLP:
                    break;

                case AVSampleFormat.AV_SAMPLE_FMT_S64:
                    break;

                case AVSampleFormat.AV_SAMPLE_FMT_S64P:
                    break;

                default:
                    throw new FFmpegException(FFmpegException.NotSupportFormat);
            }

            if (dstFotmat != AVSampleFormat.AV_SAMPLE_FMT_S16P)
            {
                using (SampleConverter converter = new SampleConverter(dstFotmat, image.Height, image.Width, dstSampleRate))

                {
                    return converter.ConvertFrame(MatToAudioFrame(image, dstSampleRate), out int a, out int b);
                }
            }

            return MatToAudioFrame(image, dstSampleRate);
        }

        private static AudioFrame MatToPlanar<T>(Mat mat, int dstSampleRate = 0) where T : new()
        {
            using (Image<Gray, T> image = mat.ToImage<Gray, T>())
            {
                AVSampleFormat format;
                if (typeof(T) == typeof(byte))
                    format = AVSampleFormat.AV_SAMPLE_FMT_U8P;
                else if (typeof(T) == typeof(short))
                    format = AVSampleFormat.AV_SAMPLE_FMT_S16P;
                else if (typeof(T) == typeof(int))
                    format = AVSampleFormat.AV_SAMPLE_FMT_FLTP;
                else if (typeof(T) == typeof(long))
                    format = AVSampleFormat.AV_SAMPLE_FMT_S64P;
                else if (typeof(T) == typeof(float))
                    format = AVSampleFormat.AV_SAMPLE_FMT_FLTP;
                else if (typeof(T) == typeof(double))
                    format = AVSampleFormat.AV_SAMPLE_FMT_DBLP;
                else
                    throw new FFmpegException(FFmpegException.NotSupportFormat);
                AudioFrame frame = new AudioFrame(format, image.Height, image.Width, dstSampleRate);
                int stride = image.Width * image.NumberOfChannels;
                for (int i = 0; i < image.Height; i++)
                {
                    Marshal.Copy(image.Bytes, i * stride, frame.Data[i], stride);
                }
                return frame;
            }
        }

        private static AudioFrame PlanarToPacket(AudioFrame srcframe)
        {
            return null;
        }

        private static AudioFrame MatToAudioFrame(Mat mat, int dstSampleRate)
        {
            using (Image<Gray, short> image = mat.ToImage<Gray, short>())
            {
                AudioFrame frame = new AudioFrame(AVSampleFormat.AV_SAMPLE_FMT_S16P, image.Height, image.Width, dstSampleRate);
                int stride = image.Width * image.NumberOfChannels;
                for (int i = 0; i < image.Height; i++)
                {
                    Marshal.Copy(image.Bytes, i * stride, frame.Data[i], stride);
                }
                return frame;
            }
        }

        private unsafe static VideoFrame MatToVideoFrame(Mat mat)
        {
            using (Image<Bgr, byte> image = mat.ToImage<Bgr, byte>())
            {
                VideoFrame frame = new VideoFrame(AVPixelFormat.AV_PIX_FMT_BGR24, image.Width, image.Height);
                int stride = image.Width * image.NumberOfChannels;
                for (int i = 0; i < frame.AVFrame.height; i++)
                {
                    Marshal.Copy(image.Bytes, i * stride, IntPtr.Add((IntPtr)frame.AVFrame.data[0], frame.AVFrame.linesize[0]), stride);
                }
                return frame;
            }
        }
    }
}