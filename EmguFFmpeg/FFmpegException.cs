using FFmpeg.AutoGen;

using System;
using System.Runtime.Serialization;

namespace EmguFFmpeg
{
    [Serializable]
    public class FFmpegException : Exception
    {
        public int ErrorCode { get; } = 0;

        public FFmpegException(int errorCode) : base($"{FFmpegMessage.FFmpegError} [{errorCode}] {GetErrorString(errorCode)}")
        {
            ErrorCode = errorCode;
        }

        public FFmpegException(int errorCode, string message) : base($"{FFmpegMessage.FFmpegError} [{errorCode}] {GetErrorString(errorCode)} {message}")
        {
            ErrorCode = errorCode;
        }

        public FFmpegException(string message) : base($"{FFmpegMessage.FFmpegError} {message}")
        { }

        public FFmpegException(string message, Exception innerException) : base($"{FFmpegMessage.FFmpegError} {message}", innerException)
        { }

        public static unsafe string GetErrorString(int errorCode)
        {
            byte* buffer = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE];
            ffmpeg.av_strerror(errorCode, buffer, ffmpeg.AV_ERROR_MAX_STRING_SIZE);
            return ((IntPtr)buffer).PtrToStringUTF8();
        }

        protected FFmpegException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        { }
    }

    internal static class FFmpegMessage
    {
        public const string FFmpegError = "FFmpeg error";
        public const string CodecIDError = "not supported codec id";
        public const string SampleRateError = "not supported sample rate";
        public const string FormatError = "not supported format";
        public const string ChannelLayoutError = "not supported channle layout";
        public const string NonNegative = "argument must be non-negative";
        public const string NullReference = "null reference";
        public const string CodecTypeError = "codec type error";
        public const string NotSupportFrame = "not supported frame";
        public const string LineSizeError = "line size error";
        public const string PtsOutOfRange = "pts out of range";
        public const string NotImplemented = "not implemented";
    }
}