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

        public static double ToDouble(this AVRational rational)
        {
            return ffmpeg.av_q2d(rational);
        }

        public static AVRational ToTranspose(this AVRational src, bool srcZeroAllow = true, bool dstZeroAllow = true)
        {
            AVRational dst = new AVRational() { den = src.num, num = src.den };
            if (!srcZeroAllow && src.den == 0)
                throw new FFmpegException(new ArgumentException($"{nameof(srcZeroAllow)} == {srcZeroAllow} && src.den == {src.den}"));
            if (!dstZeroAllow && dst.den == 0)
                throw new FFmpegException(new ArgumentException($"{nameof(dstZeroAllow)} == {dstZeroAllow} && dst.den == {dst.den}"));
            return dst;
        }

        /// <summary>
        /// Set ffmpeg internal log
        /// </summary>
        /// <param name="logLevel">
        /// log level
        /// <list type="bullet">
        ///    <item>
        ///        <term>64</term>
        ///        <description><see cref="ffmpeg.AV_LOG_MAX_OFFSET"/></description>
        ///    </item>
        ///    <item>
        ///        <term>56</term>
        ///        <description><see cref="ffmpeg.AV_LOG_TRACE"/></description>
        ///    </item>
        ///    <item>
        ///        <term>48</term>
        ///        <description><see cref="ffmpeg.AV_LOG_DEBUG"/></description>
        ///    </item>
        ///    <item>
        ///        <term>40</term>
        ///        <description><see cref="ffmpeg.AV_LOG_VERBOSE"/></description>
        ///    </item>
        ///    <item>
        ///        <term>16</term>
        ///        <description><see cref="ffmpeg.AV_LOG_ERROR"/></description>
        ///    </item>
        ///    <item>
        ///        <term>24</term>
        ///        <description><see cref="ffmpeg.AV_LOG_WARNING"/></description>
        ///    </item>
        ///    <item>
        ///        <term>8</term>
        ///        <description><see cref="ffmpeg.AV_LOG_FATAL"/></description>
        ///    </item>
        ///    <item>
        ///        <term>0</term>
        ///        <description><see cref="ffmpeg.AV_LOG_PANIC"/></description>
        ///    </item>
        ///    <item>
        ///        <term>-8</term>
        ///        <description><see cref="ffmpeg.AV_LOG_QUIET"/></description>
        ///    </item>
        ///</list>
        /// </param>
        /// <param name="logFlags">
        /// log flags, support AND operator
        /// <list type="bullet">
        ///    <item>
        ///        <term>1</term>
        ///        <description><see cref="ffmpeg.AV_LOG_SKIP_REPEATED"/></description>
        ///    </item>
        ///    <item>
        ///        <term>2</term>
        ///        <description><see cref="ffmpeg.AV_LOG_PRINT_LEVEL"/></description>
        ///    </item>
        /// </list>
        /// </param>
        /// <param name="writeLogAction">set <see langword="null"/> to use default log output</param>
        public static unsafe void SetupLogging(int logLevel = ffmpeg.AV_LOG_VERBOSE, int logFlags = ffmpeg.AV_LOG_PRINT_LEVEL, Action<string> writeLogAction = null)
        {
            ffmpeg.av_log_set_level(logLevel);
            ffmpeg.av_log_set_flags(logFlags);

            if (writeLogAction == null)
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
                    writeLogAction.Invoke(((IntPtr)lineBuffer).PtrToStringUTF8());
                };
            }
            ffmpeg.av_log_set_callback(logCallback);
        }

        private static unsafe av_log_set_callback_callback logCallback;
    }
}