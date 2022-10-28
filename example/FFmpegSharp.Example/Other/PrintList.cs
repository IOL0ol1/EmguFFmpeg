using System;
using FFmpeg.AutoGen;

namespace FFmpegSharp.Example.Other
{
    internal class PrintList : ExampleBase
    {
        public PrintList() : base()
        {
            Index = -9;
            Enable = false;
        }

        public override void Execute()
        {
            var index = 1;
            Console.WriteLine("==============================in formats==============================");
            foreach (var item in InFormat.GetFormats())
            {
                Console.WriteLine($"{index++:D3}[{item.Name}]({item.LongName}){item.MimeType}");
            }
            index = 1;
            Console.WriteLine("==============================out formats==============================");
            foreach (var item in OutFormat.GetFormats())
            {
                Console.WriteLine($"{index++:D3}[{item.Name}]({item.LongName}){item.MimeType}");
            }
            index = 1;
            Console.WriteLine("==============================codecs==============================");
            foreach (var item in MediaCodec.GetCodecs())
            {
                Console.WriteLine($"{index++:D3}[{item.Name}]({item.LongName}){item.WrapperName}");
            }
            //index = 1;
            //Console.WriteLine("==============================parsers==============================");
            //foreach (var item in MediaCodecParserContext.GetParsers())
            //{
            //    Console.WriteLine($"{index++:D3}{string.Join(",",item.codec_ids.ToArray())}");
            //}
            index = 1;
            Console.WriteLine("==============================in video device==============================");
            foreach (var item in MediaDevice.GetInputVideoDevices())
            {
                Console.WriteLine($"{index++:D3}[{item.Name}]({item.LongName}){item.MimeType}");
            }
            index = 1;
            Console.WriteLine("==============================out video device==============================");
            foreach (var item in MediaDevice.GetOutputVideoDevices())
            {
                Console.WriteLine($"{index++:D3}[{item.Name}]({item.LongName}){item.MimeType}");
            }
            index = 1;
            Console.WriteLine("==============================in audio device==============================");
            foreach (var item in MediaDevice.GetInputAudioDevices())
            {
                Console.WriteLine($"{index++:D3}[{item.Name}]({item.LongName}){item.MimeType}");
            }
            index = 1;
            Console.WriteLine("==============================out audio device==============================");
            foreach (var item in MediaDevice.GetOutputAudioDevices())
            {
                Console.WriteLine($"{index++:D3}[{item.Name}]({item.LongName}){item.MimeType}");
            }
            index = 1;
            Console.WriteLine("==============================HW device types==============================");
            AVHWDeviceType hwDeviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE;
            while ((hwDeviceType = ffmpeg.av_hwdevice_iterate_types(hwDeviceType)) != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
            {
                Console.WriteLine($"{index++:D3}[{hwDeviceType}]");
            }
        }
    }
}
