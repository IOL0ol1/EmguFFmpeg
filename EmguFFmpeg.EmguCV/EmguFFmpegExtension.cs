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
            if (frame.Width > 0 && frame.Height > 0)
                return VideoFrameToMat(frame);
            else if (frame.NbSamples > 0 && frame.Channels > 0)
                return AudioFrameToMat(frame);
            throw new FFmpegException(FFmpegException.ErrorMessages.InvalidFrame);
        }

        private static Mat AudioFrameToMat(MediaFrame frame)
        {
            switch ((AVSampleFormat)frame.Format)
            {
                case AVSampleFormat.AV_SAMPLE_FMT_U8:
                    return PlanarToMat<byte>(PacketToPlanar(frame, AVSampleFormat.AV_SAMPLE_FMT_U8P));

                case AVSampleFormat.AV_SAMPLE_FMT_U8P:
                    return PlanarToMat<byte>(frame);

                case AVSampleFormat.AV_SAMPLE_FMT_S16:
                    return PlanarToMat<short>(PacketToPlanar(frame, AVSampleFormat.AV_SAMPLE_FMT_S16P));

                case AVSampleFormat.AV_SAMPLE_FMT_S16P:
                    return PlanarToMat<short>(frame);

                case AVSampleFormat.AV_SAMPLE_FMT_S32:
                    return PlanarToMat<int>(PacketToPlanar(frame, AVSampleFormat.AV_SAMPLE_FMT_S32P));

                case AVSampleFormat.AV_SAMPLE_FMT_S32P:
                    return PlanarToMat<int>(frame);

                case AVSampleFormat.AV_SAMPLE_FMT_FLT:
                    return PlanarToMat<float>(PacketToPlanar(frame, AVSampleFormat.AV_SAMPLE_FMT_FLTP));

                case AVSampleFormat.AV_SAMPLE_FMT_FLTP:
                    return PlanarToMat<float>(frame);

                case AVSampleFormat.AV_SAMPLE_FMT_DBL:
                    return PlanarToMat<double>(PacketToPlanar(frame, AVSampleFormat.AV_SAMPLE_FMT_DBLP));

                case AVSampleFormat.AV_SAMPLE_FMT_DBLP:
                    return PlanarToMat<double>(frame);

                case AVSampleFormat.AV_SAMPLE_FMT_S64:
                    return PlanarToMat<long>(PacketToPlanar(frame, AVSampleFormat.AV_SAMPLE_FMT_S64P));

                case AVSampleFormat.AV_SAMPLE_FMT_S64P:
                    return PlanarToMat<long>(frame);

                default:
                    throw new FFmpegException(FFmpegException.ErrorMessages.NotSupportFrame);
            }
        }

        private static MediaFrame PacketToPlanar(MediaFrame frame, AVSampleFormat dstPlanarFormat)
        {
            AudioFrame dstFrame = new AudioFrame(dstPlanarFormat, frame.ChannelLayout, frame.NbSamples, frame.SampleRate);
            using (SampleConverter converter = new SampleConverter(dstFrame))
            {
                return converter.ConvertFrame(frame, out int a, out int b);
            }
        }

        private static Mat PlanarToMat<T>(MediaFrame frame) where T : new()
        {
            Image<Gray, T> image = new Image<Gray, T>(frame.NbSamples, frame.Channels);
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
            if ((AVPixelFormat)frame.Format != AVPixelFormat.AV_PIX_FMT_BGR24)
            {
                using (VideoFrame dstFrame = new VideoFrame(AVPixelFormat.AV_PIX_FMT_BGR24, frame.Width, frame.Height))
                using (PixelConverter converter = new PixelConverter(dstFrame))
                {
                    return Bgr24ToMat(converter.ConvertFrame(frame));
                }
            }
            return Bgr24ToMat(frame);
        }

        private static Mat Bgr24ToMat(MediaFrame frame)
        {
            Mat mat = new Mat(frame.Height, frame.Width, DepthType.Cv8U, 3);
            int stride = mat.Step;
            byte[] plane = frame.GetData()[0];
            for (int i = 0; i < frame.Height; i++)
            {
                Marshal.Copy(plane, i * frame.Linesize[0], IntPtr.Add(mat.DataPointer, i * stride), stride);
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
            if (dstFotmat != AVSampleFormat.AV_SAMPLE_FMT_S16P)
            {
                using (SampleConverter converter = new SampleConverter(dstFotmat, image.Height, image.Width, dstSampleRate))
                {
                    return converter.ConvertFrame(MatToAudioFrame(image, dstSampleRate), out int a, out int b);
                }
            }
            return MatToAudioFrame(image, dstSampleRate);
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

        private static VideoFrame MatToVideoFrame(Mat mat)
        {
            using (Image<Bgr, byte> image = mat.ToImage<Bgr, byte>())
            {
                VideoFrame frame = new VideoFrame(AVPixelFormat.AV_PIX_FMT_BGR24, image.Width, image.Height);
                int stride = image.Width * image.NumberOfChannels;
                for (int i = 0; i < frame.Height; i++)
                {
                    Marshal.Copy(image.Bytes, i * stride, IntPtr.Add(frame.Data[0], frame.Linesize[0]), stride);
                }
                return frame;
            }
        }
    }
}