using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FFmpeg.AutoGen;
using OpenCvSharp;

namespace FFmpeg.Sharp.Example
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
            using (var convert = new Swscale()) // pixel converter for YUV => RGB
            using (var convertDst = new MediaFrame())
            {
                MediaCodec codec = null;
                var videoStreamIndex = demuxer.FindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, ref codec); // find best video stream with codec.
                using (var videoDecoder = MediaDecoder.CreateDecoder(demuxer[videoStreamIndex].CodecparRef, _ => { _.Ref.thread_count = 10; }/* multi thread */ ))
                {
                    // pre-allocate dst frame once; Swscale.Convert auto-Resets on first call from frame metadata.
                    convertDst.Ref.width = videoDecoder.Ref.width;
                    convertDst.Ref.height = videoDecoder.Ref.height;
                    convertDst.Ref.format = (int)AVPixelFormat.AV_PIX_FMT_BGR24;
                    convertDst.AllocateBuffer();
                    foreach (var packet in demuxer.ReadPackets())
                    {
                        if (packet.Ref.stream_index == videoStreamIndex)
                        {
                            foreach (var decodeFrame in videoDecoder.DecodePacket(packet))
                            {
                                foreach (var outFrame in convert.Convert(decodeFrame, convertDst))
                                {
                                    // use OpenCV mat write to file(or Bitmap)
                                    using (var mat = new Mat(outFrame.Ref.height, outFrame.Ref.width, MatType.CV_8UC3))
                                    {
                                        var srcPtr = (IntPtr)outFrame.Ref.data[0];
                                        var srcLineSize = outFrame.Ref.linesize[0];
                                        var dstPtr = mat.Data; // Bitmap.Scan0
                                        var dstLineSize = (int)mat.Step(); // Bitmap.Stride
                                        var byteWidth = Math.Min(srcLineSize, dstLineSize);
                                        var height = Math.Min(outFrame.Ref.height, mat.Height);
                                        FFmpegUtil.CopyPlane(srcPtr, srcLineSize, dstPtr, dstLineSize, byteWidth, height);
                                        if (decodeFrame.Ref.pkt_dts >= 0)
                                            mat.SaveImage(Path.Combine(output, $"{demuxer[packet.Ref.stream_index].ToTimeSpan(decodeFrame.Ref.pkt_dts).TotalMilliseconds}ms.jpg"));
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
