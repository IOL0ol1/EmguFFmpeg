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
        public static void RegisterBinaries(string path = "")
        {
            ffmpeg.RootPath = path;
            Trace.TraceInformation($"{nameof(ffmpeg.av_version_info)} : {ffmpeg.av_version_info()}");
        }

        public unsafe static string PtrToStringUTF8(this IntPtr ptr)
        {
            if (IntPtr.Zero == ptr)
                return null;
            int length = 0;
            sbyte* psbyte = (sbyte*)ptr;
            while (psbyte[length] != 0)
                length++;
            return new string(psbyte, 0, length, Encoding.UTF8);
        }

        public static int ThrowExceptionIfError(this int error)
        {
            return error < 0 ? throw new FFmpegException(error) : error;
        }

        public static int ToChannels(this AVChannelLayout channelLayout)
        {
            return ffmpeg.av_get_channel_layout_nb_channels((ulong)channelLayout);
        }

        public static double ToDouble(this AVRational rational)
        {
            return ffmpeg.av_q2d(rational);
        }

        public static AVRational ToTranspose(this AVRational rational)
        {
            return new AVRational() { den = rational.num, num = rational.den };
        }

        /// <summary>
        /// Set ffmpeg log
        /// </summary>
        /// <param name="logLevel">log level</param>
        /// <param name="logFlags">log flags, support AND operator </param>
        /// <param name="logAction">set <see langword="null"/> to use default log output</param>
        public static unsafe void SetupLogging(LogLevel logLevel = LogLevel.Verbose, LogFlags logFlags = LogFlags.PrintLevel, Action<string> logAction = null)
        {
            ffmpeg.av_log_set_level((int)logLevel);
            ffmpeg.av_log_set_flags((int)logFlags);

            if (logAction == null)
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
                    logAction.Invoke(((IntPtr)lineBuffer).PtrToStringUTF8());
                };
            }
            ffmpeg.av_log_set_callback(logCallback);
        }

        private static unsafe av_log_set_callback_callback logCallback;
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

        // No effect??
        //SkipRepeated = ffmpeg.AV_LOG_SKIP_REPEATED,
        PrintLevel = ffmpeg.AV_LOG_PRINT_LEVEL,
    }
}