using System;
using System.Collections.Generic;
using System.Text;

namespace EmguFFmpeg.Example
{
    public class DecodeAudio : IExample
    {
        private string input;

        public DecodeAudio(string audioFile)
        {
            input = audioFile;
        }

        public void Start()
        {
            using (MediaReader reader = new MediaReader(input))
            {
                foreach (var packet in reader.ReadPacket())
                {
                    // audio maybe have one more stream, e.g.: 0 is mp3 audio, 1 is mpeg cover
                    foreach (var frame in reader[packet.StreamIndex].ReadFrame(packet))
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