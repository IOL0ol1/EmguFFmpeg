using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EmguFFmpeg.Example
{
    /// <summary>
    /// a Yuv420P sample ,thanks https://github.com/sdcb
    /// </summary>
    public class FillYuv420PSample
    {
        /// <summary>
        /// Yuv420P sample
        /// </summary>
        /// <param name="outputFile">output file</param>
        /// <param name="width">video width</param>
        /// <param name="height">video height</param>
        /// <param name="fps">video fps</param>
        public FillYuv420PSample(string outputFile, int width, int height, int fps)
        {

            var dir = Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(outputFile), Path.GetFileNameWithoutExtension(outputFile))).FullName;

            using (MediaWriter writer = new MediaWriter(outputFile))
            {
                writer.AddStream(MediaEncoder.CreateVideoEncode(writer.Format, width, height, fps));
                writer.Initialize();

                VideoFrame srcframe = new VideoFrame(width, height, FFmpeg.AutoGen.AVPixelFormat.AV_PIX_FMT_YUV420P);
                PixelConverter pixelConverter = new PixelConverter(writer[0].Codec);

                Random random = new Random();
                for (int i = 0; i < fps * 10; i++)
                {
                    // fill video frame
                    FillYuv420P(srcframe, i);

                    foreach (var dstframe in pixelConverter.Convert(srcframe))
                    {
                        dstframe.Pts = i;
                        SaveFrame(dstframe, Path.Combine(dir, $"{i}.bmp"));
                        foreach (var packet in writer[0].WriteFrame(dstframe))
                        {
                            writer.WritePacket(packet);
                        }
                    }
                }

                // flush cache
                writer.FlushMuxer();
            }
        }

        /// <summary>
        /// save to bitmap
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="output"></param>

        private static void SaveFrame(VideoFrame frame, string output)
        {
            using (var b = frame.ToBitmap())
            {
                b.Save(output);
            }
        }


        /// <summary>
        /// Fill frame
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="i"></param>
        private static unsafe void FillYuv420P(VideoFrame frame, long i)
        {
            int linesize0 = frame.Linesize[0];
            int linesize1 = frame.Linesize[1];
            int linesize2 = frame.Linesize[2];

            byte* data0 = (byte*)frame.Data[0];
            byte* data1 = (byte*)frame.Data[1];
            byte* data2 = (byte*)frame.Data[2];

            /* prepare a dummy image */
            /* Y */
            for (int y = 0; y < frame.Height; y++)
            {
                for (int x = 0; x < frame.Width; x++)
                {
                    data0[y * linesize0 + x] = (byte)(x + y + i * 3);
                }
            }

            /* Cb and Cr */
            for (int y = 0; y < frame.Height / 2; y++)
            {
                for (int x = 0; x < frame.Width / 2; x++)
                {
                    data1[y * linesize1 + x] = (byte)(128 + y + i * 2);
                    data2[y * linesize2 + x] = (byte)(64 + x + i * 5);
                }
            }

            frame.Pts = i;
        }
    }
}
