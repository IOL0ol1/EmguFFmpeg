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
                MediaFilter bufferSrc = MediaFilter.CreateBufferFilter();
                MediaFilter bufferSink = MediaFilter.CreateBufferSinkFilter();
                AVBufferSrcParameters parameters = new AVBufferSrcParameters();
                parameters.height = reader[videoIndex].Codec.AVCodecContext.height;
                parameters.width = reader[videoIndex].Codec.AVCodecContext.width;
                parameters.format = (int)reader[videoIndex].Codec.AVCodecContext.pix_fmt;
                parameters.time_base = reader[videoIndex].TimeBase;
                parameters.sample_aspect_ratio = reader[videoIndex].Codec.AVCodecContext.sample_aspect_ratio;

                //string args = $"video_size={width}x{height}:pix_fmt={format}:time_base={timebase.num}/{timebase.den}:pixel_aspect={aspect.num}/{aspect.den}";
                bufferSrc.Initialize(filterGraph, parameters);
                bufferSink.Initialize(filterGraph, _ =>
                {
                    fixed (void* pixelFmts = new AVPixelFormat[] { AVPixelFormat.AV_PIX_FMT_NONE })
                    {
                        ffmpeg.av_opt_set_bin((void*)(AVFilterContext*)bufferSink, "pixel_fmts", (byte*)pixelFmts, 0, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                    }
                });
                bufferSrc.LinkTo(0, bufferSink, 0);
                filterGraph.Initialize();

                writer.AddStream(reader[videoIndex]);
                writer.Initialize();
                long PTS = 0;
                foreach (var packet in reader.ReadPacket())
                {
                    foreach (var frame in reader[videoIndex].ReadFrame(packet))
                    {
                        PTS += 1;// frame.NbSamples;
                        frame.Pts = PTS;
                        int ret = bufferSrc.WriteFrame(frame);

                        foreach (var item in bufferSink.ReadFrame())
                        {
                            foreach (var dstpacket in writer[0].WriteFrame(item))
                            {
                                writer.WritePacket(dstpacket);
                            }
                        }
                    }
                }
                writer.FlushMuxer();
            }
        }
    }
}