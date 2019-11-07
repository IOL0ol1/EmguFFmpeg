using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace EmguFFmpeg.Example
{
    public class CreateVideo : IExample
    {
        public CreateVideo(string outputFile, int width, int height, int fps)
        {
            using (MediaWriter writer = new MediaWriter(outputFile))
            {
                writer.AddStream(MediaEncode.CreateVideoEncode(writer.Format.VideoCodec, writer.Format.Flags, width, height, fps));
                writer.Initialize();

                VideoFrame dstframe = VideoFrame.CreateFrameByCodec(writer[0].Codec);

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

                    dstframe.Pts = curPts; // video's pts is second * fps, pts can only increase.
                    lastPts = curPts;
                    // write video frame, many cases: one frame more packet, first frame no packet, etc.
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