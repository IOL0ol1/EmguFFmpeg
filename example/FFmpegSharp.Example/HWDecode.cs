using System;
using System.Diagnostics;
using System.IO;
using FFmpeg.AutoGen;
using OpenCvSharp;

namespace FFmpegSharp.Example
{
    internal unsafe class HWDecode : ExampleBase
    {
        public HWDecode() : base("d3d11va", "video-input.mp4", "HWDecode-output.bin")
        {
             
        }

        public override void Execute()
        {
            var deviceType = args[0];
            var inputFile = args[1];
            var outputFile = args[2];

            using (var demuxer = MediaDemuxer.Open(File.OpenRead(inputFile)))
            using (var output_file = File.OpenWrite(outputFile))
            using (var packet = new MediaPacket())
            using (var frame = new MediaFrame())
            using (var sw_frame = new MediaFrame())
            using (var convert = new PixelConverter())
            {
                MediaCodec decoder = null;
                var video_stream = demuxer.FindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, ref decoder);
                var vDecoder = MediaDecoder.CreateDecoder(demuxer[video_stream].CodecparRef, _ =>
                {
                    _.ThreadCount = 10;
                    _.InitHWDeviceContext(deviceType);
                });
                convert.SetOpts(vDecoder.Width, vDecoder.Height, AVPixelFormat.AV_PIX_FMT_BGR24);
                foreach (var p in demuxer.ReadPackets(packet))
                {
                    if (p.StreamIndex == video_stream)
                    { 
                        foreach (var inFrame in vDecoder.DecodePacket(p, frame, sw_frame))
                        {
                            Write(output_file, inFrame);
                            foreach (var outFrame in convert.Convert(inFrame))
                            {
                                using (var mat = new Mat(outFrame.Height, outFrame.Width, MatType.CV_8UC3))
                                {
                                    var srcLineSize = outFrame.Linesize[0];
                                    var dstLineSize = (int)mat.Step();
                                    FFmpegUtil.CopyPlane((IntPtr)outFrame.Ref.data[0], srcLineSize,
                                        mat.Data, dstLineSize, Math.Min(srcLineSize, dstLineSize), mat.Height);
                                    if (inFrame.PktDts >= 0)
                                    {
                                        var outputFolder = Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(inputFile), "HWDecode")).FullName;
                                        mat.SaveImage(Path.Combine(outputFolder, $"{demuxer[video_stream].ToTimeSpan(inFrame.PktDts).TotalMilliseconds}ms.jpg"));
                                    }
                                }
                            }
                        }
                    }
                }
                /* flush the decoder */
                foreach (var f in vDecoder.DecodePacket(null, frame, sw_frame))
                {
                    Write(output_file, f);
                }
            }
        }

        private static unsafe void Write(Stream stream, MediaFrame f)
        {
            var size = ffmpeg.av_image_get_buffer_size((AVPixelFormat)f.Format, f.Width, f.Height, 1);
            var buffer = (byte*)ffmpeg.av_malloc((ulong)size);
            var srcData = new byte_ptrArray4();
            srcData.UpdateFrom(f.Data);
            var srcLinesize = new int_array4();
            srcLinesize.UpdateFrom(f.Linesize);
            var ret = ffmpeg.av_image_copy_to_buffer(buffer, size, srcData, srcLinesize, (AVPixelFormat)f.Format, f.Width, f.Height, 1);
            stream.Write(new System.ReadOnlySpan<byte>(buffer, ret));
        }
    }
}
