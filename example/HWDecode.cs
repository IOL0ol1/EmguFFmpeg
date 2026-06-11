using System;
using System.IO;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: hw_decode.c
    /// Decode a video file using a hardware accelerator (default: d3d11va on Windows).
    /// Decoded frames are transferred from GPU memory to CPU memory and written raw to disk.
    /// </summary>
    public class HwDecode : ExampleBase
    {
        public HwDecode() { Index = 11; Enable = false; }

        public override void Execute()
        {
            var hwTypeName = args.Length > 0 ? args[0] : "d3d11va";
            var inFile     = args.Length > 1 ? args[1] : "input.mp4";
            var outFile    = args.Length > 2 ? args[2] : "out_hw.raw";

            // Resolve device type name.
            var hwType = MediaHWDevice.FindTypeByName(hwTypeName);
            if (hwType == AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
            {
                Console.Error.WriteLine($"Device type '{hwTypeName}' not found. Available types:");
                foreach (var t in MediaHWDevice.GetTypes())
                    Console.Error.Write($" {t.GetName()}");
                Console.Error.WriteLine();
                return;
            }

            // Open input.
            using var demuxer = MediaDemuxer.Open(inFile);
            demuxer.DumpFormat();

            MediaCodec videoCodec = null;
            int videoStreamIdx = demuxer.FindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, ref videoCodec);
            if (videoStreamIdx < 0) throw new Exception("No video stream found");

            // InitHWDeviceContext automatically:
            //   - walks the codec's HW configs for the requested device type
            //   - creates the AVBufferRef hw_device_ctx
            //   - wires the get_format callback to return the correct HW pixel format
            using var decoder = MediaDecoder.CreateDecoder(
                demuxer[videoStreamIdx].CodecparRef,
                ctx =>
                {
                    int method = ctx.InitHWDeviceContext(hwType);
                    if (method == 0)
                        throw new Exception($"Codec {videoCodec.Name} does not support HW device '{hwTypeName}'");
                    Console.WriteLine($"HW acceleration: {hwTypeName} (method flags: 0x{method:x})");
                });

            Console.WriteLine($"Writing raw video to '{outFile}'");
            using var outStream = File.OpenWrite(outFile);
            using var frame   = new MediaFrame();
            using var swFrame = new MediaFrame();  // receives GPU→CPU transfer
            using var packet  = new MediaPacket();
            int frameCount = 0;

            // DecodePacket(pkt, frame, swFrame) automatically transfers HW frames to swFrame.
            foreach (var pkt in demuxer.ReadPackets(packet))
            {
                if (pkt.Ref.stream_index != videoStreamIdx) continue;
                foreach (var decoded in decoder.DecodePacket(pkt, frame, swFrame))
                {
                    WriteFrame(decoded, outStream);
                    frameCount++;
                }
            }
            // Flush decoder.
            foreach (var decoded in decoder.DecodePacket(null, frame, swFrame))
            {
                WriteFrame(decoded, outStream);
                frameCount++;
            }

            Console.WriteLine($"Decoded {frameCount} frames.");
        }

        private static void WriteFrame(MediaFrame frame, Stream outStream)
        {
            var buf = new byte[frame.GetBytesSize(padding: false)];
            frame.GetBytes(buf, padding: false);
            outStream.Write(buf);
        }
    }
}

