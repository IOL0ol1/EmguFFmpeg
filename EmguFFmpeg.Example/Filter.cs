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
            MediaFilterGraph filterGraph = new MediaFilterGraph();
            MediaFilter bufferSrc = MediaFilter.CreateBufferFilter();
            MediaFilter bufferSink = MediaFilter.CreateBufferSinkFilter();
            bufferSrc.Initialize(filterGraph, "");
            bufferSink.Initialize(filterGraph, "");
            bufferSrc.LinkTo(0, bufferSink, 0);

            using (MediaReader reader = new MediaReader(input))
            using (MediaWriter writer = new MediaWriter(output))
            {
                var videoIndex = reader.Where(_ => _.Codec.AVCodecContext.codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO).First().Index;
                writer.AddStream(reader[videoIndex]);
                writer.Initialize();
                long PTS = 0;
                foreach (var packet in reader.ReadPacket())
                {
                    foreach (var frame in reader[videoIndex].ReadFrame(packet))
                    {
                        PTS += 1;// frame.NbSamples;
                        frame.Pts = PTS;

                        foreach (var dstpacket in writer[0].WriteFrame(frame))
                        {
                            writer.WritePacket(dstpacket);
                        }
                    }
                }
                writer.FlushMuxer();
            }
        }
    }
}