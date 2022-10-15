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

            var types = new List<(Type,string)>
             {
                 (typeof(AVCodec),        null),
                 (typeof(AVCodecContext), null),
                 (typeof(AVFormatContext),null),
                 (typeof(AVStream),       null),
                 (typeof(AVFrame),        null),
                 (typeof(AVPacket),       null),
                 (typeof(AVInputFormat),  "InFormatBase"),
                 (typeof(AVOutputFormat), "OutFormatBase"),
             };
            foreach (var type in types)
            {
                var g = CodeGenerator(type.Item1,dstTypeName:type.Item2);
                File.WriteAllText($"../../../{g.OutTypeName}.cs", g.SourceCode);
            }

        }

        public class GeneratorOutput
        {
            public string SourceCode { get; set; }
            public string OutTypeName { get; set; }
        }

        public static GeneratorOutput CodeGenerator(Type type, string @namespace = "FFmpegSharp.Internal", string dstTypeName = null)
        {
            using (var sw = new StringWriter())
            {
                var srcTypeName = type.Name.Replace("FFmpeg.AutoGen.", "");
                dstTypeName = dstTypeName == null ? $"{Regex.Replace(srcTypeName, @"^AV", "Media")}Base" : dstTypeName;
                var pTypeName = $"{Regex.Replace(srcTypeName, @"^AV", "p")}";
                sw.WriteLine($"using FFmpeg.AutoGen;");
                sw.WriteLine($"namespace {@namespace}");
                sw.WriteLine(@"{");
                sw.WriteLine($"    public abstract unsafe class {dstTypeName}");
                sw.WriteLine(@"    {");
                sw.WriteLine($"        protected {srcTypeName}* {pTypeName} = null;");
                sw.WriteLine(@"");
                sw.WriteLine($"        public static implicit operator {srcTypeName}*({dstTypeName} value)");
                sw.WriteLine(@"        {");
                sw.WriteLine($"            if(value == null) return null;");
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
                    sw.WriteLine($"            get=> {pTypeName}->{srcName};");
                    sw.WriteLine($"            set=> {pTypeName}->{srcName} = value;");
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
