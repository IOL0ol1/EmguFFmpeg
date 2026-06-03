using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using OpenCvSharp;

namespace FFmpeg.Sharp.Example.Other
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
                    if (decoder != null && decoder.Ref.codec_type == FFmpeg.AutoGen.AVMediaType.AVMEDIA_TYPE_VIDEO)
                    {
                        // pre-allocate dst frame once with target dims/format; Swscale.Convert auto-resets on first call from frame metadata.
                        if (f.Ref.width == 0)
                        {
                            f.Ref.width = decoder.Ref.width;
                            f.Ref.height = decoder.Ref.height;
                            f.Ref.format = (int)FFmpeg.AutoGen.AVPixelFormat.AV_PIX_FMT_BGR24;
                            f.AllocateBuffer();
                        }
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
