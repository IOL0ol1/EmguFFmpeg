using FFmpeg.AutoGen;

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace EmguFFmpeg
{
    public static class FFmpegHelper
    {
        /// <summary>
        /// Set ffmpeg root path, default path is same as executable file, return <see cref="ffmpeg.av_version_info"/>
        /// </summary>
        /// <param name="path"></param>
        public static string RegisterBinaries(string path = "")
        {
            ffmpeg.RootPath = path;
            return ffmpeg.av_version_info();
        }

        /// <summary>
        /// Set ffmpeg log
        /// </summary>
        /// <param name="logLevel">log level</param>
        /// <param name="logFlags">log flags, support &amp; operator </param>
        /// <param name="logWrite">set <see langword="null"/> to use default log output</param>
        public static void SetupLogging(LogLevel logLevel = LogLevel.Verbose, LogFlags logFlags = LogFlags.PrintLevel, Action<string> logWrite = null)
        {
            unsafe
            {
                ffmpeg.av_log_set_level((int)logLevel);
                ffmpeg.av_log_set_flags((int)logFlags);

                if (logWrite == null)
                {
                    logCallback = ffmpeg.av_log_default_callback;
                }
                else
                {
                    logCallback = (p0, level, format, vl) =>
                    {
                        if (level > ffmpeg.av_log_get_level()) return;
                        var lineSize = 1024;
                        var printPrefix = 1;
                        var lineBuffer = stackalloc byte[lineSize];
                        ffmpeg.av_log_format_line(p0, level, format, vl, lineBuffer, lineSize, &printPrefix);
                        logWrite.Invoke(((IntPtr)lineBuffer).PtrToStringUTF8());
                    };
                }
                ffmpeg.av_log_set_callback(logCallback);
            }
        }

        private static unsafe av_log_set_callback_callback logCallback;

        #region Extension

        public static string PtrToStringUTF8(this IntPtr ptr)
        {
            unsafe
            {
                if (IntPtr.Zero == ptr)
                    return null;
                int length = 0;
                sbyte* psbyte = (sbyte*)ptr;
                while (psbyte[length] != 0)
                    length++;
                return new string(psbyte, 0, length, Encoding.UTF8);
            }
        }

        internal static int ThrowExceptionIfError(this int error)
        {
            return error < 0 ? throw new FFmpegException(error) : error;
        }

        public static double ToDouble(this AVRational rational)
        {
            return ffmpeg.av_q2d(rational);
        }

        public static AVRational ToInvert(this AVRational rational)
        {
            return ffmpeg.av_inv_q(rational);
        }

        public static int ToChannels(this AVChannelLayout channelLayout)
        {
            return ffmpeg.av_get_channel_layout_nb_channels((ulong)channelLayout);
        }

        #endregion

        public static ulong GetChannelLayout(int channels)
        {
            if (channels > 64)
                throw new FFmpegException(FFmpegException.TooManyChannels);
            ulong result;
            if ((result = (ulong)ffmpeg.av_get_default_channel_layout(channels)) > 0)
                return result;
            while (channels-- > 0)
            {
                result |= 1ul << channels;
            }
            return result;
        }

        /// <summary>
        /// Copy <paramref name="src"/> unmanaged memory to <paramref name="dst"/> unmanaged memory.
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="src"></param>
        /// <param name="count"></param>
        public static void CopyMemory(IntPtr dst, IntPtr src, int count)
        {
            unsafe
            {
                CopyMemory((byte*)dst, (byte*)src, count);
            }
        }

        /// <summary>
        /// [unsafe] Copy <paramref name="src"/> unmanaged memory to <paramref name="dst"/> unmanaged memory.
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="src"></param>
        /// <param name="count"></param>
        public unsafe static void CopyMemory(void* dst, void* src, int count)
        {
            ffmpeg.av_image_copy_plane((byte*)dst, count, (byte*)src, count, count, 1);
        }

        /// <summary>
        /// Copy <paramref name="src"/> unmanaged memory to <paramref name="dst"/> unmanaged memory.
        /// That is, copy "<paramref name="height"/>" number of lines of "<paramref name="byteWidth"/>" bytes each. 
        /// <para>
        /// The first byte of each <paramref name="src"/> or <paramref name="dst"/> successive line is separated by <paramref name="srcLineSize"/> or <paramref name="dstLineSize"/> bytes.
        /// </para>
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="dstLineSize">linesize for the image plane in dst</param>
        /// <param name="src"></param>
        /// <param name="srcLineSize">linesize for the image plane in src</param>
        /// <param name="byteWidth"></param>
        /// <param name="height"></param>
        public static void CopyPlane(IntPtr dst,int dstLineSize ,IntPtr src, int srcLineSize, int byteWidth, int height)
        {
            unsafe
            {
                ffmpeg.av_image_copy_plane((byte*)dst, dstLineSize, (byte*)src, srcLineSize, byteWidth, height);
            }
        }
    }

    public enum LogLevel : int
    {
        All = ffmpeg.AV_LOG_MAX_OFFSET,
        Trace = ffmpeg.AV_LOG_TRACE,
        Debug = ffmpeg.AV_LOG_DEBUG,
        Verbose = ffmpeg.AV_LOG_VERBOSE,
        Error = ffmpeg.AV_LOG_ERROR,
        Warning = ffmpeg.AV_LOG_WARNING,
        Fatal = ffmpeg.AV_LOG_FATAL,
        Panic = ffmpeg.AV_LOG_PANIC,
        Quiet = ffmpeg.AV_LOG_QUIET,
    }

    [Flags]
    public enum LogFlags : int
    {
        None = 0,
        SkipRepeated = ffmpeg.AV_LOG_SKIP_REPEATED,
        PrintLevel = ffmpeg.AV_LOG_PRINT_LEVEL,
    }

    [Flags]
    public enum BufferSrcFlags : int
    {
        /// <summary>
        /// takes ownership of the reference passed to it.
        /// </summary>
        None = 0,

        /// <summary>
        /// AV_BUFFERSRC_FLAG_NO_CHECK_FORMAT, Do not check for format changes.
        /// </summary>
        NoCheckFormat = 1,

        /// <summary>
        /// AV_BUFFERSRC_FLAG_PUSH, Immediately push the frame to the output.
        /// </summary>
        Push = 4,

        /// <summary>
        /// AV_BUFFERSRC_FLAG_KEEP_REF, Keep a reference to the frame.
        /// If the frame if reference-counted, create a new reference; otherwise
        /// copy the frame data.
        /// </summary>
        KeepRef = 8,
    }

    [Flags]
    public enum AVChannelLayout : ulong
    {
        AV_CH_FRONT_LEFT = 0x00000001UL,
        AV_CH_FRONT_RIGHT = 0x00000002UL,
        AV_CH_FRONT_CENTER = 0x00000004UL,
        AV_CH_LOW_FREQUENCY = 0x00000008UL,
        AV_CH_BACK_LEFT = 0x00000010UL,
        AV_CH_BACK_RIGHT = 0x00000020UL,
        AV_CH_FRONT_LEFT_OF_CENTER = 0x00000040UL,
        AV_CH_FRONT_RIGHT_OF_CENTER = 0x00000080UL,
        AV_CH_BACK_CENTER = 0x00000100UL,
        AV_CH_SIDE_LEFT = 0x00000200UL,
        AV_CH_SIDE_RIGHT = 0x00000400UL,
        AV_CH_TOP_CENTER = 0x00000800UL,
        AV_CH_TOP_FRONT_LEFT = 0x00001000UL,
        AV_CH_TOP_FRONT_CENTER = 0x00002000UL,
        AV_CH_TOP_FRONT_RIGHT = 0x00004000UL,
        AV_CH_TOP_BACK_LEFT = 0x00008000UL,
        AV_CH_TOP_BACK_CENTER = 0x00010000UL,
        AV_CH_TOP_BACK_RIGHT = 0x00020000UL,
        AV_CH_STEREO_LEFT = 0x20000000UL,
        AV_CH_STEREO_RIGHT = 0x40000000UL,
        AV_CH_WIDE_LEFT = 0x0000000080000000UL,
        AV_CH_WIDE_RIGHT = 0x0000000100000000UL,
        AV_CH_SURROUND_DIRECT_LEFT = 0x0000000200000000UL,
        AV_CH_SURROUND_DIRECT_RIGHT = 0x0000000400000000UL,
        AV_CH_LOW_FREQUENCY_2 = 0x0000000800000000UL,
        AV_CH_LAYOUT_MONO = (AV_CH_FRONT_CENTER),
        AV_CH_LAYOUT_STEREO = (AV_CH_FRONT_LEFT | AV_CH_FRONT_RIGHT),
        AV_CH_LAYOUT_2POINT1 = (AV_CH_LAYOUT_STEREO | AV_CH_LOW_FREQUENCY),
        AV_CH_LAYOUT_2_1 = (AV_CH_LAYOUT_STEREO | AV_CH_BACK_CENTER),
        AV_CH_LAYOUT_SURROUND = (AV_CH_LAYOUT_STEREO | AV_CH_FRONT_CENTER),
        AV_CH_LAYOUT_3POINT1 = (AV_CH_LAYOUT_SURROUND | AV_CH_LOW_FREQUENCY),
        AV_CH_LAYOUT_4POINT0 = (AV_CH_LAYOUT_SURROUND | AV_CH_BACK_CENTER),
        AV_CH_LAYOUT_4POINT1 = (AV_CH_LAYOUT_4POINT0 | AV_CH_LOW_FREQUENCY),
        AV_CH_LAYOUT_2_2 = (AV_CH_LAYOUT_STEREO | AV_CH_SIDE_LEFT | AV_CH_SIDE_RIGHT),
        AV_CH_LAYOUT_QUAD = (AV_CH_LAYOUT_STEREO | AV_CH_BACK_LEFT | AV_CH_BACK_RIGHT),
        AV_CH_LAYOUT_5POINT0 = (AV_CH_LAYOUT_SURROUND | AV_CH_SIDE_LEFT | AV_CH_SIDE_RIGHT),
        AV_CH_LAYOUT_5POINT1 = (AV_CH_LAYOUT_5POINT0 | AV_CH_LOW_FREQUENCY),
        AV_CH_LAYOUT_5POINT0_BACK = (AV_CH_LAYOUT_SURROUND | AV_CH_BACK_LEFT | AV_CH_BACK_RIGHT),
        AV_CH_LAYOUT_5POINT1_BACK = (AV_CH_LAYOUT_5POINT0_BACK | AV_CH_LOW_FREQUENCY),
        AV_CH_LAYOUT_6POINT0 = (AV_CH_LAYOUT_5POINT0 | AV_CH_BACK_CENTER),
        AV_CH_LAYOUT_6POINT0_FRONT = (AV_CH_LAYOUT_2_2 | AV_CH_FRONT_LEFT_OF_CENTER | AV_CH_FRONT_RIGHT_OF_CENTER),
        AV_CH_LAYOUT_HEXAGONAL = (AV_CH_LAYOUT_5POINT0_BACK | AV_CH_BACK_CENTER),
        AV_CH_LAYOUT_6POINT1 = (AV_CH_LAYOUT_5POINT1 | AV_CH_BACK_CENTER),
        AV_CH_LAYOUT_6POINT1_BACK = (AV_CH_LAYOUT_5POINT1_BACK | AV_CH_BACK_CENTER),
        AV_CH_LAYOUT_6POINT1_FRONT = (AV_CH_LAYOUT_6POINT0_FRONT | AV_CH_LOW_FREQUENCY),
        AV_CH_LAYOUT_7POINT0 = (AV_CH_LAYOUT_5POINT0 | AV_CH_BACK_LEFT | AV_CH_BACK_RIGHT),
        AV_CH_LAYOUT_7POINT0_FRONT = (AV_CH_LAYOUT_5POINT0 | AV_CH_FRONT_LEFT_OF_CENTER | AV_CH_FRONT_RIGHT_OF_CENTER),
        AV_CH_LAYOUT_7POINT1 = (AV_CH_LAYOUT_5POINT1 | AV_CH_BACK_LEFT | AV_CH_BACK_RIGHT),
        AV_CH_LAYOUT_7POINT1_WIDE = (AV_CH_LAYOUT_5POINT1 | AV_CH_FRONT_LEFT_OF_CENTER | AV_CH_FRONT_RIGHT_OF_CENTER),
        AV_CH_LAYOUT_7POINT1_WIDE_BACK = (AV_CH_LAYOUT_5POINT1_BACK | AV_CH_FRONT_LEFT_OF_CENTER | AV_CH_FRONT_RIGHT_OF_CENTER),
        AV_CH_LAYOUT_OCTAGONAL = (AV_CH_LAYOUT_5POINT0 | AV_CH_BACK_LEFT | AV_CH_BACK_CENTER | AV_CH_BACK_RIGHT),
        AV_CH_LAYOUT_HEXADECAGONAL = (AV_CH_LAYOUT_OCTAGONAL | AV_CH_WIDE_LEFT | AV_CH_WIDE_RIGHT | AV_CH_TOP_BACK_LEFT | AV_CH_TOP_BACK_RIGHT | AV_CH_TOP_BACK_CENTER | AV_CH_TOP_FRONT_CENTER | AV_CH_TOP_FRONT_LEFT | AV_CH_TOP_FRONT_RIGHT),
        AV_CH_LAYOUT_STEREO_DOWNMIX = (AV_CH_STEREO_LEFT | AV_CH_STEREO_RIGHT),
    }
}