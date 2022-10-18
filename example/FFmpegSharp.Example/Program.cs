using System;
using FFmpegSharp.Example.Other;

namespace FFmpegSharp.Example
{
    internal unsafe class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                new Video2Image().Execute();
                //new CreateMPEG4().Execute();
                //new AvioReading().Execute();
                //new EncodeAudio().Execute();
                //new EncodeVideo().Execute();
                //new DecodeAudio().Execute();
                //new DecodeVideo().Execute();
                //new DemuxingDecoding().Execute();
                //new Transcoding().Execute();
                //new Metadata().Execute();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }

            //foreach (var deviceInfoList in f.ListDevice())
            //{
            //    Console.WriteLine(deviceInfoList.DefaultDevice);
            //    foreach (var deviceInfo in deviceInfoList.Devices)
            //    {
            //        Console.WriteLine(deviceInfo.DeviceName);
            //        Console.WriteLine(deviceInfo.DeviceDescripton);
            //        foreach (var type in deviceInfo.MediaTypes)
            //        {
            //            Console.WriteLine($"{type}");
            //        }
            //    }
            //}


        }



    }

    public interface IExample
    {
        void Execute();
    }

    public abstract class ExampleBase : IExample
    {
        protected string[] args;

        public ExampleBase(params string[] args)
        {
            this.args = args ?? new string[0];
        }

        public abstract void Execute();
    }
}

