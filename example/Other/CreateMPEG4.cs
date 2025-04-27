using System;
using System.Diagnostics;
using System.IO;
using FFmpeg.AutoGen.Abstractions;
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
            using (var muxer = MediaMuxer.Create(File.OpenWrite(outputFile), MediaOutputFormat.GuessFormat(null, outputFile, null)))
            using (var convert = new Swscale())
            {
                using (var vEncoder = MediaEncoder.CreateVideoEncoder(muxer.Format, width, heith, fps, otherSettings: _ => _.Ref.thread_count = 10))
                using (var f = MediaFrame.CreateVideoFrame(vEncoder.Ref.width, vEncoder.Ref.height, vEncoder.Ref.pix_fmt))
                {
                    //convert.SetOpts(width, heith, vEncoder.Ref.pix_fmt);
                    var vStream = muxer.AddStream(vEncoder);
                    muxer.WriteHeader();

                    using (var vFrame = MediaFrame.CreateVideoFrame(width, heith, AVPixelFormat.AV_PIX_FMT_BGR24))
                    {
                        for (var i = 0; i < 3000; i++)
                        {
                            FillBgr24(vFrame, i);
                            foreach (var frame in convert.Convert(vFrame, f))
                            {
                                //FillYuv420P(vFrame, i);
                                frame.Ref.pts = i;
                                foreach (var packet in vEncoder.EncodeFrame(frame))
                                {
                                    packet.Ref.stream_index = vStream.Ref.index;
                                    muxer.WritePacket(packet, vEncoder.Ref.time_base);
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
            using (var mat = new Mat(frame.Ref.height, frame.Ref.width, MatType.CV_8UC3, Scalar.RandomColor()))
            {
                mat.PutText($"{i}", new Point(50, 50), HersheyFonts.HersheyPlain, 5, Scalar.White, 1, LineTypes.AntiAlias);
                var srcLineSize = (int)mat.Step();
                var dstLineSize = frame.Ref.linesize[0];
                FFmpegUtil.CopyPlane(mat.Data, srcLineSize,
                   (IntPtr)frame.Ref.data[0], dstLineSize, Math.Min(srcLineSize, dstLineSize), frame.Ref.height);
            }
        }

        /// <summary>
        /// Fill frame
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="i"></param>
        private static unsafe void FillYuv420P(MediaFrame frame, int i)
        {
            var data = frame.Ref.data;
            var linesize = frame.Ref.linesize;
            /* Prepare a dummy image.
              In real code, this is where you would have your own logic for
              filling the frame. FFmpeg does not care what you put in the
              frame.
            */
            /* Y */
            for (var y = 0; y < frame.Ref.height; y++)
            {
                for (var x = 0; x < frame.Ref.width; x++)
                {
                    data[0][y * linesize[0] + x] = (byte)(x + y + i * 3);
                }
            }

            /* Cb and Cr */
            for (var y = 0; y < frame.Ref.height / 2; y++)
            {
                for (var x = 0; x < frame.Ref.width / 2; x++)
                {
                    data[1][y * linesize[1] + x] = (byte)(128 + y + i * 2);
                    data[2][y * linesize[2] + x] = (byte)(64 + x + i * 5);
                }
            }
        }
    }
}
