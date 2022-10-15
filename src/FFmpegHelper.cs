using System;
using System.Globalization;
using System.Text;

using FFmpeg.AutoGen;

namespace FFmpegSharp
{
    public unsafe static class FFmpegHelper
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
        public static void SetupLogging(LogLevel logLevel = LogLevel.Verbose, LogFlags logFlags = LogFlags.PrintLevel, Action<string, int> logWrite = null)
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
                    logWrite.Invoke(((IntPtr)lineBuffer).PtrToStringUTF8(), level);
                };
            }
            ffmpeg.av_log_set_callback(logCallback);
        }

        private static av_log_set_callback_callback logCallback;

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
            if (IntPtr.Zero == ptr)
                return null;
            int length = 0;
            sbyte* psbyte = (sbyte*)ptr;
            while (psbyte[length] != 0)
                length++;
            return new string(psbyte, 0, length, Encoding.UTF8);
        }


        internal static T ThrowIfError<T>(this T error) where T : struct, IComparable, IFormattable, IConvertible, IComparable<T>, IEquatable<T>
        {
            if (error is int _int)
                return _int < 0 ? throw new FFmpegException(_int) : error;
            else if (error is long _long)
                return _long < 0 ? throw new FFmpegException((int)_long) : error;
            else
                return error.CompareTo(default(T)) < 0 ? throw new FFmpegException(error.ToInt32(NumberFormatInfo.InvariantInfo)) : error;
        }

        #endregion Extension

        /// <summary>
        /// Copy <paramref name="src"/> unmanaged memory to <paramref name="dst"/> unmanaged memory.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="count"></param>
        public static void CopyMemory(IntPtr src, IntPtr dst, int count)
        {
            CopyMemory((byte*)src, (byte*)dst, count);
        }

        /// <summary>
        /// [Unsafe] Copy <paramref name="src"/> unmanaged memory to <paramref name="dst"/> unmanaged memory.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="count"></param>
        public static void CopyMemory(void* src, void* dst, int count)
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
        /// <param name="src">source address.</param>
        /// <param name="srcLineSize">linesize for the image plane in src.</param>
        /// <param name="dst">destination address.</param>
        /// <param name="dstLineSize">linesize for the image plane in dst.</param>
        /// <param name="byteWidth">the number of bytes copied per line.</param>
        /// <param name="height">the number of rows.</param>
        public static void CopyPlane(IntPtr src, int srcLineSize, IntPtr dst, int dstLineSize, int byteWidth, int height)
        {
            ffmpeg.av_image_copy_plane((byte*)dst, dstLineSize, (byte*)src, srcLineSize, byteWidth, height);
        }
    }

    /// <summary>
    /// pointer to pointer (void**)
    /// </summary>
    public unsafe class IntPtr2Ptr
    {
        private void* _ptr;

        /// <summary>
        /// create pointer to <paramref name="ptr"/>.
        /// </summary>
        /// <param name="ptr"></param>
        public IntPtr2Ptr(IntPtr ptr)
        {
            _ptr = (void*)ptr;
            fixed (void** pptr = &_ptr)
            {
                Ptr2Ptr = (IntPtr)pptr;
            }
        }

        /// <summary>
        /// create a pointer to <paramref name="ptr"/>.
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public static IntPtr2Ptr GetPointer(IntPtr ptr) => new IntPtr2Ptr(ptr);

        /// <summary>
        /// create a pointer to <see langword="null"/>.
        /// </summary>
        /// <returns></returns>
        public static IntPtr2Ptr Ptr2Null => new IntPtr2Ptr(IntPtr.Zero);

        /// <summary>
        /// get pointer (ptr).
        /// </summary>
        public IntPtr Ptr => (IntPtr)_ptr;

        /// <summary>
        /// get pointer to pointer (&amp;ptr).
        /// </summary>
        public IntPtr Ptr2Ptr { get; private set; }

        public static implicit operator IntPtr(IntPtr2Ptr ptr2ptr)
        {
            if (ptr2ptr == null) return IntPtr.Zero;
            return ptr2ptr.Ptr2Ptr;
        }

        public static implicit operator void**(IntPtr2Ptr ptr2Ptr)
        {
            if (ptr2Ptr == null) return null;
            return (void**)ptr2Ptr.Ptr2Ptr;
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


}
