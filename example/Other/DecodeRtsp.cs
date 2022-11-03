using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FFmpeg.AutoGen;
using OpenCvSharp;

namespace FFmpegSharp.Example
{
    internal class DecodeRtsp : ExampleBase
    {
        public DecodeRtsp() : base("your-rtsp-url") // eg. "rtsp://192.168.0.105:8554/mystream"
        {
            Index = -9999;
        }

        public unsafe override void Execute()
        {
            var rtspUrl = args[0];

            var output = Directory.CreateDirectory("DecodeRtsp").FullName;

            // rtsp settings
            using (var options = new MediaDictionary()
            {
                ["rtsp_transport"] = "tcp",
                ["max_delay"] = "5",
                ["fflags"] = "nobuffer",
                ["stimeout"] = "3000000",
            })
            using (var demuxer = MediaDemuxer.Open(rtspUrl, options: options))
            using (var convert = new PixelConverter()) // pixel converter for YUV => RGB
            {
                MediaCodec codec = null;
                var videoStreamIndex = demuxer.FindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, ref codec); // find best video stream with codec.
                using (var videoDecoder = MediaDecoder.CreateDecoder(demuxer[videoStreamIndex].CodecparRef, _ => { _.ThreadCount = 10; }/* multi thread */ ))
                {
                    convert.SetOpts(videoDecoder.Width, videoDecoder.Height, AVPixelFormat.AV_PIX_FMT_BGR24);
                    foreach (var packet in demuxer.ReadPackets())
                    {
                        if (packet.StreamIndex == videoStreamIndex)
                        {
                            foreach (var decodeFrame in videoDecoder.DecodePacket(packet))
                            {
                                foreach (var outFrame in convert.Convert(decodeFrame))
                                {
                                    // use OpenCV mat write to file(or Bitmap)
                                    using (var mat = new Mat(outFrame.Height, outFrame.Width, MatType.CV_8UC3))
                                    {
                                        var srcPtr = (IntPtr)outFrame.Ref.data[0];
                                        var srcLineSize = outFrame.Linesize[0];
                                        var dstPtr = mat.Data; // Bitmap.Scan0
                                        var dstLineSize = (int)mat.Step(); // Bitmap.Stride
                                        var byteWidth = Math.Min(srcLineSize, dstLineSize);
                                        var height = Math.Min(outFrame.Height, mat.Height);
                                        FFmpegUtil.CopyPlane(srcPtr, srcLineSize, dstPtr, dstLineSize, byteWidth, height);
                                        if (decodeFrame.PktDts >= 0)
                                            mat.SaveImage(Path.Combine(output, $"{demuxer[packet.StreamIndex].ToTimeSpan(decodeFrame.PktDts).TotalMilliseconds}ms.jpg"));
                                    }
                                }
                            }
                        }
                    }


                }
            }




        }
    }
}
