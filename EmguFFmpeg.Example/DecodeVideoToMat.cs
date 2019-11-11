using EmguFFmpeg.EmguCV;

using FFmpeg.AutoGen;

using System;
using System.IO;
using System.Linq;

namespace EmguFFmpeg.Example
{
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