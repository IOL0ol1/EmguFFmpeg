using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    /// <summary>
    /// Strongly-typed names for the FFmpeg error codes most commonly checked in application code.
    /// The values are negative integers (AVERROR mapping); compare with <see cref="FFmpegException.ErrorCode"/>.
    /// </summary>
    public enum FFmpegErrorCode
    {
        /// <summary>Some unknown error.</summary>
        Unknown = 0,
        /// <summary>
        /// Resource temporarily unavailable — send/receive must drain first.
        /// WARNING: the numeric value is platform-dependent (-11 on Windows/Linux, -35 on macOS) —
        /// do NOT compare <see cref="FFmpegException.ErrorCode"/> against this constant directly;
        /// use <see cref="FFmpegException.TypedCode"/>, which maps via <c>ffmpeg.AVERROR(EAGAIN)</c>.
        /// </summary>
        EAGAIN = -11,
        /// <summary>End of file or end of stream.</summary>
        EOF = -541478725, // AVERROR_EOF = FFERRTAG('E','O','F',' ')
        /// <summary>Invalid data found when processing input.</summary>
        InvalidData = -1094995529, // AVERROR_INVALIDDATA
        /// <summary>Bug detected, please report the issue.</summary>
        Bug = -558323010,
        /// <summary>Required feature missing.</summary>
        Decoder_Not_Found = -1128613112,
        /// <summary>Generic external library/callback failure.</summary>
        External = -542398533,
    }

    /// <summary>
    /// FFmpeg exception.
    /// </summary>
    public unsafe class FFmpegException : Exception
    {
        public int ErrorCode { get; } = 0;

        /// <summary>Typed view of <see cref="ErrorCode"/> for the canonical FFmpeg error values.</summary>
        public FFmpegErrorCode TypedCode => ToTyped(ErrorCode);

        public FFmpegException(int errorCode) : base($"{FFmpegError} [{errorCode}] {GetErrorString(errorCode)}")
        {
            ErrorCode = errorCode;
        }

        public FFmpegException(int errorCode, string message) : base($"{FFmpegError} [{errorCode}] {GetErrorString(errorCode)} {message}")
        {
            ErrorCode = errorCode;
        }

        public FFmpegException(string message) : base($"{FFmpegError} {message}")
        { }

        public FFmpegException(string message, Exception innerException) : base($"{FFmpegError} {message}", innerException)
        { }

        /// <summary>
        /// Get ffmpeg error string by error code
        /// </summary>
        public static string GetErrorString(int errorCode)
        {
            byte* buffer = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE];
            ffmpeg.av_strerror(errorCode, buffer, ffmpeg.AV_ERROR_MAX_STRING_SIZE);
            return ((IntPtr)buffer).PtrToStringUTF8();
        }

        /// <summary>Map a raw error code to a typed enum value (Unknown if the code is not one we recognize).</summary>
        public static FFmpegErrorCode ToTyped(int errorCode)
        {
            if (errorCode == ffmpeg.AVERROR(ffmpeg.EAGAIN)) return FFmpegErrorCode.EAGAIN;
            if (errorCode == ffmpeg.AVERROR_EOF) return FFmpegErrorCode.EOF;
            if (errorCode == ffmpeg.AVERROR_INVALIDDATA) return FFmpegErrorCode.InvalidData;
            if (errorCode == ffmpeg.AVERROR_BUG) return FFmpegErrorCode.Bug;
            if (errorCode == ffmpeg.AVERROR_DECODER_NOT_FOUND) return FFmpegErrorCode.Decoder_Not_Found;
            if (errorCode == ffmpeg.AVERROR_EXTERNAL) return FFmpegErrorCode.External;
            return FFmpegErrorCode.Unknown;
        }

        private const string FFmpegError = "FFmpeg error";
    }
}
