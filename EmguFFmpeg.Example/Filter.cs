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

                MediaFilterGraph.CreateMediaFilterGraph("split [main][tmp]; [tmp] crop=iw:ih/2:0:0, vflip [flip]; [main][flip] overlay=0:H/2");

                MediaFilterGraph filterGraph = new MediaFilterGraph();
                int height = reader[videoIndex].Codec.AVCodecContext.height;
                int width = reader[videoIndex].Codec.AVCodecContext.width;
                int format = (int)reader[videoIndex].Codec.AVCodecContext.pix_fmt;
                AVRational time_base = reader[videoIndex].TimeBase;
                AVRational sample_aspect_ratio = reader[videoIndex].Codec.AVCodecContext.sample_aspect_ratio;
                string args = $"video_size={width}x{height}:pix_fmt={format}:time_base={time_base.num}/{time_base.den}:pixel_aspect={sample_aspect_ratio.num}/{sample_aspect_ratio.den}";

                filterGraph.AddFilter(new MediaFilter(MediaFilter.VideoSources.Buffer), args).LinkTo(0,
                    filterGraph.AddFilter(new MediaFilter("drawtext"), "fontsize=56:fontcolor=green:text='Hello World'"), 0).LinkTo(0,
                    filterGraph.AddFilter(new MediaFilter(MediaFilter.VideoSinks.Buffersink), _ =>
                    {
                        fixed (void* pixelFmts = new AVPixelFormat[] { AVPixelFormat.AV_PIX_FMT_NONE })
                        {
                            ffmpeg.av_opt_set_bin(_, "pixel_fmts", (byte*)pixelFmts, 0, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                        }
                    }), 0);
                filterGraph.Initialize();
                //MediaFilter bufferSrc = MediaFilter.CreateBufferFilter();
                //MediaFilter bufferSink = MediaFilter.CreateBufferSinkFilter();
                //AVBufferSrcParameters parameters = new AVBufferSrcParameters();
                //parameters.height = reader[videoIndex].Codec.AVCodecContext.height;
                //parameters.width = reader[videoIndex].Codec.AVCodecContext.width;
                //parameters.format = (int)reader[videoIndex].Codec.AVCodecContext.pix_fmt;
                //parameters.time_base = reader[videoIndex].TimeBase;
                //parameters.sample_aspect_ratio = reader[videoIndex].Codec.AVCodecContext.sample_aspect_ratio;

                ////string args = $"video_size={width}x{height}:pix_fmt={format}:time_base={timebase.num}/{timebase.den}:pixel_aspect={aspect.num}/{aspect.den}";
                //bufferSrc.Initialize(filterGraph, parameters);
                //bufferSink.Initialize(filterGraph, _ =>
                //{
                //    fixed (void* pixelFmts = new AVPixelFormat[] { AVPixelFormat.AV_PIX_FMT_NONE })
                //    {
                //        ffmpeg.av_opt_set_bin((void*)(AVFilterContext*)bufferSink, "pixel_fmts", (byte*)pixelFmts, 0, ffmpeg.AV_OPT_SEARCH_CHILDREN);
                //    }
                //});
                //bufferSrc.LinkTo(0, bufferSink, 0);
                //filterGraph.Initialize();

                writer.AddStream(reader[videoIndex]);
                writer.Initialize();
                long PTS = 0;
                foreach (var packet in reader.ReadPacket())
                {
                    foreach (var frame in reader[videoIndex].ReadFrame(packet))
                    {
                        PTS += 1;// frame.NbSamples;
                        frame.Pts = PTS;
                        int ret = filterGraph.Inputs.First().WriteFrame(frame);

                        foreach (var item in filterGraph.Outputs.First().ReadFrame())
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