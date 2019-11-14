using FFmpeg.AutoGen;

using System;
using System.Runtime.Serialization;

namespace EmguFFmpeg
{
    [Serializable]
    public class FFmpegException : Exception
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

        public static unsafe string GetErrorString(int errorCode)
        {
            byte* buffer = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE];
            ffmpeg.av_strerror(errorCode, buffer, ffmpeg.AV_ERROR_MAX_STRING_SIZE);
            return ((IntPtr)buffer).PtrToStringUTF8();
        }

        protected FFmpegException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        { }

        public const string FFmpegError = "FFmpeg error";
        public const string NotSupportCodecId = "not supported codec id";
        public const string NotSupportSampleRate = "not supported sample rate";
        public const string NotSupportFormat = "not supported format";
        public const string NotSupportChLayout = "not supported channle layout";
        public const string NotSupportFrame = "not supported frame";
        public const string NonNegative = "argument must be non-negative";
        public const string NotImplemented = "not implemented";
        public const string NullReference = "null reference";
        public const string CodecTypeError = "codec type error";
        public const string MediaTypeError = "media type error";
        public const string LineSizeError = "line size error";
        public const string PtsOutOfRange = "pts out of range";
        public const string InvalidVideoFrame = "invalid video frame";
        public const string InvalidAudioFrame = "invalid audio frame";
        public const string InvalidFrame = "invalid frame";
        public const string NotInitCodecContext = "not init codec context";
        public const string TooManyChannels = "too many channels";
        public const string FilterHasInit = "filter has init";
    }
}