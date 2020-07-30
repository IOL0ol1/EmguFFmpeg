using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using FFmpeg.AutoGen;

using System;

namespace EmguFFmpeg.EmguCV
{
    public static partial class EmguFFmpegExtension
    {
        /// <summary>
        /// Convert to Mat
        /// <para>
        /// video frame: convert to AV_PIX_FMT_BGRA and return new Mat(frame.Height, frame.Width, DepthType.Cv8U, 4)
        /// </para>
        /// <para>
        /// audio frame:
        /// <list type="bullet">
        /// <item>if is planar, return new Mat(frame.AVFrame.nb_samples, frame.AVFrame.channels , depthType, 1);</item>
        /// <item>if is packet, return new Mat(frame.AVFrame.nb_samples, 1 , depthType, frame.AVFrame.channels);</item>
        /// </list>
        /// <para><see cref="AVSampleFormat"/> to <see cref="DepthType"/> mapping table</para>
        /// <list type="table" >
        /// <item>
        /// <term><see cref="AVSampleFormat.AV_SAMPLE_FMT_U8"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_U8P"/></term>
        /// <description><see cref="DepthType.Cv8U"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="AVSampleFormat.AV_SAMPLE_FMT_S16"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_S16P"/></term>
        /// <description><see cref="DepthType.Cv16S"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="AVSampleFormat.AV_SAMPLE_FMT_S32"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_S32P"/></term>
        /// <description><see cref="DepthType.Cv32S"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="AVSampleFormat.AV_SAMPLE_FMT_FLT"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_FLTP"/></term>
        /// <description><see cref="DepthType.Cv32F"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="AVSampleFormat.AV_SAMPLE_FMT_DBL"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_DBLP"/></term>
        /// <description><see cref="DepthType.Cv64F"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="AVSampleFormat.AV_SAMPLE_FMT_S64"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_S64P"/></term>
        /// <description><see cref="DepthType.Cv64F"/></description>
        /// </item>
        /// <item>NOTE: Emgucv not supported S64, replace with Cv64F, so read result by bytes convert to int64, otherwise will read <see cref="double.NaN"/>
        /// </item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static Mat ToMat(this MediaFrame frame)
        {
            if (frame.IsVideoFrame)
                return VideoFrameToMat(frame as VideoFrame);
            else if (frame.IsAudioFrame)
                return AudioFrameToMat(frame as AudioFrame);
            throw new FFmpegException(FFmpegException.InvalidFrame);
        }

        private static Mat AudioFrameToMat(AudioFrame frame)
        {
            DepthType dstType;
            switch ((AVSampleFormat)frame.AVFrame.format)
            {
                case AVSampleFormat.AV_SAMPLE_FMT_U8:
                case AVSampleFormat.AV_SAMPLE_FMT_U8P:
                    dstType = DepthType.Cv8U;
                    break;

                case AVSampleFormat.AV_SAMPLE_FMT_S16:
                case AVSampleFormat.AV_SAMPLE_FMT_S16P:
                    dstType = DepthType.Cv16S;
                    break;

                case AVSampleFormat.AV_SAMPLE_FMT_S32:
                case AVSampleFormat.AV_SAMPLE_FMT_S32P:
                    dstType = DepthType.Cv32S;
                    break;

                case AVSampleFormat.AV_SAMPLE_FMT_FLT:
                case AVSampleFormat.AV_SAMPLE_FMT_FLTP:
                    dstType = DepthType.Cv32F;
                    break;

                case AVSampleFormat.AV_SAMPLE_FMT_DBL:
                case AVSampleFormat.AV_SAMPLE_FMT_DBLP:
                // emgucv not have S64, use 64F
                case AVSampleFormat.AV_SAMPLE_FMT_S64:
                case AVSampleFormat.AV_SAMPLE_FMT_S64P:
                    dstType = DepthType.Cv64F;
                    break;

                default:
                    throw new FFmpegException(FFmpegException.NotSupportFormat);
            }

            int planar = ffmpeg.av_sample_fmt_is_planar((AVSampleFormat)frame.AVFrame.format);
            int planes = planar != 0 ? frame.AVFrame.channels : 1;
            int block_align = ffmpeg.av_get_bytes_per_sample((AVSampleFormat)frame.AVFrame.format) * (planar != 0 ? 1 : frame.AVFrame.channels);
            int stride = frame.AVFrame.nb_samples * block_align;

            Mat mat = new Mat(planes, frame.AVFrame.nb_samples, dstType, (planar != 0 ? 1 : frame.AVFrame.channels));
            for (int i = 0; i < planes; i++)
            {
                FFmpegHelper.CopyMemory(mat.DataPointer + i * stride, frame.Data[i], (uint)stride);
            }
            return mat;
        }

        private static Mat VideoFrameToMat(VideoFrame frame)
        {
            if ((AVPixelFormat)frame.AVFrame.format != AVPixelFormat.AV_PIX_FMT_BGRA)
            {
                using (VideoFrame dstFrame = new VideoFrame(AVPixelFormat.AV_PIX_FMT_BGRA, frame.AVFrame.width, frame.AVFrame.height))
                using (PixelConverter converter = new PixelConverter(dstFrame))
                {
                    return BgraToMat(converter.ConvertFrame(frame));
                }
            }
            return BgraToMat(frame);
        }

        private static Mat BgraToMat(MediaFrame frame)
        {
            Mat mat = new Mat(frame.AVFrame.height, frame.AVFrame.width, DepthType.Cv8U, 4);
            int stride = mat.Step;
            for (int i = 0; i < frame.AVFrame.height; i++)
            {
                FFmpegHelper.CopyMemory(mat.DataPointer + i * stride, frame.Data[0] + i * frame.AVFrame.linesize[0], (uint)stride);
            }
            return mat;
        }

        /// <summary>
        /// Convert to video frame to <paramref name="dstFormat"/> after Mat.ToImage&lt;Bgr, byte&gt;"
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="dstFormat">video frame format</param>
        /// <returns></returns>
        public static VideoFrame ToVideoFrame(this Mat mat, AVPixelFormat dstFormat = AVPixelFormat.AV_PIX_FMT_BGR24)
        {
            if (dstFormat != AVPixelFormat.AV_PIX_FMT_BGR24)
            {
                using (PixelConverter converter = new PixelConverter(dstFormat, mat.Width, mat.Height))
                {
                    return converter.ConvertFrame(MatToVideoFrame(mat));
                }
            }
            return MatToVideoFrame(mat);
        }

        /// <summary>
        /// Convert to audio frame to <paramref name="dstFotmat"/>
        /// <para><see cref="DepthType"/> to <see cref="AVSampleFormat"/> mapping table.
        /// if <see cref="Mat.NumberOfChannels"/> > 1, use packet format, otherwise planar</para>
        /// <list type="table" >
        /// <item>
        /// <term><see cref="DepthType.Cv8U"/></term>
        /// <description1><see cref="AVSampleFormat.AV_SAMPLE_FMT_U8"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_U8P"/></description1>
        /// </item>
        /// <item>
        /// <term><see cref="DepthType.Cv16S"/></term>
        /// <description1><see cref="AVSampleFormat.AV_SAMPLE_FMT_S16"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_S16P"/></description1>
        /// </item>
        /// <item>
        /// <term><see cref="DepthType.Cv32S"/></term>
        /// <description1><see cref="AVSampleFormat.AV_SAMPLE_FMT_S32"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_S32P"/></description1>
        /// </item>
        /// <item>
        /// <term><see cref="DepthType.Cv32F"/></term>
        /// <description1><see cref="AVSampleFormat.AV_SAMPLE_FMT_FLT"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_FLTP"/></description1>
        /// </item>
        /// <item>
        /// <term><see cref="DepthType.Cv64F"/></term>
        /// <description1><see cref="AVSampleFormat.AV_SAMPLE_FMT_DBL"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_DBLP"/></description1>
        /// </item>
        /// <item>
        /// <term><see cref="DepthType.Cv64F"/></term>
        /// <description1><see cref="AVSampleFormat.AV_SAMPLE_FMT_S64"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_S64P"/></description1>
        /// </item>
        /// <item>NOTE: Emgucv not supported int64, mapping Cv64F to int64,
        /// so set Mat with int64 if <paramref name="dstFotmat"/> is <see cref="AVSampleFormat.AV_SAMPLE_FMT_S64"/> or <see cref="AVSampleFormat.AV_SAMPLE_FMT_S64P"/>
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="dstFotmat">Default is auto format by <see cref="Mat.Depth"/> and <see cref="Mat.NumberOfChannels"/> use mapping table</param>
        /// <param name="dstSampleRate">Mat not have sample rate, set value here or later</param>
        /// <returns></returns>
        public static AudioFrame ToAudioFrame(this Mat mat, AVSampleFormat dstFotmat = AVSampleFormat.AV_SAMPLE_FMT_NONE, int dstSampleRate = 0)
        {
            AVSampleFormat srcformat;
            switch (mat.Depth)
            {
                case DepthType.Default:
                case DepthType.Cv8U:
                case DepthType.Cv8S:
                    srcformat = mat.NumberOfChannels > 1 ? AVSampleFormat.AV_SAMPLE_FMT_U8 : AVSampleFormat.AV_SAMPLE_FMT_U8P;
                    break;

                case DepthType.Cv16U:
                case DepthType.Cv16S:
                    srcformat = mat.NumberOfChannels > 1 ? AVSampleFormat.AV_SAMPLE_FMT_S16 : AVSampleFormat.AV_SAMPLE_FMT_S16P;
                    break;

                case DepthType.Cv32S:
                    srcformat = mat.NumberOfChannels > 1 ? AVSampleFormat.AV_SAMPLE_FMT_S32 : AVSampleFormat.AV_SAMPLE_FMT_S32P;
                    break;

                case DepthType.Cv32F:
                    srcformat = mat.NumberOfChannels > 1 ? AVSampleFormat.AV_SAMPLE_FMT_FLT : AVSampleFormat.AV_SAMPLE_FMT_FLTP;
                    break;

                case DepthType.Cv64F:
                    srcformat = mat.NumberOfChannels > 1 ? AVSampleFormat.AV_SAMPLE_FMT_DBL : AVSampleFormat.AV_SAMPLE_FMT_DBLP;
                    break;

                default:
                    throw new FFmpegException(FFmpegException.NotSupportFormat);
            }

            if (dstFotmat != AVSampleFormat.AV_SAMPLE_FMT_NONE && dstFotmat != srcformat)
            {
                // converter must need set sample rate
                using (SampleConverter converter = new SampleConverter(dstFotmat, mat.NumberOfChannels > 1 ? mat.NumberOfChannels : mat.Height, mat.Width, Math.Min(1, dstSampleRate)))
                {
                    AudioFrame frame = converter.ConvertFrame(MatToAudioFrame(mat, srcformat, Math.Min(1, dstSampleRate)), out int a, out int b);
                    unsafe
                    {
                        // set real sample rate after convert
                        ((AVFrame*)frame)->sample_rate = dstSampleRate;
                    }
                }
            }

            return MatToAudioFrame(mat, srcformat, dstSampleRate);
        }

        private static AudioFrame MatToAudioFrame(Mat mat, AVSampleFormat srctFormat, int sampleRate)
        {
            int channels = mat.NumberOfChannels > 1 ? mat.NumberOfChannels : mat.Height;
            AudioFrame frame = new AudioFrame(srctFormat, channels, mat.Width, sampleRate);
            bool isPlanar = ffmpeg.av_sample_fmt_is_planar(srctFormat) > 0;
            int stride = mat.Step;
            for (int i = 0; i < (isPlanar ? channels : 1); i++)
            {
                FFmpegHelper.CopyMemory(frame.Data[i], mat.DataPointer + i * stride, (uint)stride);
            }
            return frame;
        }

        private static VideoFrame MatToVideoFrame(Mat mat)
        {
            using (Image<Bgr, byte> image = mat.ToImage<Bgr, byte>())
            {
                VideoFrame frame = new VideoFrame(AVPixelFormat.AV_PIX_FMT_BGR24, image.Width, image.Height);
                int stride = image.Width * image.NumberOfChannels;
                for (int i = 0; i < frame.AVFrame.height; i++)
                {
                    FFmpegHelper.CopyMemory(frame.Data[0] + i * frame.AVFrame.linesize[0], image.Mat.DataPointer + i * stride, (uint)stride);
                }
                return frame;
            }
        }
    }
}