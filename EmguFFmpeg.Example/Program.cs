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
            Process.Start(Environment.CurrentDirectory);
            FFmpeg.RegisterBinaries();
            FFmpeg.SetupLogging(logWrite: _ => Trace.Write(_));
            Console.WriteLine("Hello FFmpeg!");

            //MediaFilterGraph filterGraph = new MediaFilterGraph();
            //MediaFilter mediaFilter = new MediaFilter("setpts");
            //string opts = "0.5*PTS";
            //mediaFilter.Initialize(filterGraph, opts, null);
            //filterGraph.Initialize();

            //MediaFilterGraph.CreateMediaFilterGraph("[in] split [main][tmp]; [tmp] crop=iw:ih/2:0:0, vflip [flip]; [main][flip] overlay=0:H/2 [out]");
            Filter filter = new Filter(@"C:\Users\Admin\Videos\Desktop\input.mp4", @"output.mp4");
            //Filter filter = new Filter(@"C:\Users\Admin\Desktop\input.mp3", @"output.mp3");
            //Remuxing filter = new Remuxing(@"C:\Users\Admin\Videos\Desktop\input.mp4");
            //DecodeAudioToMat decodeAudio = new DecodeAudioToMat(@"C:\Users\Admin\Desktop\input.mp3");
            //BitmapConverter bitmap = new BitmapConverter();
            //EncodeVideoByMat video = new EncodeVideoByMat("output.mp4", 800, 600, 1);
            //DecodeVideoToImage videoToImage = new DecodeVideoToImage(@"C:\Users\Admin\Videos\Desktop\input.mp4", "image");

            Console.ReadKey();
        }
    }
}