
using FFmpeg.AutoGen;

using System.Linq;

namespace EmguFFmpeg.Example
{
    public class PngChromekeyFilter
    {
        /// <summary>
        /// a red cheomekey filter for video or image example.
        /// <para>
        /// ffmpeg -i <paramref name="input"/> -vf chromakey=red:0.1:0.0 <paramref name="output"/>
        /// </para>
        /// filter graph:
        /// ┌──────┐     ┌──────┐     ┌─────────┐     ┌──────────┐     ┌──────┐
        /// │input0│---->│buffer│---->│chromakey│---->│buffersink│---->│output│
        /// └──────┘     └──────┘     └─────────┘     └──────────┘     └──────┘
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="chromakeyOptions">rgb(green or 0x008000):similarity:blend, see http://ffmpeg.org/ffmpeg-filters.html#chromakey </param>
        public PngChromekeyFilter(string input, string output,string chromakeyOptions = "red:0.1:0.0")
        {
            using (MediaReader reader = new MediaReader(input))
            using (MediaWriter writer = new MediaWriter(output))
            {
                var videoIndex = reader.Where(_ => _.Codec.AVCodecContext.codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO).First().Index;

                // init filter
                int height = reader[videoIndex].Codec.AVCodecContext.height;
                int width = reader[videoIndex].Codec.AVCodecContext.width;
                int format = (int)reader[videoIndex].Codec.AVCodecContext.pix_fmt;
                AVRational time_base = reader[videoIndex].TimeBase;
                AVRational sample_aspect_ratio = reader[videoIndex].Codec.AVCodecContext.sample_aspect_ratio;

                MediaFilterGraph filterGraph = new MediaFilterGraph();
                filterGraph.AddVideoSrcFilter(new MediaFilter(MediaFilter.VideoSources.Buffer), width, height, (AVPixelFormat)format, time_base, sample_aspect_ratio)
                    .LinkTo(0, filterGraph.AddFilter(new MediaFilter("chromakey"), chromakeyOptions))
                    .LinkTo(0, filterGraph.AddVideoSinkFilter(new MediaFilter(MediaFilter.VideoSinks.Buffersink)));
                filterGraph.Initialize();

                // add stream by reader and init writer
                writer.AddStream(reader[videoIndex]);
                writer.Initialize();

                // init video frame format converter by dstcodec
                PixelConverter pixelConverter = new PixelConverter(writer[0].Codec);


                foreach (var srcPacket in reader.ReadPacket())
                {
                    foreach (var srcFrame in reader[videoIndex].ReadFrame(srcPacket))
                    {
                        filterGraph.Inputs.First().WriteFrame(srcFrame);
                        foreach (var filterFrame in filterGraph.Outputs.First().ReadFrame())
                        {
                            // can use filterFrame.ToMat() gets the output image directly without the need for a writer.
                            //using EmguFFmpeg.EmguCV;
                            //using (var mat = filterFrame.ToMat())
                            //{
                            //    mat.Save(output);
                            //}

                            foreach (var dstFrame in pixelConverter.Convert(filterFrame))
                            {
                                foreach (var dstPacket in writer[0].WriteFrame(dstFrame))
                                {
                                    writer.WritePacket(dstPacket);
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
