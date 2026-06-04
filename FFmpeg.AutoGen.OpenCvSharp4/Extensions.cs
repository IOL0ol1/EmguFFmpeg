using System;

using FFmpeg.AutoGen;

using OpenCvSharp;

namespace FFmpeg.OpenCvSharp
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
                (int)1/*SwsFlags.SWS_BILINEAR*/, null, null, null);
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

        // ─────────────────────────── Audio ───────────────────────────

        /// <summary>
        /// Convert an audio <see cref="AVFrame"/> to a <see cref="Mat"/>.
        /// Layout: <c>rows = nb_channels, cols = nb_samples, CV_XXC1</c> where the depth
        /// matches the sample format (U8→CV_8U, S16→CV_16S, S32→CV_32S, FLT→CV_32F, DBL→CV_64F).
        /// Planar frames are copied plane by plane; packed (interleaved) frames are de-interleaved.
        /// The caller owns the returned <see cref="Mat"/> and must dispose it.
        /// </summary>
        public static Mat ToAudioMat(this AVFrame frame)
        {
            int nbChannels = frame.ch_layout.nb_channels;
            int nbSamples = frame.nb_samples;
            if (nbChannels <= 0 || nbSamples <= 0)
                throw new ArgumentException("AVFrame is not a valid audio frame (nb_channels/nb_samples must be positive).", nameof(frame));

            var sampleFmt = (AVSampleFormat)frame.format;
            var matDepth = SampleFormatToMatDepth(sampleFmt);
            int bytesPerSample = ffmpeg.av_get_bytes_per_sample(sampleFmt);
            bool isPlanar = ffmpeg.av_sample_fmt_is_planar(sampleFmt) != 0;

            var mat = new Mat(nbChannels, nbSamples, MatType.MakeType(matDepth, 1));

            if (isPlanar)
            {
                // Each plane holds one channel of contiguous samples.
                for (int ch = 0; ch < nbChannels; ch++)
                {
                    var dst = (byte*)mat.Ptr(ch);
                    var src = frame.data[(uint)ch];
                    Buffer.MemoryCopy(src, dst, (long)nbSamples * bytesPerSample, (long)nbSamples * bytesPerSample);
                }
            }
            else
            {
                // Packed / interleaved: data[0] = [ch0[0], ch1[0], ..., ch0[1], ch1[1], ...]
                var src = frame.data[0];
                for (int ch = 0; ch < nbChannels; ch++)
                {
                    var dst = (byte*)mat.Ptr(ch);
                    for (int s = 0; s < nbSamples; s++)
                        Buffer.MemoryCopy(src + ((long)s * nbChannels + ch) * bytesPerSample,
                                          dst + (long)s * bytesPerSample,
                                          bytesPerSample, bytesPerSample);
                }
            }

            return mat;
        }

        /// <summary>
        /// Convert a <see cref="Mat"/> back to a planar audio <see cref="AVFrame"/>.
        /// Expected layout: <c>rows = nb_channels, cols = nb_samples, CV_XXC1</c>.
        /// The returned frame owns ref-counted buffers; release with <see cref="ffmpeg.av_frame_unref(AVFrame*)"/> when done.
        /// </summary>
        /// <param name="mat">Audio matrix (rows = channels, cols = samples, single-channel).</param>
        /// <param name="sampleRate">Sample rate stored in the returned frame (0 if unknown).</param>
        public static AVFrame ToAudioFrame(this Mat mat, int sampleRate = 0)
        {
            if (mat == null) throw new ArgumentNullException(nameof(mat));
            if (mat.Empty()) throw new ArgumentException("Mat is empty.", nameof(mat));
            if (mat.Channels() != 1)
                throw new NotSupportedException("Audio Mat must be single-channel (CV_XXC1); rows=channels, cols=samples.");

            int nbChannels = mat.Rows;
            int nbSamples = mat.Cols;

            var sampleFmt = MatDepthToPlanarSampleFormat(mat.Depth());
            int bytesPerSample = ffmpeg.av_get_bytes_per_sample(sampleFmt);

            var frame = new AVFrame
            {
                format = (int)sampleFmt,
                nb_samples = nbSamples,
                sample_rate = sampleRate,
            };
            ffmpeg.av_channel_layout_default(&frame.ch_layout, nbChannels);
            ThrowIfError(ffmpeg.av_frame_get_buffer(&frame, 0), nameof(ffmpeg.av_frame_get_buffer));

            for (int ch = 0; ch < nbChannels; ch++)
            {
                var src = (byte*)mat.Ptr(ch);
                var dst = frame.data[(uint)ch];
                Buffer.MemoryCopy(src, dst, (long)nbSamples * bytesPerSample, (long)nbSamples * bytesPerSample);
            }

            return frame;
        }

        private static int SampleFormatToMatDepth(AVSampleFormat fmt)
        {
            switch (ffmpeg.av_get_packed_sample_fmt(fmt))
            {
                case AVSampleFormat.AV_SAMPLE_FMT_U8:  return MatType.CV_8U;
                case AVSampleFormat.AV_SAMPLE_FMT_S16: return MatType.CV_16S;
                case AVSampleFormat.AV_SAMPLE_FMT_S32: return MatType.CV_32S;
                case AVSampleFormat.AV_SAMPLE_FMT_FLT: return MatType.CV_32F;
                case AVSampleFormat.AV_SAMPLE_FMT_DBL: return MatType.CV_64F;
                default:
                    throw new NotSupportedException($"Unsupported audio sample format: {fmt}.");
            }
        }

        private static AVSampleFormat MatDepthToPlanarSampleFormat(int depth)
        {
            switch (depth)
            {
                case MatType.CV_8U:  return AVSampleFormat.AV_SAMPLE_FMT_U8P;
                case MatType.CV_16S: return AVSampleFormat.AV_SAMPLE_FMT_S16P;
                case MatType.CV_32S: return AVSampleFormat.AV_SAMPLE_FMT_S32P;
                case MatType.CV_32F: return AVSampleFormat.AV_SAMPLE_FMT_FLTP;
                case MatType.CV_64F: return AVSampleFormat.AV_SAMPLE_FMT_DBLP;
                default:
                    throw new NotSupportedException($"Unsupported Mat depth for audio: {depth}. Use CV_8U/CV_16S/CV_32S/CV_32F/CV_64F.");
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
