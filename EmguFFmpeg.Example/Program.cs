using Emgu.CV;
using System;
using System.Diagnostics;
using EmguFFmpeg.EmguCV;
using FFmpeg.AutoGen;
using System.Runtime.InteropServices;

namespace EmguFFmpeg.Example
{
    internal class Program
    {
        private unsafe static void Main(string[] args)
        {
            Process.Start(Environment.CurrentDirectory);
            FFmpeg.RegisterBinaries();
            FFmpeg.SetupLogging(logWrite: _ => Trace.Write(_));
            Console.WriteLine("Hello FFmpeg!");

            //Filter filter = new Filter(@"C:\Users\Admin\Videos\Desktop\input.mp4", @"output.mp4");
            //Filter filter = new Filter(@"C:\Users\Admin\Desktop\input.mp3", @"output.mp3");
            //Remuxing filter = new Remuxing(@"C:\Users\Admin\Videos\Desktop\input.mp4");
            //DecodeAudioToMat decodeAudio = new DecodeAudioToMat(@"C:\Users\Admin\Desktop\input.mp3");
            //BitmapConverter bitmap = new BitmapConverter();
            //EncodeAudioByMat encodeAudio = new EncodeAudioByMat("output.mp3");
            //EncodeVideoByMat video = new EncodeVideoByMat("output.mp4", 800, 600, 1);
            //DecodeVideoToMat videoToImage = new DecodeVideoToMat(@"C:\Users\Admin\Videos\Desktop\input.mp4", "image");
            //DecodeVideoToMat videoToImage = new DecodeVideoToMat(@"C:\Users\Admin\Desktop\out9.webm", "image");

            Console.ReadKey();
        }
    }
}