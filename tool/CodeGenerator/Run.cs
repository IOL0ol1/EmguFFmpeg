using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FFmpeg.AutoGen;

namespace CodeGenerator
{
    internal unsafe class Program
    {
        private static void Main(string[] _)
        {

            var types = new List<Info>
             {
                new (){Type = typeof(AVCodec)},
                new (){Type = typeof(AVCodecContext) },
                new (){Type = typeof(AVFormatContext) },
                new (){Type = typeof(AVStream) },
                new (){Type = typeof(AVFrame)  },
                new (){Type = typeof(AVPacket) },
                new (){Type = typeof(AVInputFormat) },
                new (){Type = typeof(AVOutputFormat) },
                new (){Type = typeof(AVFilter) },
                new (){Type = typeof(AVFilterContext) },
                new (){Type = typeof(AVFilterGraph) },
             };
            var folder = Directory.CreateDirectory("Internal").FullName;
            foreach (var type in types)
            {
                var g = CodeGenerator(type);
                var f = Path.Combine(folder, $"{g.OutTypeName}.cs");
                File.WriteAllText(f, g.SourceCode, System.Text.Encoding.UTF8);
            }

        }

        public class Info
        {
            public Type Type { get; set; }

            public string Name { get; set; }

            public bool IsDisposable { get; set; }
        }

        public class GeneratorOutput
        {
            public string SourceCode { get; set; }
            public string OutTypeName { get; set; }
        }

        public static GeneratorOutput CodeGenerator(Info info, string @namespace = "FFmpeg.Sharp")
        {
            var type = info.Type;
            var dstTypeName = info.Name;
            //var isDisposable = info.IsDisposable;
            using var sw = new StringWriter();
            var srcTypeName = type.Name.Replace("FFmpeg.AutoGen.", "");
            dstTypeName ??= $"{Regex.Replace(srcTypeName, @"^AV", "Media")}";
            var pTypeName = $"{Regex.Replace(srcTypeName, @"^AV", "p")}";

            sw.WriteLine($"using System;");
            sw.WriteLine($"using FFmpeg.AutoGen;");
            sw.WriteLine(@"");
            sw.WriteLine($"namespace {@namespace}");
            sw.WriteLine(@"{");
            sw.WriteLine($"    public unsafe partial class {dstTypeName}");
            sw.WriteLine(@"    {");
            sw.WriteLine(@"        /// <summary>");
            sw.WriteLine(@"        /// Be careful!!!");
            sw.WriteLine(@"        /// </summary>");
            sw.WriteLine($"        protected {srcTypeName}* {pTypeName} = null;");
            sw.WriteLine(@"");
            sw.WriteLine(@"        /// <summary>");
            sw.WriteLine($"        /// const {srcTypeName}*");
            sw.WriteLine(@"        /// </summary>");
            sw.WriteLine(@"        /// <param name=""value""></param>");
            sw.WriteLine($"        public static implicit operator {srcTypeName}*({dstTypeName} value)");
            sw.WriteLine(@"        {");
            sw.WriteLine($"            return value == null ? null : value.{pTypeName};");
            sw.WriteLine(@"        }");
            sw.WriteLine(@"");
            sw.WriteLine($"        public {dstTypeName}({srcTypeName}* p{srcTypeName})");
            sw.WriteLine(@"        {");
            sw.WriteLine($"            {pTypeName} = p{srcTypeName};");
            sw.WriteLine(@"        }");
            sw.WriteLine(@"");
            sw.WriteLine($"        public {dstTypeName}(IntPtr p{srcTypeName})");
            sw.WriteLine($"            : this(({srcTypeName}*)p{srcTypeName})");
            sw.WriteLine(@"        { }");
            sw.WriteLine(@"");
            sw.WriteLine(@"        /// <summary>");
            sw.WriteLine($"        /// Direct access to the underlying struct fields.");
            sw.WriteLine($"        /// WARNING: Be careful when modifying. Tracks {srcTypeName} struct evolution upstream.");
            sw.WriteLine(@"        /// </summary>");
            sw.WriteLine($"        public ref {srcTypeName} Ref => ref *{pTypeName};");
            sw.WriteLine(@"");
            sw.WriteLine(@"    }");
            sw.WriteLine(@"}");
            return new GeneratorOutput { SourceCode = sw.ToString(), OutTypeName = dstTypeName };
        }

    }
}
