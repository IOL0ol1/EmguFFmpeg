
using FFmpeg.AutoGen;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace EmguFFmpeg.Example
{
    /// <summary>
    /// from <see cref="https://github.com/HEBOS"/>
    /// </summary>
    internal class DecodeVideoWithCustomCodecScaledToMat
    {
        /// <summary>
        /// decode video to image
        /// filter graph:
        /// ┌──────┐     ┌──────┐     ┌─────┐     ┌──────────┐     ┌──────┐
        /// │input0│---->│buffer│---->│scale│---->│buffersink│---->│output│
        /// └──────┘     └──────┘     └─────┘     └──────────┘     └──────┘
        /// </summary>
        /// <param name="inputFile">input video file</param>
        /// <param name="outDirectory">folder for output image files</param>
        /// <param name="scaleOptions">scale options <see cref="http://ffmpeg.org/ffmpeg-filters.html#scale-1"/></param>
        public DecodeVideoWithCustomCodecScaledToMat(string inputFile, string outDirectory, string scaleOptions = "512:288")
        {

            using (MediaReader reader = new MediaReader(inputFile, null, null))
            {

                var videoIndex = reader.First(_ => _.Codec.AVCodecContext.codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO).Index;

                unsafe
                {
                    // relpace the default vide decode
                    // !!! IMPORTANT NOTE: This sample won't work, if you haven't downloaded ffmpeg (GPL license, as it is more complete), and you don't have NVIDIA hardware (CUDA) !!!
                    reader[videoIndex].Codec = MediaDecode.CreateDecode("h264_cuvid", _ => ffmpeg.avcodec_parameters_to_context(_, reader[videoIndex].Stream.codecpar));
                }

                int height = reader[videoIndex].Codec.AVCodecContext.height;
                int width = reader[videoIndex].Codec.AVCodecContext.width;
                int format = (int)reader[videoIndex].Codec.AVCodecContext.pix_fmt;
                AVRational time_base = reader[videoIndex].TimeBase;
                AVRational sample_aspect_ratio = reader[videoIndex].Codec.AVCodecContext.sample_aspect_ratio;

                /* We are moving the packet to CUDA to perform the scaling.
                 We can then:
                 - remove hwdownload and format to leave it in CUDA, and forward the pointer to any other function, or write the frame to the output video
                 - convert it to MAT whereas converting speed depends on the size of the scaled frame.
                */
                MediaFilterGraph filterGraph = new MediaFilterGraph();
                filterGraph.AddVideoSrcFilter(new MediaFilter(MediaFilter.VideoSources.Buffer), width, height, (AVPixelFormat)format, time_base, sample_aspect_ratio)
                    .LinkTo(0, filterGraph.AddFilter(new MediaFilter("scale"), scaleOptions))
                    .LinkTo(0, filterGraph.AddVideoSinkFilter(new MediaFilter(MediaFilter.VideoSinks.Buffersink)));
                filterGraph.Initialize();

                var sw = Stopwatch.StartNew();
                foreach (var packet in reader.ReadPacket())
                {
                    foreach (var frame in reader[videoIndex].ReadFrame(packet))
                    {
                        filterGraph.Inputs.First().WriteFrame(frame);
                        foreach (var filterFrame in filterGraph.Outputs.First().ReadFrame())
                        {
                            using (var image = filterFrame.ToMat())
                            {
                                image.Save(Path.Combine(Directory.CreateDirectory(outDirectory).FullName, $"{DateTime.Now.Ticks}.jpg"));
                            }
                        }
                    }
                }
                Console.WriteLine($"Converting to MAT [ processed in {sw.Elapsed.TotalMilliseconds:0} ms ]");
            }
        }
    }
}
