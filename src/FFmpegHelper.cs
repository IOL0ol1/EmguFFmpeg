using System;
using System.Text;

using FFmpeg.AutoGen;

namespace EmguFFmpeg
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

        /// <summary>
        /// throw exception if error when it's true, otherwise return error code
        /// </summary>
        public static bool IsThrowIfError { get; set; } = true;

        internal static int ThrowIfError(this int error)
        {
            return IsThrowIfError ? (error < 0 ? throw new FFmpegException(error) : error) : error;
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


        #endregion

        /// <summary>
        /// Return default channel layout for a given number of channels use <see cref="ffmpeg.av_get_default_channel_layout(int)"/>
        /// </summary>
        /// <param name="channels"></param>
        /// <returns></returns>

        public static AVChannelLayout GetChannelLayout(int channels)
        {
            if (channels > 64)
                throw new FFmpegException(FFmpegException.TooManyChannels);
            AVChannelLayout result = new AVChannelLayout();
            //AVChannelLayout* p = &result;
            //ffmpeg.av_channel_layout_default(p, channels);
            result.nb_channels = channels;
            result.order = AVChannelOrder.AV_CHANNEL_ORDER_UNSPEC;
            result.u.mask = (ulong)ffmpeg.av_get_default_channel_layout(channels);
            return result;
        }

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
        public static IntPtr2Ptr Pointer(IntPtr ptr) => new IntPtr2Ptr(ptr);

        /// <summary>
        /// create a pointer to <see langword="null"/>.
        /// </summary>
        /// <returns></returns>
        public static IntPtr2Ptr Null => new IntPtr2Ptr(IntPtr.Zero);

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
        /// <see cref="ffmpeg.AV_LOG_INFO"/>
        /// </summary>
        Info = ffmpeg.AV_LOG_INFO,
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
}
