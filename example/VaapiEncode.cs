using System;
using System.IO;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: vaapi_encode.c
    /// VAAPI-accelerated H.264 encoding: read raw NV12 frames from a file and
    /// write the encoded H.264 bitstream to an output file.
    /// Usage: args[0] = width, args[1] = height, args[2] = input.nv12, args[3] = output.h264
    /// </summary>
    public unsafe class VaapiEncode : ExampleBase
    {
        public VaapiEncode() { Index = 23; Enable = false; }

        public override void Execute()
        {
            if (args.Length < 4)
            {
                Console.Error.WriteLine("Usage: VaapiEncode <width> <height> <input.nv12> <output.h264>");
                return;
            }

            int    width   = int.Parse(args[0]);
            int    height  = int.Parse(args[1]);
            var    inFile  = args[2];
            var    outFile = args[3];
            int    size    = width * height;

            // ── Encoder (UseHardware creates the VAAPI device plus an NV12-backed
            //    hw_frames_ctx and wires both into the codec context) ───────────
            using var encoder = MediaEncoder.Video()
                .Codec("h264_vaapi")
                .Size(width, height)
                .Fps(new AVRational { num = 25, den = 1 })
                .UseHardware(AVPixelFormat.AV_PIX_FMT_VAAPI, AVPixelFormat.AV_PIX_FMT_NV12,
                             AVHWDeviceType.AV_HWDEVICE_TYPE_VAAPI)
                .Configure(c => c.Ref.sample_aspect_ratio = new AVRational { num = 1, den = 1 })
                .Build();

            // ── Encode loop ───────────────────────────────────────────────────
            using (var fin  = new FileStream(inFile,  FileMode.Open,   FileAccess.Read))
            using (var fout = new FileStream(outFile, FileMode.Create, FileAccess.Write))
            {
                using var swFrame = new MediaFrame();
                using var hwFrame = new MediaFrame();

                int err;
                while (true)
                {
                    // Allocate a software frame for NV12 data.
                    swFrame.Ref.width  = width;
                    swFrame.Ref.height = height;
                    swFrame.Ref.format = (int)AVPixelFormat.AV_PIX_FMT_NV12;
                    swFrame.AllocateBuffer();

                    // Read Y plane (width * height bytes).
                    var yBuf = new byte[size];
                    if (fin.Read(yBuf, 0, size) < size) break;
                    fixed (byte* pY = yBuf)
                        Buffer.MemoryCopy(pY, swFrame.Ref.data[0u], size, size);

                    // Read UV plane (width * height / 2 bytes).
                    var uvBuf = new byte[size / 2];
                    if (fin.Read(uvBuf, 0, size / 2) < size / 2) break;
                    fixed (byte* pUV = uvBuf)
                        Buffer.MemoryCopy(pUV, swFrame.Ref.data[1u], size / 2, size / 2);

                    // Allocate a HW frame and transfer SW → HW.
                    hwFrame.AllocateOnHWFrames(encoder.GetHWFramesRef());
                    MediaCodecContext.HWFrameTransferData(hwFrame, swFrame);

                    err = EncodeWrite(encoder, hwFrame, fout);
                    if (err != 0 && err != ffmpeg.AVERROR(ffmpeg.EAGAIN))
                    {
                        Console.Error.WriteLine("Failed to encode.");
                        break;
                    }

                    hwFrame.Unref();
                    swFrame.Unref();
                }

                // Flush encoder.
                err = EncodeWrite(encoder, null, fout);
                if (err == ffmpeg.AVERROR_EOF) err = 0;
            }
        }

        private static int EncodeWrite(MediaEncoder encoder, MediaFrame frame, Stream fout)
        {
            using var encPkt = new MediaPacket();
            int ret = encoder.SendFrame(frame);
            if (ret < 0)
            {
                Console.Error.WriteLine($"Error code: {FFmpegException.GetErrorString(ret)}");
                return ret;
            }

            while (true)
            {
                ret = encoder.ReceivePacket(encPkt);
                if (ret != 0) break;

                encPkt.Ref.stream_index = 0;
                fout.Write(encPkt.Data);
                encPkt.Unref();
            }

            return ret;
        }
    }
}
