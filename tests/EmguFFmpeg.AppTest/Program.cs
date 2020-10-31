using System;
using System.Diagnostics;

namespace EmguFFmpeg.AppTest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
           

            var outputFile = "111.mp4";
            var width = 800;
            var height = 600;
            var fps = 60;
            using (MediaWriter writer = new MediaWriter(outputFile))
            {
                writer.AddStream(MediaEncoder.CreateVideoEncode(writer.Format, width, height, fps));
                writer.Initialize();

                VideoFrame dstframe = VideoFrame.CreateFrameByCodec(writer[0].Codec);

                Random random = new Random();
                for (int i = 0; i < 61; i++)
                {
                    // create a video frame by Mat
                    FillYuv420P(dstframe, i);

                    dstframe.Pts = i;  
                    foreach (var packet in writer[0].WriteFrame(dstframe))
                    {
                        writer.WritePacket(packet);
                    }
                }

                // flush cache
                writer.FlushMuxer();
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