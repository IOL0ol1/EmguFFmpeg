using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmguFFmpeg.Example
{
    class Video2Frame2Video
    {
        public Video2Frame2Video(string inputFile,string outputFile)
        {
            using (MediaReader reader = new MediaReader(inputFile))
            using (MediaWriter writer = new MediaWriter(outputFile))
            {
                var videoIndex = reader.Where(_ => _.Codec.AVCodecContext.codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO).First().Index;
                writer.AddStream(reader[videoIndex]);
                writer.Initialize();

                PixelConverter pixelConverter = new PixelConverter(writer.First().Codec);

                foreach (var packet in reader.ReadPacket())
                {
                    foreach (var frame in reader[videoIndex].ReadFrame(packet))
                    {
                        foreach (var dstFrame in pixelConverter.Convert(frame))
                        {
                            foreach (var dstPacket in writer[0].WriteFrame(dstFrame))
                            {
                                writer.WritePacket(dstPacket);
                            }
                        }
                    }
                }
                writer.FlushMuxer();
            }
        }
    }
}
