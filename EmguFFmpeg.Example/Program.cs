using EmguFFmpeg.Example.Example;
using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace EmguFFmpeg.Example
{
    internal class Program
    {
        private unsafe static void Main(string[] args)
        {
            // copy ffmpeg binarys to ./bin
            FFmpegHelper.RegisterBinaries();
            FFmpegHelper.SetupLogging(logAction: _ => Trace.Write(_));
            Console.WriteLine("Hello FFmpeg!");

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string input = Path.Combine(desktop, "input.flac");
            string output = Path.Combine(desktop, "output.mp3");
            AudioTranscode audioTranscode = new AudioTranscode(input, output);

            Trace.TraceInformation("-----------------------");
            return;

            // No media files provided
            new List<IExample>()
            {
                new DecodeAudio("input.mp3"),
                new CreateVideo("output.mp4"),
                new ReadDevice(),
                new Remuxing("input.mp3"),
                new RtmpPull("rtmp://127.0.0.0/live/stream"),
            }.ForEach(_ =>
            {
                try
                {
                    _.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            });

            Console.ReadKey();
        }
    }
}