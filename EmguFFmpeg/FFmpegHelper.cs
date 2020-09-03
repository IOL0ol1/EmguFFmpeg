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
        /// Set ffmpeg root path, return <see cref="ffmpeg.av_version_info"/>
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

        /// <summary>
        /// <see cref="ffmpeg.avdevice_register_all"/>
        /// </summary>
        public static void RegisterDevice()
        {
            ffmpeg.avdevice_register_all();
        }

        #region Extension

        /// <summary>
        /// Copies all characters up to the first null character from an unmanaged UTF8 string
        ///     to a managed <see langword="string"/>, and widens each UTF8 character to Unicode.
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
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

        /// <summary>
        /// throw exception if error when it's true, otherwise return error code
        /// </summary>
        public static bool IsThrowExceptionIfError { get; set; } = true;

        internal static int ThrowExceptionIfError(this int error)
        {
            return IsThrowExceptionIfError ? (error < 0 ? throw new FFmpegException(error) : error) : error;
        }

        /// <summary>
        /// Convert an <see cref="AVRational"/> to a double use <see cref="ffmpeg.av_q2d(AVRational)"/>.
        /// <para>
        /// NOTE: this will lose precision !!
        /// </para>
        /// </summary>
        /// <param name="rational"></param>
        /// <returns></returns>
        public static double ToDouble(this AVRational rational)
        {
            return ffmpeg.av_q2d(rational);
        }

        /// <summary>
        /// Invert a <see cref="AVRational"/> use <see cref="ffmpeg.av_inv_q(AVRational)"/>
        /// </summary>
        /// <param name="rational"></param>
        /// <returns></returns>
        public static AVRational ToInvert(this AVRational rational)
        {
            return ffmpeg.av_inv_q(rational);
        }

        /// <summary>
        /// Return the number of channels in the channel layout use <see cref="ffmpeg.av_get_channel_layout_nb_channels(ulong)"/>
        /// </summary>
        /// <param name="channelLayout"></param>
        /// <returns></returns>
        public static int ToChannels(this AVChannelLayout channelLayout)
        {
            return ffmpeg.av_get_channel_layout_nb_channels((ulong)channelLayout);
        }

        #endregion

        /// <summary>
        /// Return default channel layout for a given number of channels use <see cref="ffmpeg.av_get_default_channel_layout(int)"/>
        /// </summary>
        /// <param name="channels"></param>
        /// <returns></returns>

        public static ulong GetChannelLayout(int channels)
        {
            if (channels > 64)
                throw new FFmpegException(FFmpegException.TooManyChannels);
            ulong result;
            if ((result = (ulong)ffmpeg.av_get_default_channel_layout(channels)) > 0)
                return result;
            while (channels-- > 0)
                result |= 1ul << channels;
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
        /// [Unsafe] Copy <paramref name="src"/> unmanaged memory to <paramref name="dst"/> unmanaged memory.
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="src"></param>
        /// <param name="count"></param>
        public unsafe static void CopyMemory(void* dst, void* src, int count)
        {
            ffmpeg.av_image_copy_plane((byte*)dst, count, (byte*)src, count, count, 1);
        }

        /// <summary>
        /// Batch copy <paramref name="src"/> unmanaged memory to <paramref name="dst"/> unmanaged memory.
        /// <para>
        /// Copy "<paramref name="height"/>" number of lines in a row,"<paramref name="byteWidth"/>" bytes each. 
        /// The <paramref name="dst"/> address increments by <paramref name="dstLineSize"/> bytes per line.
        /// The <paramref name="src"/> address increments by <paramref name="srcLineSize"/> bytes per line.
        /// </para>
        /// </summary>
        /// <param name="dst">destination address.</param>
        /// <param name="dstLineSize">linesize for the image plane in dst.</param>
        /// <param name="src">source address.</param>
        /// <param name="srcLineSize">linesize for the image plane in src.</param>
        /// <param name="byteWidth">the number of bytes copied per line.</param>
        /// <param name="height">the number of rows.</param>
        public static void CopyPlane(IntPtr dst, int dstLineSize, IntPtr src, int srcLineSize, int byteWidth, int height)
        {
            unsafe
            {
                ffmpeg.av_image_copy_plane((byte*)dst, dstLineSize, (byte*)src, srcLineSize, byteWidth, height);
            }
        }
    }

    /// <summary>
    /// AV_LOG_
    /// </summary>
    public enum LogLevel : int
    {
        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_MAX_OFFSET"/>
        /// </summary>
        All = ffmpeg.AV_LOG_MAX_OFFSET,
        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_TRACE"/>
        /// </summary>
        Trace = ffmpeg.AV_LOG_TRACE,
        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_DEBUG"/>
        /// </summary>
        Debug = ffmpeg.AV_LOG_DEBUG,
        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_VERBOSE"/>
        /// </summary>
        Verbose = ffmpeg.AV_LOG_VERBOSE,
        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_WARNING"/>
        /// </summary>
        Warning = ffmpeg.AV_LOG_WARNING,
        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_ERROR"/>
        /// </summary>
        Error = ffmpeg.AV_LOG_ERROR,
        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_FATAL"/>
        /// </summary>
        Fatal = ffmpeg.AV_LOG_FATAL,
        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_PANIC"/>
        /// </summary>
        Panic = ffmpeg.AV_LOG_PANIC,
        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_QUIET"/>
        /// </summary>
        Quiet = ffmpeg.AV_LOG_QUIET,
    }

    [Flags]
    public enum LogFlags : int
    {
        None = 0,
        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_SKIP_REPEATED"/>
        /// </summary>
        SkipRepeated = ffmpeg.AV_LOG_SKIP_REPEATED,
        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_PRINT_LEVEL"/>
        /// </summary>
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
