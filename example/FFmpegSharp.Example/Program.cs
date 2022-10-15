using System;
using FFmpeg.AutoGen;
using FFmpegSharp.Internal;

namespace FFmpegSharp.Example
{
    internal unsafe class Program
    {
        private static void Main(string[] args)
        {
            //new CreateMPEG4().Execute();
            //new AvioReading().Execute();
            //new EncodeAudio().Execute();
            //new EncodeVideo().Execute();
            //new DecodeAudio().Execute();
            //new DecodeVideo().Execute();
            //new DemuxingDecoding().Execute();

            ffmpeg.avdevice_register_all();
            var o = new MediaDictionary { ["list_devices"] = "true" };

            var f = MediaDemuxer.Open("video=dummy",InFormat.FindFormat("dshow"),o,new MediaFormatContext());

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
            f.Dispose();
            o.Dispose();

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

