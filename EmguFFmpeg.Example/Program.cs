using Emgu.CV;
using System;
using System.Diagnostics;
using EmguFFmpeg.EmguCV;
using FFmpeg.AutoGen;
using System.Runtime.InteropServices;
using System.IO;

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

            Console.WriteLine("--------------");
            Console.ReadKey();
            Process.Start(Environment.CurrentDirectory);
        }
    }
}