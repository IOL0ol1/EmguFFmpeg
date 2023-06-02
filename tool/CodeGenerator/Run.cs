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
        private static void Main(string[] args)
        {

            var types = new List<Info>
             {
                new Info{Type = typeof(AVCodec)},
                new Info{Type = typeof(AVCodecContext) },
                new Info{Type = typeof(AVFormatContext) },
                new Info{Type = typeof(AVStream) },
                new Info{Type = typeof(AVFrame)  },
                new Info{Type = typeof(AVPacket) },
                new Info{Type = typeof(AVInputFormat), Name = "InFormatBase" },
                new Info{Type = typeof(AVOutputFormat), Name = "OutFormatBase" },
                new Info{Type = typeof(AVFilter) },
                new Info{Type = typeof(AVFilterContext) },
                new Info{Type = typeof(AVFilterGraph) },
             };
            foreach (var type in types)
            {
                var g = CodeGenerator(type);
                File.WriteAllText($"../../../{g.OutTypeName}.cs", g.SourceCode, System.Text.Encoding.UTF8);
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

        public static GeneratorOutput CodeGenerator(Info info, string @namespace = "FFmpegSharp.Internal")
        {
            var type = info.Type;
            var dstTypeName = info.Name;
            var isDisposable = info.IsDisposable;
            using (var sw = new StringWriter())
            {
                var srcTypeName = type.Name.Replace("FFmpeg.AutoGen.", "");
                dstTypeName = dstTypeName == null ? $"{Regex.Replace(srcTypeName, @"^AV", "Media")}Base" : dstTypeName;
                var pTypeName = $"{Regex.Replace(srcTypeName, @"^AV", "p")}";
                if (isDisposable)
                    sw.WriteLine($"using System;");

                sw.WriteLine($"using FFmpeg.AutoGen;");
                sw.WriteLine($"namespace {@namespace}");
                sw.WriteLine(@"{");
                sw.WriteLine($"    public abstract unsafe partial class {dstTypeName}{(isDisposable ? " : IDisposable" : "")}");
                sw.WriteLine(@"    {");
                sw.WriteLine(@"        /// <summary>");
                sw.WriteLine(@"        /// Be careful!!!");
                sw.WriteLine(@"        /// </summary>");
                sw.WriteLine($"        protected internal {srcTypeName}* {pTypeName} = null;");
                sw.WriteLine(@"");
                sw.WriteLine(@"        /// <summary>");
                sw.WriteLine($"        /// const {srcTypeName}*");
                sw.WriteLine(@"        /// </summary>");
                sw.WriteLine(@"        /// <param name=""value""></param>");
                sw.WriteLine($"        public static implicit operator {srcTypeName}*({dstTypeName} value)");
                sw.WriteLine(@"        {");
                sw.WriteLine($"            if (value == null) return null;");
                sw.WriteLine($"            return value.{pTypeName};");
                sw.WriteLine(@"        }");
                sw.WriteLine(@"");
                sw.WriteLine($"        public {dstTypeName}({srcTypeName}* value)");
                sw.WriteLine(@"        {");
                sw.WriteLine($"            {pTypeName} = value;");
                sw.WriteLine(@"        }");
                sw.WriteLine(@"");
                sw.WriteLine($"        public {srcTypeName} Ref => *{pTypeName};");
                sw.WriteLine(@"");
                if (isDisposable)
                {
                    sw.WriteLine($"        public abstract void Dispose();");
                    sw.WriteLine(@"");
                }
                foreach (var element in type.GetMembers(BindingFlags.Public | BindingFlags.Instance))
                {
                    var srcTypeWithName = $"{element}";
                    if (element.CustomAttributes.Any(_ => _.AttributeType == typeof(ObsoleteAttribute))
                    || element.MemberType != MemberTypes.Field
                    || srcTypeWithName.Contains("*")
                    || srcTypeWithName.Contains("_func "))
                        continue;

                    var tmp = srcTypeWithName.Split(' ');
                    var srcName = element.Name;
                    var dstType = tmp[0]
                        .Replace("FFmpeg.AutoGen.", "")
                        .Replace("Void", "void")
                        .Replace("Byte", "byte")
                        .Replace("UInt16", "ushort")
                        .Replace("Int16", "short")
                        .Replace("UInt32", "uint")
                        .Replace("Int32", "int")
                        .Replace("UInt64", "ulong")
                        .Replace("Int64", "long")
                        .Replace("Single", "float")
                        .Replace("Double", "double");
                    var dstName = string.Join("", tmp[1].Split('_').Select(_ => $"{char.ToUpper(_[0])}{_.Substring(1)}"));
                    sw.WriteLine($"        public {dstType} {dstName}");
                    sw.WriteLine(@"        {");
                    sw.WriteLine($"            get => {pTypeName}->{srcName};");
                    sw.WriteLine($"            set => {pTypeName}->{srcName} = value;");
                    sw.WriteLine(@"        }");
                    sw.WriteLine("");
                }
                sw.WriteLine(@"    }");
                sw.WriteLine(@"}");
                return new GeneratorOutput { SourceCode = sw.ToString(), OutTypeName = dstTypeName };
            }
        }

    }
}
