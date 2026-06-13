using System;
using System.IO;

using FFmpeg.AutoGen;
using OpenCvSharp;

namespace FFmpeg.Sharp.Example.Other
{
    internal class DecodeRtsp : ExampleBase
    {
        public DecodeRtsp() : base("your-rtsp-url") // eg. "rtsp://192.168.0.105:8554/mystream"
        {
            Index = 33;
            Enable = false;
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
                using (var videoDecoder = new MediaDecoder(codec))
                {
                    videoDecoder.SetCodecParameters(ref demuxer[videoStreamIndex].CodecparRef);
                    videoDecoder.Ref.thread_count = 0; // multi thread
                    videoDecoder.Open();
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
                                convert.Convert(decodeFrame, convertDst);
                                // use OpenCV mat write to file(or Bitmap)
                                using (var mat = new Mat(convertDst.Ref.height, convertDst.Ref.width, MatType.CV_8UC3))
                                {
                                    var srcPtr = (IntPtr)convertDst.Ref.data[0];
                                    var srcLineSize = convertDst.Ref.linesize[0];
                                    var dstPtr = mat.Data; // Bitmap.Scan0
                                    var dstLineSize = (int)mat.Step(); // Bitmap.Stride
                                    var byteWidth = Math.Min(srcLineSize, dstLineSize);
                                    var height = Math.Min(convertDst.Ref.height, mat.Height);
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
