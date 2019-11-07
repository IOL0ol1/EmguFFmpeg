using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace EmguFFmpeg.Example
{
    public class CreateVideo : IExample
    {
        private string output;

        public CreateVideo(string outputFile)
        {
            output = outputFile;
        }

        public void Start()
        {
            int width = 1920;
            int height = 1080;
            int fps = 30;

            using (MediaWriter writer = new MediaWriter(output))
            {
                writer.AddStream(MediaEncode.CreateVideoEncode(writer.Format.VideoCodec, writer.Format.Flags, width, height, fps));
                writer.Initialize();

                VideoFrame videoFrame = new VideoFrame(AVPixelFormat.AV_PIX_FMT_YUV420P, width, height);

                // create 60s during video
                long lastPts = -1;
                Stopwatch stopwatch = Stopwatch.StartNew();
                while (stopwatch.Elapsed < TimeSpan.FromSeconds(60))
                {
                    // pts must be monotonically increased
                    var curPts = (long)(stopwatch.Elapsed.TotalSeconds * fps);
                    if (curPts <= lastPts)
                        continue;

                    // TODO: add converter to fill videoframe yuv data from bgr24 data
                    // TODO: change WriteFrame interface, add timespan parame

                    videoFrame.Pts = curPts;
                    lastPts = curPts;
                    // write video frame, many cases: one frame more packet, first frame no packet, etc.
                    // so use IEnumerable.
                    foreach (var packet in writer[0].WriteFrame(videoFrame))
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