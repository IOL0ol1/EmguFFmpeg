using EmguFFmpeg.Example.Opencv4SharpSpace;

using System;
using System.Diagnostics;

namespace EmguFFmpeg.Example
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            FFmpegHelper.RegisterBinaries();
            FFmpegHelper.SetupLogging(logWrite: _ => Trace.Write(_));
            Console.WriteLine("Hello FFmpeg!");

            var output = "output.mp4";
            new EncodeVideoByMat(output, 800, 600, 1);
            Process.Start(output);

            new DecodeVideoToMat(output, "images");
            new DecodeVideoWithCustomCodecScaledToMat(output, "images");

            Console.WriteLine("--------------");
            Console.ReadKey();
            Process.Start(Environment.CurrentDirectory);
        }
    }
}