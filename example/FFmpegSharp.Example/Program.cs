using System;
using System.Linq;

namespace FFmpegSharp.Example
{
    internal unsafe class Program
    {
        private static void Main(string[] args)
        {
            try
            {
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
                    .ForEach(_ => _.Execute());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
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

