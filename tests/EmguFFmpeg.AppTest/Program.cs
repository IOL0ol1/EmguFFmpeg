using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FFmpeg.AutoGen;

namespace EmguFFmpeg.AppTest
{
    internal unsafe class Program
    {
        private static void Main(string[] args)
        {
            var version = FFmpegHelper.RegisterBinaries(@"E:\\Projects\\EmguFFmpeg\\tests\\EmguFFmpeg.AppTest\\bin\\Debug\\netcoreapp3.1\\bin");
            Console.WriteLine(version);

            using (var muxer = new MediaWriter(@"C:\path-to-your.mp4"))
            {
                muxer.AddStream()
            }

            using (var demuxer = new MediaReader(@"C:\path-to-your.mp4"))
            {
                var coders = demuxer.Select(_ => MediaReader.CreateDefaultCodecContext(_)).ToList();
                foreach (var packet in demuxer.ReadPackets())
                {
                    if (coders[packet.StreamIndex] != null)
                    {
                        foreach (var frame in coders[packet.StreamIndex].DecodePacket(packet))
                        {
                            Trace.TraceInformation($"{coders[packet.StreamIndex].CodecType}\t{frame.Pts}\t{frame.Height}\t{frame.Width}\t{frame.NbSamples}");
                        }
                    }
                }
                coders.ForEach(_ => _?.Dispose());
            }

        }


        /// <summary>
        /// Fill frame
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="i"></param>
        private static unsafe void FillYuv420P(MediaFrame frame, long i)
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

