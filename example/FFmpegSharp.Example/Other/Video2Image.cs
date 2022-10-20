using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using FFmpegSharp.Utilities;

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
            using (var srcPacket = new MediaPacket())
            using (var srcFrame = new MediaFrame())
            using (var swFrame = new MediaFrame())
            using (var convert = new PixelConverter())
            {
                var decoders = mediaReader.Select(_ => MediaDecoder.CreateDecoder(_.CodecparRef, _ => _.ThreadCount = 10)).ToList();
                MediaFrame dstFrame = null;
                foreach (var inPacket in mediaReader.ReadPackets(srcPacket))
                {
                    var decoder = decoders[inPacket.StreamIndex];
                    if (decoder != null)
                    {
                        dstFrame = dstFrame == null ? MediaFrame.CreateVideoFrame(decoder.Width, decoder.Height, FFmpeg.AutoGen.AVPixelFormat.AV_PIX_FMT_BGR24) : dstFrame;
                        foreach (var inFrame in decoder.DecodePacket(inPacket, srcFrame, swFrame))
                        {
                            if (decoder.CodecType == FFmpeg.AutoGen.AVMediaType.AVMEDIA_TYPE_VIDEO)
                            {
                                foreach (var outFrame in convert.Convert(inFrame, dstFrame))
                                {
                                    using (var bitmap = new Bitmap(outFrame.Width, outFrame.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                                    {
                                        var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);
                                        var srcLineSize = outFrame.Linesize[0];
                                        var dstLineSize = bitmapData.Stride;
                                        FFmpegUtil.CopyPlane((IntPtr)outFrame.Ref.data[0], srcLineSize,
                                            bitmapData.Scan0, bitmapData.Stride, Math.Min(srcLineSize, dstLineSize), bitmap.Height);
                                        bitmap.UnlockBits(bitmapData);
                                        if (inPacket.Pts > 0)
                                            bitmap.Save(Path.Combine(output, $"{mediaReader[inPacket.StreamIndex].ToTimeSpan(inPacket.Pts).TotalMilliseconds}ms.jpg"));
                                    }
                                }
                            }
                        }
                    }
                }
                dstFrame?.Dispose();
                decoders.ForEach(_ => _?.Dispose());
            }
            Console.WriteLine($"{s.Elapsed.TotalMilliseconds}ms");
        }
    }
}
