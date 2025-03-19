using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FFmpeg.AutoGen.Abstractions;
using OpenCvSharp;

namespace FFmpegSharp.Example.Other
{
    internal class Video2Image : ExampleBase
    {
        public Video2Image() : this($"video-input.mp4", $"{nameof(Video2Image)}-output")
        {

        }

        public Video2Image(params string[] args) : base(args)
        {
        }

        public unsafe override void Execute()
        {
            var input = args[0];
            var output = Directory.CreateDirectory(args[1]).FullName;
            var s = Stopwatch.StartNew();
            using (var mediaReader = MediaDemuxer.Open(File.OpenRead(input)))
            using (var convert = new PixelConverter())
            using (var f = new MediaFrame())
            {
                var decoders = mediaReader.Select(_ => MediaDecoder.CreateDecoder(_.CodecparRef, _ => _.ThreadCount = 10)).ToList();
                foreach (var inPacket in mediaReader.ReadPackets())
                {
                    var decoder = decoders[inPacket.StreamIndex];
                    if (decoder != null && decoder.CodecType ==  AVMediaType.AVMEDIA_TYPE_VIDEO)
                    {
                        convert.SetOpts(decoder.Width, decoder.Height,  AVPixelFormat.AV_PIX_FMT_BGR24);
                        foreach (var inFrame in decoder.DecodePacket(inPacket))
                        {
                            foreach (var outFrame in convert.Convert(inFrame, f))
                            {
                                using (var mat = new Mat(outFrame.Height, outFrame.Width, MatType.CV_8UC3))
                                {
                                    var srcLineSize = outFrame.Linesize[0];
                                    var dstLineSize = (int)mat.Step();
                                    FFmpegUtil.CopyPlane((IntPtr)outFrame.Const.data[0], srcLineSize,
                                        mat.Data, dstLineSize, Math.Min(srcLineSize, dstLineSize), mat.Height);
                                    if (inFrame.PktDts >= 0)
                                        mat.SaveImage(Path.Combine(output, $"{mediaReader[inPacket.StreamIndex].ToTimeSpan(inFrame.PktDts).TotalMilliseconds}ms.jpg"));
                                }
                            }
                        }
                    }
                }
                decoders.ForEach(_ => _?.Dispose());
            }
            Console.WriteLine($"{s.Elapsed.TotalMilliseconds}ms");
        }
    }
}
