using System;
using FFmpeg.AutoGen.Abstractions;
using OpenCvSharp;

namespace FFmpegSharp.OpenCvSharp4
{
    public unsafe static class Extensions
    { 
        public static MediaFrame ToFrame(this Mat mat)
        {
            throw new NotImplementedException();
        }

        public static Mat ToMat(this AVFrame frame)
        {
            throw new NotImplementedException();
        }
    }
}
