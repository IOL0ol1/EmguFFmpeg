using FFmpeg.AutoGen;

using OpenCvSharp;

using System;

namespace EmguFFmpeg
{
    /// <summary>
    /// OpenCVSharp Extension
    /// </summary>
    public static partial class OpenCvSharpExtension
    {
        /// <summary>
        /// Convert to Mat
        /// <para>
        /// video frame: convert to AV_PIX_FMT_BGRA and return new Mat(frame.Height, frame.Width, MatType.CV_8U4)
        /// </para>
        /// <para>
        /// audio frame:
        /// <list type="bullet">
        /// <item>if is planar, return new Mat(frame.AVFrame.nb_samples, frame.AVFrame.channels , MatType.MakeType(depth, 1));</item>
        /// <item>if is packet, return new Mat(frame.AVFrame.nb_samples, 1 , MatType.MakeType(depth, frame.AVFrame.channels));</item>
        /// </list>
        /// <para><see cref="AVSampleFormat"/> to <see cref="MatType.Depth"/> mapping table</para>
        /// <list type="table" >
        /// <item>
        /// <term><see cref="AVSampleFormat.AV_SAMPLE_FMT_U8"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_U8P"/></term>
        /// <description><see cref="MatType.CV_8U"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="AVSampleFormat.AV_SAMPLE_FMT_S16"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_S16P"/></term>
        /// <description><see cref="MatType.CV_16S"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="AVSampleFormat.AV_SAMPLE_FMT_S32"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_S32P"/></term>
        /// <description><see cref="MatType.CV_32S"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="AVSampleFormat.AV_SAMPLE_FMT_FLT"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_FLTP"/></term>
        /// <description><see cref="MatType.CV_32F"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="AVSampleFormat.AV_SAMPLE_FMT_DBL"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_DBLP"/></term>
        /// <description><see cref="MatType.CV_64F"/></description>
        /// </item>
        /// <item>
        /// <term><see cref="AVSampleFormat.AV_SAMPLE_FMT_S64"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_S64P"/></term>
        /// <description><see cref="MatType.CV_64F"/></description>
        /// </item>
        /// <item>NOTE: OpenCV not supported 64S, replace with CV_64F, so read result by bytes convert to int64, otherwise will read <see cref="double.NaN"/>
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

            int planar = ffmpeg.av_sample_fmt_is_planar((AVSampleFormat)frame.AVFrame.format);
            int planes = planar != 0 ? frame.AVFrame.channels : 1;
            int block_align = ffmpeg.av_get_bytes_per_sample((AVSampleFormat)frame.AVFrame.format) * (planar != 0 ? 1 : frame.AVFrame.channels);
            int stride = frame.AVFrame.nb_samples * block_align;
            int channels = planar != 0 ? 1 : frame.AVFrame.channels;

            MatType dstType;
            switch ((AVSampleFormat)frame.AVFrame.format)
            {
                case AVSampleFormat.AV_SAMPLE_FMT_U8:
                case AVSampleFormat.AV_SAMPLE_FMT_U8P:
                    dstType = MatType.CV_8UC(channels);
                    break;

                case AVSampleFormat.AV_SAMPLE_FMT_S16:
                case AVSampleFormat.AV_SAMPLE_FMT_S16P:
                    dstType = MatType.CV_16SC(channels);
                    break;

                case AVSampleFormat.AV_SAMPLE_FMT_S32:
                case AVSampleFormat.AV_SAMPLE_FMT_S32P:
                    dstType = MatType.CV_32SC(channels);
                    break;

                case AVSampleFormat.AV_SAMPLE_FMT_FLT:
                case AVSampleFormat.AV_SAMPLE_FMT_FLTP:
                    dstType = MatType.CV_32FC(channels);
                    break;

                case AVSampleFormat.AV_SAMPLE_FMT_DBL:
                case AVSampleFormat.AV_SAMPLE_FMT_DBLP:
                // opencv not have 64S, use 64F
                case AVSampleFormat.AV_SAMPLE_FMT_S64:
                case AVSampleFormat.AV_SAMPLE_FMT_S64P:
                    dstType = MatType.CV_64FC(channels);
                    break;

                default:
                    throw new FFmpegException(FFmpegException.NotSupportFormat);
            }

            Mat mat = new Mat(planes, frame.AVFrame.nb_samples, dstType);
            for (int i = 0; i < planes; i++)
            {
                FFmpegHelper.CopyMemory(frame.Data[i], mat.Data + i * stride, stride);
            }
            return mat;
        }

        private static Mat VideoFrameToMat(VideoFrame frame)
        {
            if ((AVPixelFormat)frame.AVFrame.format != AVPixelFormat.AV_PIX_FMT_BGRA)
            {
                using (VideoFrame dstFrame = new VideoFrame(frame.AVFrame.width, frame.AVFrame.height, AVPixelFormat.AV_PIX_FMT_BGRA))
                using (PixelConverter converter = new PixelConverter(dstFrame))
                {
                    return BgraToMat(converter.ConvertFrame(frame));
                }
            }
            return BgraToMat(frame);
        }

        private static Mat BgraToMat(MediaFrame frame)
        {
            Mat mat = new Mat(frame.AVFrame.height, frame.AVFrame.width, MatType.CV_8UC4);
            int stride = (int)(uint)mat.Step();
            unsafe
            {
                var bytewidth = Math.Min(stride, frame.AVFrame.linesize[0]);
                ffmpeg.av_image_copy_plane(mat.DataPointer, stride, (byte*)frame.Data[0], frame.AVFrame.linesize[0], bytewidth, frame.AVFrame.height);
            }
            return mat;
        }

        /// <summary>
        /// Convert video frame to <paramref name="dstFormat"/> with Bgr24 mat
        /// <para>
        /// NOTE: only support CV_8U3 Mat!!
        /// </para>
        /// </summary>
        /// <param name="mat">must bge format</param>
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
        /// <para><see cref="MatType"/> to <see cref="AVSampleFormat"/> mapping table.
        /// if <see cref="Mat.Channels()"/> > 1, use packet format, otherwise planar</para>
        /// <list type="table" >
        /// <item>
        /// <term><see cref="MatType.CV_8U"/></term>
        /// <description1><see cref="AVSampleFormat.AV_SAMPLE_FMT_U8"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_U8P"/></description1>
        /// </item>
        /// <item>
        /// <term><see cref="MatType.CV_16S"/></term>
        /// <description1><see cref="AVSampleFormat.AV_SAMPLE_FMT_S16"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_S16P"/></description1>
        /// </item>
        /// <item>
        /// <term><see cref="MatType.CV_32S"/></term>
        /// <description1><see cref="AVSampleFormat.AV_SAMPLE_FMT_S32"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_S32P"/></description1>
        /// </item>
        /// <item>
        /// <term><see cref="MatType.CV_32F"/></term>
        /// <description1><see cref="AVSampleFormat.AV_SAMPLE_FMT_FLT"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_FLTP"/></description1>
        /// </item>
        /// <item>
        /// <term><see cref="MatType.CV_64F"/></term>
        /// <description1><see cref="AVSampleFormat.AV_SAMPLE_FMT_DBL"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_DBLP"/></description1>
        /// </item>
        /// <item>
        /// <term><see cref="MatType.CV_64F"/></term>
        /// <description1><see cref="AVSampleFormat.AV_SAMPLE_FMT_S64"/>/<see cref="AVSampleFormat.AV_SAMPLE_FMT_S64P"/></description1>
        /// </item>
        /// <item>NOTE: Emgucv not supported int64, mapping Cv64F to int64,
        /// so set Mat with int64 if <paramref name="dstFotmat"/> is <see cref="AVSampleFormat.AV_SAMPLE_FMT_S64"/> or <see cref="AVSampleFormat.AV_SAMPLE_FMT_S64P"/>
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="dstFotmat">Default is auto format by <see cref="Mat.Depth"/> and <see cref="Mat.Channels()"/> use mapping table</param>
        /// <param name="dstSampleRate">Mat not have sample rate, set value here or later</param>
        /// <returns></returns>
        public unsafe static AudioFrame ToAudioFrame(this Mat mat, AVSampleFormat dstFotmat = AVSampleFormat.AV_SAMPLE_FMT_NONE, int dstSampleRate = 0)
        {
            AVSampleFormat srcformat;
            switch (mat.Depth())
            {
                case MatType.CV_8U:
                case MatType.CV_8S:
                    srcformat = mat.Channels() > 1 ? AVSampleFormat.AV_SAMPLE_FMT_U8 : AVSampleFormat.AV_SAMPLE_FMT_U8P;
                    break;

                case MatType.CV_16U:
                case MatType.CV_16S:
                    srcformat = mat.Channels() > 1 ? AVSampleFormat.AV_SAMPLE_FMT_S16 : AVSampleFormat.AV_SAMPLE_FMT_S16P;
                    break;

                case MatType.CV_32S:
                    srcformat = mat.Channels() > 1 ? AVSampleFormat.AV_SAMPLE_FMT_S32 : AVSampleFormat.AV_SAMPLE_FMT_S32P;
                    break;

                case MatType.CV_32F:
                    srcformat = mat.Channels() > 1 ? AVSampleFormat.AV_SAMPLE_FMT_FLT : AVSampleFormat.AV_SAMPLE_FMT_FLTP;
                    break;

                case MatType.CV_64F:
                    srcformat = mat.Channels() > 1 ? AVSampleFormat.AV_SAMPLE_FMT_DBL : AVSampleFormat.AV_SAMPLE_FMT_DBLP;
                    break;

                default:
                    throw new FFmpegException(FFmpegException.NotSupportFormat);
            }

            if (dstFotmat != AVSampleFormat.AV_SAMPLE_FMT_NONE && dstFotmat != srcformat)
            {
                // converter must need set sample rate
                using (SampleConverter converter = new SampleConverter(dstFotmat, mat.Channels() > 1 ? mat.Channels() : mat.Height, mat.Width, Math.Min(1, dstSampleRate)))
                {
                    AudioFrame frame = converter.ConvertFrame(MatToAudioFrame(mat, srcformat, Math.Min(1, dstSampleRate)), out int a, out int b);
                    // set real sample rate after convert
                    ((AVFrame*)frame)->sample_rate = dstSampleRate;
                }
            }

            return MatToAudioFrame(mat, srcformat, dstSampleRate);
        }

        private static AudioFrame MatToAudioFrame(Mat mat, AVSampleFormat srctFormat, int sampleRate)
        {
            int channels = mat.Channels() > 1 ? mat.Channels() : mat.Height;
            AudioFrame frame = new AudioFrame(channels, mat.Width, srctFormat, sampleRate);
            bool isPlanar = ffmpeg.av_sample_fmt_is_planar(srctFormat) > 0;
            int stride = (int)mat.Step();
            for (int i = 0; i < (isPlanar ? channels : 1); i++)
            {
                FFmpegHelper.CopyMemory(mat.Data + i * stride, frame.Data[i], stride);
            }
            return frame;
        }

        private unsafe static VideoFrame MatToVideoFrame(Mat mat)
        {
            if (mat.Type() != MatType.CV_8UC3)
                throw new FFmpegException(FFmpegException.NotSupportFormat);
            VideoFrame frame = new VideoFrame(mat.Width, mat.Height, AVPixelFormat.AV_PIX_FMT_BGR24);
            int stride = (int)mat.Step();
            var bytewidth = Math.Min(stride, frame.AVFrame.linesize[0]);
            ffmpeg.av_image_copy_plane((byte*)frame.Data[0], frame.AVFrame.linesize[0], mat.DataPointer, stride, bytewidth, frame.AVFrame.height);
            return frame;
        }


        /// <summary>
        /// Convert to <see cref="long"/> array use <see cref="BitConverter.DoubleToInt64Bits(double)"/>
        /// </summary>
        /// <param name="vec"></param>
        /// <param name="length">length, -1 is auto get</param>
        /// <returns></returns>

        public static long[] ToInt64Bits(this IVec<double> vec, int length = -1)
        {
            var c = length < 0 ? vec.Count() : length;
            long[] output = new long[c];
            for (int i = 0; i < c; i++)
            {
                output[i] = BitConverter.DoubleToInt64Bits(vec[i]);
            }
            return output;
        }

        /// <summary>
        /// DO NOT call frequently. get count by <see cref="ArgumentOutOfRangeException"/>
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>

        public static int Count(this IVec<double> vec)
        {
            int i = 0;
            try
            {
                double tmp = 0;
                while (true)
                {
                    tmp = vec[i];
                    i += 1;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                return i;
            }
        }
    }
}
