namespace EmguFFmpeg.AppTest
{
    internal class CreateMPEG4 : ExampleBase
    {

        public CreateMPEG4(string outputFile = @"path-to-your.mp4")
        {
            parames["outputFile"] = outputFile;
        }

        public override void Execute()
        {
            var outputFile = GetParame<string>("outputFile");
            using (var muxer = new MediaMuxer(outputFile))
            {
                using (var vEncoder = MediaEncoder.CreateVideoEncoder(muxer.Format, 800, 600, 30))
                {
                    MediaStream vStream = muxer.AddStream(vEncoder);

                    muxer.WriteHeader();

                    using (var vFrame = MediaFrame.CreateVideoFrame(800, 600, vEncoder.Context.PixFmt))
                    {
                        for (int i = 0; i < 30; i++)
                        {
                            FillYuv420P(vFrame, i);
                            foreach (var packet in vEncoder.EncodeFrame(vFrame))
                            {
                                packet.StreamIndex = vStream.Index;
                                muxer.WritePacket(packet, vEncoder.Context.TimeBase);
                            }
                        }
                    }
                    muxer.FlushCodecs(new[] { vEncoder });
                }
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
