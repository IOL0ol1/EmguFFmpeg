
using Emgu.CV;

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

            new EnumerableInfo(_ => Console.WriteLine(_));

            //new S64Audio();

            var output = "output.mp4";
            //new FillYuv420PSample(output, 800, 600, 30);
            new EncodeVideoByMat(output, 800, 600, 1);
            //Process.Start(output);

            //new DecodeVideoToMat(output, "images");
            //new DecodeVideoWithCustomCodecScaledToMat(output, "images");

            Console.WriteLine("--------------");
            Console.ReadKey();
            Process.Start(Environment.CurrentDirectory);
        }
    }
}