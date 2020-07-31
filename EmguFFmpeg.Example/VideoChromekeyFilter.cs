using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmguFFmpeg.Example
{
    public class VideoChromekeyFilter
    {

        /// <summary>
        /// Make the specified color of <paramref name="input0"/> transparent and overlay it on the <paramref name="input1"/> video to <paramref name="output"/>
        /// <para>
        /// NOTE: green [R:0 G:128 B:0]
        /// </para>
        /// <para>
        /// ffmpeg -i <paramref name="input0"/> -i <paramref name="input1"/> -filter_complex "[1:v]chromakey=green:0.1:0.0[ckout];[0:v][ckout]overlay[out]" -map "[out]" <paramref name="output"/>
        /// </para>
        /// filter graph:
        /// ┌──────┐     ┌──────┐     ┌─────────┐     ┌─────────┐
        /// │input0│---->│buffer│---->│chromakey│---->│         │
        /// └──────┘     └──────┘     └─────────┘     │         │     ┌──────────┐     ┌──────┐
        ///                                           │ overlay │---->│buffersink│---->│output│
        /// ┌──────┐     ┌──────┐                     │         │     └──────────┘     └──────┘
        /// │input1│-----│buffer│-------------------->│         │
        /// └──────┘     └──────┘                     └─────────┘
        /// </summary> 
        /// <param name="input0">foreground</param>
        /// <param name="input1">background</param>
        /// <param name="output">output</param>
        /// <param name="chromakeyOptions">rgb(green or 0x008000):similarity:blend, see <see cref="http://ffmpeg.org/ffmpeg-filters.html#chromakey"/> </param>
        public VideoChromekeyFilter(string input0, string input1, string output,string chromakeyOptions = "green:0.1:0.0")
        {
            using (MediaReader reader0 = new MediaReader(input0))
            using (MediaReader reader1 = new MediaReader(input1))
            using (MediaWriter writer = new MediaWriter(output))
            {
                var videoIndex0 = reader0.Where(_ => _.Codec.AVCodecContext.codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO).First().Index;
                var videoIndex1 = reader1.Where(_ => _.Codec.AVCodecContext.codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO).First().Index;

                // init complex filter graph
                int height0 = reader0[videoIndex0].Codec.AVCodecContext.height;
                int width0 = reader0[videoIndex0].Codec.AVCodecContext.width;
                int format0 = (int)reader0[videoIndex0].Codec.AVCodecContext.pix_fmt;
                AVRational time_base0 = reader0[videoIndex0].TimeBase;
                AVRational sample_aspect_ratio0 = reader0[videoIndex0].Codec.AVCodecContext.sample_aspect_ratio;

                int height1 = reader1[videoIndex1].Codec.AVCodecContext.height;
                int width1 = reader1[videoIndex1].Codec.AVCodecContext.width;
                int format1 = (int)reader1[videoIndex1].Codec.AVCodecContext.pix_fmt;
                AVRational time_base1 = reader1[videoIndex1].TimeBase;
                AVRational sample_aspect_ratio1 = reader1[videoIndex1].Codec.AVCodecContext.sample_aspect_ratio;

                MediaFilterGraph filterGraph = new MediaFilterGraph();
                var in0 = filterGraph.AddVideoSrcFilter(new MediaFilter(MediaFilter.VideoSources.Buffer), width0, height0, (AVPixelFormat)format0, time_base0, sample_aspect_ratio0);
                var in1 = filterGraph.AddVideoSrcFilter(new MediaFilter(MediaFilter.VideoSources.Buffer), width1, height1, (AVPixelFormat)format1, time_base1, sample_aspect_ratio1);
                var chromakey = filterGraph.AddFilter(new MediaFilter("chromakey"), chromakeyOptions); 
                var overlay = filterGraph.AddFilter(new MediaFilter("overlay"));
                var out0 = filterGraph.AddVideoSinkFilter(new MediaFilter(MediaFilter.VideoSinks.Buffersink));
                in0.LinkTo(0, chromakey, 0).LinkTo(0, overlay, 1).LinkTo(0, out0, 0);
                in1.LinkTo(0, overlay, 0);
                filterGraph.Initialize();

                // add stream by reader and init writer
                writer.AddStream(reader0[videoIndex0]);
                writer.Initialize();

                // init video frame format converter by dstcodec
                PixelConverter pixelConverter = new PixelConverter(writer[0].Codec);

                long pts = 0;
                MediaReader[] readers = new MediaReader[] { reader0, reader1 };
                int[] index = new int[] { videoIndex0, videoIndex1 };
                for (int i = 0; i < readers.Length; i++)
                {
                    var reader = readers[i];
                    foreach (var srcPacket in reader.ReadPacket())
                    {
                        foreach (var srcFrame in reader[index[i]].ReadFrame(srcPacket))
                        {
                            filterGraph.Inputs[i].WriteFrame(srcFrame);
                            foreach (var filterFrame in filterGraph.Outputs.First().ReadFrame())
                            {
                                foreach (var dstFrame in pixelConverter.Convert(filterFrame))
                                {
                                    dstFrame.Pts = pts++;
                                    foreach (var dstPacket in writer[0].WriteFrame(dstFrame))
                                    {
                                        writer.WritePacket(dstPacket);
                                    }
                                }
                            }
                        }
                    }
                }

                // flush codec cache
                writer.FlushMuxer();
            }
        }
    }
}
