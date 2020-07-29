using Emgu.CV;
using Emgu.CV.Structure;

using EmguFFmpeg.EmguCV;
using FFmpeg.AutoGen;
using System;

namespace EmguFFmpeg.Example
{
    public class EncodeVideoByMat 
    {
        /// <summary>
        /// create a 60 / <paramref name="fps"/> second video
        /// </summary>
        /// <param name="outputFile">video output</param>
        /// <param name="width">video width</param>
        /// <param name="height">video height</param>
        /// <param name="fps">video fps</param>
        public EncodeVideoByMat(string outputFile, int width, int height, int fps)
        {
            using (MediaWriter writer = new MediaWriter(outputFile))
            {
                writer.AddStream(MediaEncode.CreateVideoEncode(writer.Format, width, height, fps));
                writer.Initialize();

                VideoFrame dstframe = VideoFrame.CreateFrameByCodec(writer[0].Codec);

                Random random = new Random();
                for (int i = 0; i < 61; i++)
                {
                    // create a video frame by Mat
                    byte b = (byte)random.Next(0, 255);
                    byte g = (byte)random.Next(0, 255);
                    byte r = (byte)random.Next(0, 255);
                    using (Image<Bgr, byte> image = new Image<Bgr, byte>(width, height, new Bgr(b, g, r)))
                    {
                        string line1 = $"pts = {i}, color = [{b,3},{g,3},{r,3}]";
                        string line2 = $"time = {DateTime.Now:HH:mm:ss.fff}";
                        image.Draw(line1, new System.Drawing.Point(30, 50), Emgu.CV.CvEnum.FontFace.HersheyDuplex, 1, new Bgr(255 - b, 255 - g, 255 - r));
                        image.Draw(line2, new System.Drawing.Point(30, 100), Emgu.CV.CvEnum.FontFace.HersheyDuplex, 1, new Bgr(255 - b, 255 - g, 255 - r));
                        dstframe = image.Mat.ToVideoFrame(AVPixelFormat.AV_PIX_FMT_YUV420P);
                    }

                    dstframe.Pts = i; // video pts = seconds from video start * fps (pts can only increase).
                    // write video frame, many cases: one frame more out packet, first frame no out packet, etc.
                    // so use IEnumerable.
                    foreach (var packet in writer[0].WriteFrame(dstframe))
                    {
                        writer.WritePacket(packet);
                    }
                }

                // flush cache
                writer.FlushMuxer();
            }
        }
    }
}