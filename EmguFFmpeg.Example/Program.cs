using System;

namespace EmguFFmpeg.Example
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            FFmpegHelper.RegisterBinaries();
            FFmpegHelper.SetupLogging();
            Console.WriteLine("Hello FFmpeg!");
            Console.ReadKey();
        }
    }
}