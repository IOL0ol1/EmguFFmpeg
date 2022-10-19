namespace FFmpegSharp.Example
{
    internal class CreateMPEG4 : ExampleBase
    {

        public CreateMPEG4() : this($"{nameof(CreateMPEG4)}-output.mp4")
        { }

        public CreateMPEG4(params string[] args) : base(args)
        { }

        public override void Execute()
        {
            var outputFile = args[0];
            var fps = 25.999d;
            var width = 800;
            var heith = 600;
            using (var muxer = MediaMuxer.Create(outputFile))
            {
                using (var vEncoder = MediaEncoder.CreateVideoEncoder(muxer.Format, width, heith, fps))
                {
                    var vStream = muxer.AddStream(vEncoder);

                    muxer.WriteHeader();

                    using (var vFrame = MediaFrame.CreateVideoFrame(width, heith, vEncoder.PixFmt))
                    {
                        for (var i = 0; i < 30; i++)
                        {
                            FillYuv420P(vFrame, i);
                            vFrame.Pts = i;
                            foreach (var packet in vEncoder.EncodeFrame(vFrame))
                            {
                                packet.StreamIndex = vStream.Index;
                                muxer.WritePacket(packet, vEncoder.TimeBase);
                            }
                        }
                    }
                    muxer.FlushCodecs(new[] { vEncoder });
                    muxer.WriteTrailer();
                }
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
