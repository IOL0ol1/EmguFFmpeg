using System;
using System.Diagnostics;
using System.IO;
using FFmpeg.AutoGen;
using OpenCvSharp;

namespace FFmpeg.Sharp.Example
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
            using (var convert = new Swscale())
            using (var convertDst = new MediaFrame())
            {
                MediaCodec decoder = null;
                var video_stream = demuxer.FindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, ref decoder);
                var vDecoder = MediaDecoder.CreateDecoder(demuxer[video_stream].CodecparRef, _ =>
                {
                    _.Ref.thread_count = 10;
                    _.InitHWDeviceContext(deviceType);
                });
                // pre-allocate dst frame; Swscale.Convert auto-Resets on first call from frame metadata.
                convertDst.Ref.width = vDecoder.Ref.width;
                convertDst.Ref.height = vDecoder.Ref.height;
                convertDst.Ref.format = (int)AVPixelFormat.AV_PIX_FMT_BGR24;
                convertDst.AllocateBuffer();
                foreach (var p in demuxer.ReadPackets(packet))
                {
                    if (p.Ref.stream_index == video_stream)
                    {
                        foreach (var inFrame in vDecoder.DecodePacket(p, frame, sw_frame))
                        {
                            Write(output_file, inFrame);
                            foreach (var outFrame in convert.Convert(inFrame, convertDst))
                            {
                                using (var mat = new Mat(outFrame.Ref.height, outFrame.Ref.width, MatType.CV_8UC3))
                                {
                                    var srcLineSize = outFrame.Ref.linesize[0];
                                    var dstLineSize = (int)mat.Step();
                                    FFmpegUtil.CopyPlane((IntPtr)outFrame.Ref.data[0], srcLineSize,
                                        mat.Data, dstLineSize, Math.Min(srcLineSize, dstLineSize), mat.Height);
                                    if (inFrame.Ref.pkt_dts >= 0)
                                    {
                                        var outputFolder = Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(inputFile), "HWDecode")).FullName;
                                        mat.SaveImage(Path.Combine(outputFolder, $"{demuxer[video_stream].ToTimeSpan(inFrame.Ref.pkt_dts).TotalMilliseconds}ms.jpg"));
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
            var size = ffmpeg.av_image_get_buffer_size((AVPixelFormat)f.Ref.format, f.Ref.width, f.Ref.height, 1);
            var buffer = (byte*)ffmpeg.av_malloc((ulong)size);
            var srcData = new byte_ptrArray4();
            srcData.UpdateFrom(f.Ref.data);
            var srcLinesize = new int_array4();
            srcLinesize.UpdateFrom(f.Ref.linesize);
            var ret = ffmpeg.av_image_copy_to_buffer(buffer, size, srcData, srcLinesize, (AVPixelFormat)f.Ref.format, f.Ref.width, f.Ref.height, 1);
            stream.Write(new System.ReadOnlySpan<byte>(buffer, ret));
        }
    }
}
