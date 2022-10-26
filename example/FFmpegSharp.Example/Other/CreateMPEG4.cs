using System;
using System.Diagnostics;
using System.IO;
using FFmpeg.AutoGen;
using OpenCvSharp;

namespace FFmpegSharp.Example
{
    internal class CreateMPEG4 : ExampleBase
    {

        public CreateMPEG4() : this($"{nameof(CreateMPEG4)}-output.mp4")
        {
        }

        public CreateMPEG4(params string[] args) : base(args)
        { }

        public override void Execute()
        {
            var outputFile = args[0];
            var fps = 25.999d;
            var width = 800;
            var heith = 600;
            var s = Stopwatch.StartNew();
            using (var muxer = MediaMuxer.Create(File.OpenWrite(outputFile), OutFormat.GuessFormat(null, outputFile, null)))
            using (var convert = new PixelConverter())
            {
                using (var vEncoder = MediaEncoder.CreateVideoEncoder(muxer.Format, width, heith, fps, otherSettings: _ => _.ThreadCount = 10))
                {
                    convert.SetOpts(width, heith, vEncoder.PixFmt);
                    var vStream = muxer.AddStream(vEncoder);
                    muxer.WriteHeader();

                    using (var vFrame = MediaFrame.CreateVideoFrame(width, heith, AVPixelFormat.AV_PIX_FMT_BGR24))
                    {
                        for (var i = 0; i < 3000; i++)
                        {
                            FillBgr24(vFrame, i);
                            foreach (var frame in convert.Convert(vFrame))
                            {
                                //FillYuv420P(vFrame, i);
                                frame.Pts = i;
                                foreach (var packet in vEncoder.EncodeFrame(frame))
                                {
                                    packet.StreamIndex = vStream.Index;
                                    muxer.WritePacket(packet, vEncoder.TimeBase);
                                }
                            }
                        }
                    }
                    muxer.FlushCodecs(new[] { vEncoder });
                    muxer.WriteTrailer();
                }
            }
            Console.WriteLine($"{s.Elapsed.TotalMilliseconds}ms");
        }


        private static unsafe void FillBgr24(MediaFrame frame, int i)
        {
            using (var mat = new Mat(frame.Height, frame.Width, MatType.CV_8UC3, Scalar.RandomColor()))
            {
                mat.PutText($"{i}", new Point(50, 50), HersheyFonts.HersheyPlain, 5, Scalar.White, 1, LineTypes.AntiAlias);
                var srcLineSize = (int)mat.Step();
                var dstLineSize = frame.Linesize[0];
                FFmpegUtil.CopyPlane(mat.Data, srcLineSize,
                   (IntPtr)frame.Ref.data[0], dstLineSize, Math.Min(srcLineSize, dstLineSize), frame.Height);
            }
        }

        /// <summary>
        /// Fill frame
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="i"></param>
        private static unsafe void FillYuv420P(MediaFrame frame, int i)
        {
            var data = frame.Data;
            var linesize = frame.Linesize;
            /* Prepare a dummy image.
              In real code, this is where you would have your own logic for
              filling the frame. FFmpeg does not care what you put in the
              frame.
            */
            /* Y */
            for (var y = 0; y < frame.Height; y++)
            {
                for (var x = 0; x < frame.Width; x++)
                {
                    data[0][y * linesize[0] + x] = (byte)(x + y + i * 3);
                }
            }

            /* Cb and Cr */
            for (var y = 0; y < frame.Height / 2; y++)
            {
                for (var x = 0; x < frame.Width / 2; x++)
                {
                    data[1][y * linesize[1] + x] = (byte)(128 + y + i * 2);
                    data[2][y * linesize[2] + x] = (byte)(64 + x + i * 5);
                }
            }
        }
    }
}
