using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{

    public static class AVRationalExtension
    {
        public static AVRational ToInvert(this AVRational rational)
        {
            return ffmpeg.av_inv_q(rational);
        }

        /// <summary>
        /// Convert a double precision floating point number to a rational.
        /// </summary>
        /// <param name="value">`double` to convert</param>
        /// <param name="max">Maximum allowed numerator and denominator</param>
        /// <returns></returns>
        public static AVRational ToRational(this double value, int max = 100000)
        {
            return ffmpeg.av_d2q(value, max);
        }

        public static AVRational ToRational(this int value)
        {
            return new AVRational { den = 1, num = value };
        }

        public static double ToDouble(this AVRational rational)
        {
            return ffmpeg.av_q2d(rational);
        }

        /// <summary>
        /// Rescale a 64-bit integer from <paramref name="srcTimeBase"/> to <paramref name="dstTimeBase"/>. Wraps <c>av_rescale_q</c>.
        /// </summary>
        public static long Rescale(this long a, AVRational srcTimeBase, AVRational dstTimeBase) => ffmpeg.av_rescale_q(a, srcTimeBase, dstTimeBase);
    }

    public static class AVChannelLayoutExtension
    {
        public static unsafe AVChannelLayout ToDefaultChLayout(this int nb_channels)
        {
            var chLayout = new AVChannelLayout();
            ffmpeg.av_channel_layout_default(&chLayout, nb_channels);
            return chLayout;
        }

        public static unsafe AVChannelLayout Copy(this AVChannelLayout channelLayout)
        {
            var chLayout = new AVChannelLayout();
            ffmpeg.av_channel_layout_copy(&chLayout, &channelLayout);
            return chLayout;
        }

        public static bool IsContentEqual(this AVChannelLayout value, AVChannelLayout layout)
        {
            return value.nb_channels == layout.nb_channels
                && value.order == layout.order
                && value.u.mask == layout.u.mask;
        }

        /// <summary>
        /// Initialize a channel layout from the given channel mask. Wraps <c>av_channel_layout_from_mask</c>.
        /// </summary>
        public static unsafe AVChannelLayout ToChLayout(this ulong mask)
        {
            var chLayout = new AVChannelLayout();
            ffmpeg.av_channel_layout_from_mask(&chLayout, mask).ThrowIfError();
            return chLayout;
        }

        /// <summary>
        /// Return a human-readable description of this channel layout (e.g. <c>"stereo"</c>, <c>"5.1"</c>).
        /// Wraps <c>av_channel_layout_describe</c>. Returns an empty string when the layout is
        /// <c>AV_CHANNEL_ORDER_UNSPEC</c>; consider calling <see cref="ToDefaultChLayout"/> first.
        /// </summary>
        public static unsafe string Describe(this AVChannelLayout channelLayout)
        {
            fixed (byte* p = new byte[64])
            {
                ffmpeg.av_channel_layout_describe(&channelLayout, p, 64);
                return ((IntPtr)p).PtrToStringUTF8();
            }
        }
    }

    public static class AVSampleFormatExtension
    {
        public static string GetName(this AVSampleFormat sampleFormat)
        {
            return ffmpeg.av_get_sample_fmt_name(sampleFormat);
        }

        /// <summary>
        /// Check if the sample format is planar. Wraps <c>av_sample_fmt_is_planar</c>.
        /// </summary>
        public static bool IsPlanar(this AVSampleFormat sampleFormat) => ffmpeg.av_sample_fmt_is_planar(sampleFormat) != 0;

        /// <summary>
        /// Get the packed alternative form of the sample format. Wraps <c>av_get_packed_sample_fmt</c>.
        /// </summary>
        public static AVSampleFormat ToPacked(this AVSampleFormat sampleFormat) => ffmpeg.av_get_packed_sample_fmt(sampleFormat);

        /// <summary>
        /// Get the number of bytes per sample. Wraps <c>av_get_bytes_per_sample</c>.
        /// </summary>
        public static int GetBytesPerSample(this AVSampleFormat sampleFormat) => ffmpeg.av_get_bytes_per_sample(sampleFormat);
    }

    public static class AVPixelFormatExtension
    {
        public static string GetName(this AVPixelFormat pixelFormat)
        {
            return ffmpeg.av_get_pix_fmt_name(pixelFormat);
        }
    }

    public static class AVHWDeviceTypeExtension
    {
        /// <summary>
        /// Get the string name of the hardware device type. Wraps <c>av_hwdevice_get_type_name</c>.
        /// </summary>
        public static string GetName(this AVHWDeviceType type)
        {
            return ffmpeg.av_hwdevice_get_type_name(type);
        }
    }

    public static class IntPtrExtension
    {

        /// <summary>
        /// Copies all characters up to the first null character from an unmanaged UTF8 string
        ///     to a managed <see langword="string"/>, and widens each UTF8 character to Unicode.
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public static unsafe string PtrToStringUTF8(this IntPtr ptr)
        {
#if NETSTANDARD2_1_OR_GREATER
            return System.Runtime.InteropServices.Marshal.PtrToStringUTF8(ptr);
#else
            if (IntPtr.Zero == ptr) return null;
            var length = 0;
            var psbyte = (sbyte*)ptr;
            while (psbyte[length] != 0)
                length++; 
            return new string(psbyte, 0, length, System.Text.Encoding.UTF8);
#endif
        }
    } 

    public static class ExceptionExtension
    {
        /// <summary>
        /// Throw if it's ffmpeg error code
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        /// <exception cref="FFmpegException"></exception>
        public static int ThrowIfError(this int error)
        {
            return error < 0 ? throw new FFmpegException(error) : error;
        }

        /// <summary>
        /// Throw if it's ffmpeg error code
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        /// <exception cref="FFmpegException"></exception>
        public static long ThrowIfError(this long error)
        {
            return error < 0 ? throw new FFmpegException((int)error) : error;
        }
    }

}

