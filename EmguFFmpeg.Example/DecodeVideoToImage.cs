using FFmpeg.AutoGen;
using EmguFFmpeg.EmguCV;
using System.IO;
using System.Linq;
using Emgu.CV;
using System;

namespace EmguFFmpeg.Example
{
    internal class DecodeVideoToImage
    {
        /// <summary>
        /// decode video to image
        /// </summary>
        /// <param name="inputFile">input video file</param>
        /// <param name="outDirectory">folder for output image files</param>
        public DecodeVideoToImage(string inputFile, string outDirectory)
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
                            // convert pts to timespan string
                            if (reader[videoIndex].TryToTimeSpan(frame.Pts, out TimeSpan ts))
                            {
                                image.Save(Path.Combine(outputdir, $"{ts.ToString().Replace(":", ".")}.bmp"));
                            }
                            else
                            {
                                image.Save(Path.Combine(outputdir, $"{frame.Pts}.bmp"));
                            }
                        }
                    }
                }
            }
        }
    }
}