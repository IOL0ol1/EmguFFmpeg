using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using FFmpeg.AutoGen;

namespace FFmpegSharp
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
    }

    public static class AVSampleFormatExtension
    {
        public static string GetName(this AVSampleFormat sampleFormat)
        {
            return ffmpeg.av_get_sample_fmt_name(sampleFormat);
        }
    }

    public static class AVPixelFormatExtension
    {
        public static string GetName(this AVPixelFormat pixelFormat)
        {
            return ffmpeg.av_get_pix_fmt_name(pixelFormat);
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
            return Marshal.PtrToStringUTF8(ptr);
#else
            if (IntPtr.Zero == ptr)
                return null;
            var length = 0;
            var psbyte = (sbyte*)ptr;
            while (psbyte[length] != 0)
                length++;
            return new string(psbyte, 0, length, Encoding.UTF8);
#endif
        }
    }

    public unsafe class IntPtrPtr<T> where T : unmanaged
    {
        public T* Ptr;

        public IntPtrPtr()
        { }

        public IntPtrPtr(IntPtr ptr)
        {
            Ptr = (T*)ptr;
        }
    }

    public unsafe class IntPtrPtr
    {
        public void* Ptr = null;

        public IntPtrPtr()
        { }

        public IntPtrPtr(IntPtr ptr)
        {
            Ptr = (void*)ptr;
        }
    }

    internal static class TExtension
    {

        internal static int ThrowIfError(this int error)
        {
            return error < 0 ? throw new FFmpegException(error) : error;
        }

        internal static long ThrowIfError(this long error)
        {
            return error < 0 ? throw new FFmpegException((int)error) : error;
        }
    }

}

