using EmguFFmpeg.EmguCV;
using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmguFFmpeg.Example
{
    public class Filter
    {
        public unsafe Filter(string input, string output)
        {
            using (MediaReader reader = new MediaReader(input))
            using (MediaWriter writer = new MediaWriter(output))
            {
                var videoIndex = reader.Where(_ => _.Codec.AVCodecContext.codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO).First().Index;

                MediaFilterGraph filterGraph = new MediaFilterGraph();
                int height = reader[videoIndex].Codec.AVCodecContext.height;
                int width = reader[videoIndex].Codec.AVCodecContext.width;
                int format = (int)reader[videoIndex].Codec.AVCodecContext.pix_fmt;
                AVRational time_base = reader[videoIndex].TimeBase;
                AVRational sample_aspect_ratio = reader[videoIndex].Codec.AVCodecContext.sample_aspect_ratio;

                filterGraph.AddVideoSrcFilter(new MediaFilter(MediaFilter.VideoSources.Buffer), width, height, (AVPixelFormat)format, time_base, sample_aspect_ratio).LinkTo(0,
                    filterGraph.AddFilter(new MediaFilter("setpts"), "2*PTS")).LinkTo(0,
                    filterGraph.AddVideoSinkFilter(new MediaFilter(MediaFilter.VideoSinks.Buffersink), new AVPixelFormat[] { AVPixelFormat.AV_PIX_FMT_NONE }));

                filterGraph.Initialize();

                writer.AddStream(reader[videoIndex]);
                writer.Initialize();
                long pts1 = 0;
                long pts2 = 0;
                foreach (var packet in reader.ReadPacket())
                {
                    foreach (var frame in reader[videoIndex].ReadFrame(packet))
                    {
                        frame.Pts = pts1;
                        pts1 += 1;
                        filterGraph.Inputs.First().WriteFrame(frame);
                        foreach (var item in filterGraph.Outputs.First().ReadFrame())
                        {
                            if (pts2 == 0 || item.Pts > pts2)
                            {
                                pts2++;
                                foreach (var dstpacket in writer[0].WriteFrame(item))
                                {
                                    writer.WritePacket(dstpacket);
                                }
                            }
                        }
                    }
                }
                writer.FlushMuxer();
            }
        }
    }
}