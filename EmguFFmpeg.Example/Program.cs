using System;
using System.Diagnostics;

namespace EmguFFmpeg.Example
{
    internal class Program
    {
        private unsafe static void Main(string[] args)
        {
            FFmpegHelper.RegisterBinaries();
            FFmpegHelper.SetupLogging(logAction: _ => Trace.Write(_));
            Console.WriteLine("Hello FFmpeg!");

            Console.ReadKey();
        }
    }
}