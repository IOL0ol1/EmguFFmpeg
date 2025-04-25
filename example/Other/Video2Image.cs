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
            using (var convert = new Swscale())
            using (var f = new MediaFrame())
            {
                var decoders = mediaReader.Select(_ => MediaDecoder.CreateDecoder(_.CodecparRef, _ => _.Ref.thread_count = 10)).ToList();
                foreach (var inPacket in mediaReader.ReadPackets())
                {
                    var decoder = decoders[inPacket.Ref.stream_index];
                    if (decoder != null && decoder.Ref.codec_type ==  AVMediaType.AVMEDIA_TYPE_VIDEO)
                    {
                        convert.SetOpts(decoder.Ref.width, decoder.Ref.height,  AVPixelFormat.AV_PIX_FMT_BGR24);
                        foreach (var inFrame in decoder.DecodePacket(inPacket))
                        {
                            foreach (var outFrame in convert.Convert(inFrame, f))
                            {
                                using (var mat = new Mat(outFrame.Ref.height, outFrame.Ref.width, MatType.CV_8UC3))
                                {
                                    var srcLineSize = outFrame.Ref.linesize[0];
                                    var dstLineSize = (int)mat.Step();
                                    FFmpegUtil.CopyPlane((IntPtr)outFrame.Ref.data[0], srcLineSize,
                                        mat.Data, dstLineSize, Math.Min(srcLineSize, dstLineSize), mat.Height);
                                    if (inFrame.Ref.pkt_dts >= 0)
                                        mat.SaveImage(Path.Combine(output, $"{mediaReader[inPacket.Ref.stream_index].ToTimeSpan(inFrame.Ref.pkt_dts).TotalMilliseconds}ms.jpg"));
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
