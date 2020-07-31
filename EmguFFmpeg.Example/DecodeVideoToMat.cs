


using FFmpeg.AutoGen;

using System.IO;
using System.Linq;

namespace EmguFFmpeg.Example
{

    namespace EmgucvExtern
    {
        using EmguFFmpeg.EmgucvExtern;

        internal class DecodeVideoToMat
        {
            /// <summary>
            /// decode video to image
            /// </summary>
            /// <param name="inputFile">input video file</param>
            /// <param name="outDirectory">folder for output image files</param>
            public DecodeVideoToMat(string inputFile, string outDirectory)
            {
                string outputdir = Directory.CreateDirectory(outDirectory).FullName;
                using (MediaReader reader = new MediaReader(inputFile))
                {
                    var videoIndex = reader.Where(_ => _.Codec.AVCodecContext.codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO).First().Index;

                    foreach (var packet in reader.ReadPacket())
                    {
                        foreach (var frame in reader[videoIndex].ReadFrame(packet))
                        {
                            using (var image = frame.ToMat())
                            {
                                image.Save(Path.Combine(outputdir, $"{frame.Pts}.bmp"));
                            }
                        }
                    }
                }
            }
        }
    }


    namespace OpenCvSharpExtern
    {
        using EmguFFmpeg.OpenCvSharpExtern;

        using System;
        using System.Diagnostics;

        internal class DecodeVideoToMat
        {

            /// <summary>
            /// decode video to image
            /// </summary>
            /// <param name="inputFile">input video file</param>
            /// <param name="outDirectory">folder for output image files</param>
            public DecodeVideoToMat(string inputFile, string outDirectory)
            {
                string outputdir = Directory.CreateDirectory(outDirectory).FullName;
                using (MediaReader reader = new MediaReader(inputFile))
                {
                    var videoIndex = reader.Where(_ => _.Codec.AVCodecContext.codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO).First().Index;

                    var sw = Stopwatch.StartNew();

                    foreach (var packet in reader.ReadPacket())
                    {
                        foreach (var frame in reader[videoIndex].ReadFrame(packet))
                        {
                            using (var image = frame.ToMat())
                            {
                                image.SaveImage(Path.Combine(outputdir, $"{frame.Pts}.bmp"));
                            }
                        }
                    }
                    Console.WriteLine($"Converting to MAT [ processed in {sw.Elapsed.TotalMilliseconds:0} ms ]");
                }
            }
        }
    }
}