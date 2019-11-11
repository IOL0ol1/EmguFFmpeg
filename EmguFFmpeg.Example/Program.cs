using Emgu.CV;
using System;
using System.Diagnostics;
using EmguFFmpeg.EmguCV;
using FFmpeg.AutoGen;

namespace EmguFFmpeg.Example
{
    internal class Program
    {
        private unsafe static void Main(string[] args)
        {
            FFmpegHelper.RegisterBinaries();
            FFmpegHelper.SetupLogging(logAction: _ => Trace.Write(_));
            Console.WriteLine("Hello FFmpeg!");

            AudioFrame audioFrame = new AudioFrame(AVSampleFormat.AV_SAMPLE_FMT_DBL, 3, 1024);
            BitmapConverter bitmap = new BitmapConverter();
            EncodeVideoByMat video = new EncodeVideoByMat("output.mp4", 800, 600, 30);
            //DecodeAudio decodeAudio = new DecodeAudio(@"C:\Users\Admin\Desktop\input.flac");
            //DecodeVideoToImage videoToImage = new DecodeVideoToImage(@"C:\Users\Admin\Videos\Desktop\input.mp4", "image");

            Process.Start(Environment.CurrentDirectory);
            Console.ReadKey();
        }
    }
}