using System;
using System.IO;
using FFmpeg.AutoGen;
using OpenCvSharp;

namespace FFmpeg.Sharp.Example.Legacy
{
    internal unsafe class HWDecode : ExampleBase
    {
        public HWDecode() : base("d3d11va", "video-input.mp4", "HWDecode-output.bin")
        { Index = 46; Enable = false; }

        public override void Execute()
        {
            var deviceType = args[0];
            var inputFile = args[1];
            var outputFile = args[2];

            using (var demuxer = MediaDemuxer.Open(inputFile))
            using (var output_file = File.OpenWrite(outputFile))
            using (var packet = new MediaPacket())
            using (var frame = new MediaFrame())
            using (var sw_frame = new MediaFrame())
            using (var convert = new Swscale())
            using (var convertDst = new MediaFrame())
            {
                MediaCodec decoder = null;
                var video_stream = demuxer.FindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, ref decoder);
                using (var vDecoder = new MediaDecoder(decoder))
                {
                    vDecoder.SetCodecParameters(ref demuxer[video_stream].CodecparRef);
                    vDecoder.Ref.thread_count = 0;
                    vDecoder.InitHWDeviceContext(deviceType);
                    vDecoder.Open();

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
                                convert.Convert(inFrame, convertDst);
                                using (var mat = new Mat(convertDst.Ref.height, convertDst.Ref.width, MatType.CV_8UC3))
                                {
                                    var srcLineSize = convertDst.Ref.linesize[0];
                                    var dstLineSize = (int)mat.Step();
                                    FFmpegUtil.CopyPlane((IntPtr)convertDst.Ref.data[0], srcLineSize,
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
                    /* flush the decoder */
                    foreach (var f in vDecoder.DecodePacket(null, frame, sw_frame))
                    {
                        Write(output_file, f);
                    }
                }
            }
        }

        // Write a raw image plane using the zero-allocation Span overload on MediaFrame.
        private static unsafe void Write(Stream stream, MediaFrame f)
        {
            int size = f.GetBytesSize(padding: false);
            // For modest image sizes stackalloc is fine; for HD+ rent from ArrayPool.
            const int stackBudget = 256 * 1024;
            if (size <= stackBudget)
            {
                Span<byte> buf = stackalloc byte[size];
                int written = f.GetBytes(buf, padding: false);
                stream.Write(buf.Slice(0, written));
            }
            else
            {
                var rented = System.Buffers.ArrayPool<byte>.Shared.Rent(size);
                try
                {
                    int written = f.GetBytes(rented.AsSpan(0, size), padding: false);
                    stream.Write(rented, 0, written);
                }
                finally
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }
    }
}
