using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmguFFmpeg.Example
{
    public class DecodeAudio : IExample
    {

        public DecodeAudio(string inputFile)
        {
            using (MediaReader reader = new MediaReader(inputFile))
            {
                foreach (var packet in reader.ReadPacket())
                {

                    // audio maybe have one more stream, e.g. 0 is mp3 audio, 1 is mpeg cover
                    var audioIndex = reader.Where(_ => _.Codec.AVCodecContext.codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO).First().Index;

                    foreach (var frame in reader[audioIndex].ReadFrame(packet))
                    {
                        // This is a copy to managed memory from AVFrame.data
                        // NOTE: memory alignment causes it maybe longer than valid data.
                        // TODO: add converter to convert other format.
                        var avframeData = frame.GetData();
                    }
                }
            }
        }

    }
}