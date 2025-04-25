using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using FFmpeg.AutoGen.Abstractions;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;

namespace FFmpegSharp.Example
{
    internal class Program
    {
        private static unsafe void Main(string[] args)
        {
            try
            {
                DynamicallyLoadedBindings.LibrariesPath = Path.Combine(AppContext.BaseDirectory, "runtimes\\win-x64\\native");
                DynamicallyLoadedBindings.Initialize();
                //Task.Run(() =>
                //{
                //    while (true)
                //    {
                //        GC.Collect();
                //    }
                //}); 
                //{
                //    ["texst"] = "12",
                //    ["listen"] = "2",
                //};
                //dict.Add("texst", "2334", AVDictWriteFlags.MultiKey);
                //var a = dict.Get("a").ToList();
                //var b = dict.Get("texst").ToList();
                //ffmpeg.avformat_network_init();
                //MediaIOContext.Open("http://localhost:10010", ffmpeg.AVIO_FLAG_WRITE, dict);



                foreach (var item in MediaCodec.GetCodecs())
                {
                    Console.WriteLine(item);
                }


                var v = ffmpeg.avdevice_version();
                ffmpeg.avdevice_register_all();
                MediaDevice.ListInputSources(MediaInputFormat.GetFormats().First(), x =>
                {
                    Console.WriteLine("-----------------");
                    for (int i = 0; i < x.nb_devices; i++)
                    {
                        Console.WriteLine(((IntPtr)x.devices[i]->device_description).PtrToStringUTF8());
                        Console.WriteLine(((IntPtr)x.devices[i]->device_name).PtrToStringUTF8());
                        for (int j = 0; j < x.devices[i]->nb_media_types; j++)
                        {
                            Console.WriteLine(JsonSerializer.Serialize(x.devices[i]->media_types[j]));
                        }
                    }
                });
                //typeof(Program).Assembly
                //    .GetTypes()
                //    .Where(_ => _.IsAssignableTo(typeof(ExampleBase)) && !_.IsAbstract)
                //    .Select(_ => Activator.CreateInstance(_)).OfType<ExampleBase>()
                //    .Where(_ => _.Enable)
                //    .OrderBy(_ => _.Index).ToList()
                //    .ForEach(_ =>
                //    {
                //        var name = _.GetType().Name;
                //        var fColor = Console.ForegroundColor;
                //        Console.ForegroundColor = ConsoleColor.Red;
                //        Console.WriteLine($"-------------------{name} start----------------------");
                //        Console.ForegroundColor = fColor;
                //        try
                //        {
                //            _.Execute();
                //            //return;
                //        }
                //        catch (Exception ex)
                //        {
                //            Console.WriteLine(ex.Message + ex.StackTrace);
                //        }
                //        //var s = Stopwatch.StartNew();
                //        //var count = 2;
                //        //for (int i = 0; i < count; i++)
                //        //{
                //        //    try
                //        //    {
                //        //        _.Execute();
                //        //    }
                //        //    catch (Exception)
                //        //    { 
                //        //    }
                //        //}
                //        Console.ForegroundColor = ConsoleColor.Red;
                //        //Console.WriteLine($"-------------------{name} end[{s.Elapsed.TotalMilliseconds / count}ms]----------------------");
                //        Console.ForegroundColor = fColor;
                //    });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
#if RELEASE
            Console.WriteLine("Pause 'Enter' to exit");
            Console.ReadLine();
#endif
        }
    }

    public abstract class ExampleBase
    {
        protected string[] args = [];

        public ExampleBase(params string[] args)
        {
            this.args = args;
        }

        public int Index { get; protected set; }
        public bool Enable { get; protected set; } = true;

        public abstract void Execute();
    }
}

