using FFmpeg.AutoGen;

using System;

namespace FFmpegManaged
{
    public class FFmpegException : Exception
    {
        public int ErrorCode { get; }

        public FFmpegException(int errorCode) : base($"ffmpeg error [{errorCode}] {GetErrorString(errorCode)}")
        {
            ErrorCode = errorCode;
        }

        public FFmpegException(int errorCode, string message) : base($"ffmpeg error [{errorCode}] {GetErrorString(errorCode)} {message}")
        {
            ErrorCode = errorCode;
        }

        public FFmpegException(string message) : base($"ffmpeg error {message}")
        {
        }

        public static unsafe string GetErrorString(int errorCode)
        {
            byte* buffer = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE];
            ffmpeg.av_strerror(errorCode, buffer, ffmpeg.AV_ERROR_MAX_STRING_SIZE);
            return ((IntPtr)buffer).PtrToStringUTF8();
        }
    }
}