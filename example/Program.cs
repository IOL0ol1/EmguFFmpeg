using System;
using System.IO;
using System.Linq;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            ffmpeg.RootPath = Path.Combine(AppContext.BaseDirectory, "runtimes\\win-x64\\native");
            try
            {
                typeof(Program).Assembly
                    .GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(ExampleBase)) && !t.IsAbstract)
                    .Select(t => (ExampleBase)Activator.CreateInstance(t))
                    .Where(e => e.Enable)
                    .OrderBy(e => e.Index)
                    .ToList()
                    .ForEach(e =>
                    {
                        var name = e.GetType().Name;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"=== {name} ===");
                        Console.ResetColor();
                        try { e.Execute(); }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[{name}] FAILED: {ex.Message}");
                            Console.ResetColor();
                        }
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
#if RELEASE
            Console.WriteLine("Press Enter to exit");
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

