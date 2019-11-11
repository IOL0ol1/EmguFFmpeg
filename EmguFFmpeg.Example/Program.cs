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
            FFmpeg.RegisterBinaries();
            FFmpeg.SetupLogging(logWrite: _ => Trace.Write(_));
            Console.WriteLine("Hello FFmpeg!");


            DecodeAudioToMat decodeAudio = new DecodeAudioToMat(@"C:\Users\IOL0ol1\Desktop\input.mp3");
            BitmapConverter bitmap = new BitmapConverter();
            EncodeVideoByMat video = new EncodeVideoByMat("output.mp4", 800, 600, 30);
            //DecodeVideoToImage videoToImage = new DecodeVideoToImage(@"C:\Users\Admin\Videos\Desktop\input.mp4", "image");

            Process.Start(Environment.CurrentDirectory);
            Console.ReadKey();
        }
    }
}