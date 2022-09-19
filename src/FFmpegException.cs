using System;
using System.Runtime.Serialization;
using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    /// <summary>
    /// FFmpeg exception
    /// </summary>
    [Serializable]
    public unsafe class FFmpegException : Exception
    {
        public int ErrorCode { get; } = 0;

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
        /// <param name="errorCode"></param>
        /// <returns></returns>
        public static string GetErrorString(int errorCode)
        {
            byte* buffer = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE];
            ffmpeg.av_strerror(errorCode, buffer, ffmpeg.AV_ERROR_MAX_STRING_SIZE);
            return ((IntPtr)buffer).PtrToStringUTF8();
        }

        protected FFmpegException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        { }

        private const string FFmpegError = "FFmpeg error";
    }
}
