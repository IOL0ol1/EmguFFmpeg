using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FFmpeg.AutoGen;

namespace FFmpegSharp.Example
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                Task.Run(() =>
                {
                    while (true)
                    {
                        GC.Collect();
                    }
                });
                //var dict = new MediaDictionary()
                //{
                //    ["texst"] = "12",
                //    ["listen"] = "2",
                //};
                //dict.Add("texst", "2334", AVDictWriteFlags.MultiKey);
                //var a = dict.Get("a").ToList();
                //var b = dict.Get("texst").ToList();
                //ffmpeg.avformat_network_init();
                //MediaIOContext.Open("http://localhost:10010", ffmpeg.AVIO_FLAG_WRITE, dict);

                typeof(Program).Assembly
                    .GetTypes()
                    .Where(_ => _.IsAssignableTo(typeof(ExampleBase)) && !_.IsAbstract)
                    .Select(_ => Activator.CreateInstance(_)).OfType<ExampleBase>()
                    .Where(_ => _.Enable)
                    .OrderBy(_ => _.Index).ToList()
                    .ForEach(_ =>
                    {
                        var name = _.GetType().Name;
                        var fColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"-------------------{name} start----------------------");
                        Console.ForegroundColor = fColor;
                        try
                        {
                            _.Execute();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message + ex.StackTrace);
                        }
                        var s = Stopwatch.StartNew();
                        var count = 10;
                        for (int i = 0; i < count; i++)
                        {
                            try
                            {
                                _.Execute();
                            }
                            catch (Exception)
                            { 
                            }
                        }
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"-------------------{name} end[{s.Elapsed.TotalMilliseconds / count}ms]----------------------");
                        Console.ForegroundColor = fColor;
                    });
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
        protected string[] args = new string[0];

        public ExampleBase(params string[] args)
        {
            this.args = args;
        }

        public int Index { get; protected set; }
        public bool Enable { get; protected set; } = true;

        public abstract void Execute();
    }
}

