using System;
using System.IO;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: scale_video.c
    /// Generate a synthetic 320×240 YUV420P video signal and rescale it to a
    /// user-specified size (default 640×480) in RGB24 using libswscale, writing
    /// the raw output to a file.
    /// </summary>
    public unsafe class ScaleVideo : ExampleBase
    {
        public ScaleVideo() { Index = 14; Enable = false; }

        public override void Execute()
        {
            var outFile  = args.Length > 0 ? args[0] : "out_scaled.rgb24";
            int dstW     = args.Length > 1 ? int.Parse(args[1]) : 640;
            int dstH     = args.Length > 2 ? int.Parse(args[2]) : 480;

            const int srcW = 320, srcH = 240;
            const AVPixelFormat srcFmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
            const AVPixelFormat dstFmt = AVPixelFormat.AV_PIX_FMT_RGB24;

            // Allocate source and destination frames; Swscale handles context creation lazily.
            using var srcFrame = MediaFrame.CreateVideoFrame(srcW, srcH, srcFmt);
            using var dstFrame = MediaFrame.CreateVideoFrame(dstW, dstH, dstFmt);
            using var scaler   = new Swscale();

            using var outStream = File.OpenWrite(outFile);

            for (int i = 0; i < 100; i++)
            {
                FillYuvImage(srcFrame, srcW, srcH, i);
                scaler.Convert(srcFrame, dstFrame);

                // Flatten (possibly padded) frame to a tightly-packed buffer for file output.
                int dstBufSize = ffmpeg.av_image_get_buffer_size(dstFmt, dstW, dstH, 1);
                var buf   = new byte[dstBufSize];
                var data4 = new byte_ptrArray4(); data4.UpdateFrom(dstFrame.Ref.data);
                var line4 = new int_array4();     line4.UpdateFrom(dstFrame.Ref.linesize);
                fixed (byte* pBuf = buf)
                    ffmpeg.av_image_copy_to_buffer(pBuf, dstBufSize, data4, line4, dstFmt, dstW, dstH, 1).ThrowIfError();
                outStream.Write(buf);
            }

            Console.Error.WriteLine($"Scaling succeeded. Play with:");
            Console.Error.WriteLine($"ffplay -f rawvideo -pix_fmt {ffmpeg.av_get_pix_fmt_name(dstFmt)} -video_size {dstW}x{dstH} {outFile}");
        }

        private static void FillYuvImage(MediaFrame frame, int width, int height, int frameIndex)
        {
            // Y plane.
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    frame.Ref.data[0][y * frame.Ref.linesize[0] + x] = (byte)(x + y + frameIndex * 3);

            // Cb and Cr planes.
            for (int y = 0; y < height / 2; y++)
            {
                for (int x = 0; x < width / 2; x++)
                {
                    frame.Ref.data[1][y * frame.Ref.linesize[1] + x] = (byte)(128 + y + frameIndex * 2);
                    frame.Ref.data[2][y * frame.Ref.linesize[2] + x] = (byte)(64  + x + frameIndex * 5);
                }
            }
        }
    }
}
