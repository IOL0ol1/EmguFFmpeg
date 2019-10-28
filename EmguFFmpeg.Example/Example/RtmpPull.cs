using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmguFFmpeg.Example
{
    public class RtmpPull : IExample
    {
        private string input;

        public RtmpPull(string rtmpUrl)
        {
            input = rtmpUrl;
        }

        public void Start()
        {
            MediaDictionary options = new MediaDictionary();
            options.Add("stimeout", "30000000", 0); // set timeout 30s

            using (MediaReader reader = new MediaReader(input, null, options))
            {
                var codecContext = reader.Where(_ => _.Codec.Type == AVMediaType.AVMEDIA_TYPE_VIDEO).First().Codec.AVCodecContext;

                VideoFrameConverter videoFrameConverter = new VideoFrameConverter(AVPixelFormat.AV_PIX_FMT_BGR24, codecContext.width, codecContext.height);
                foreach (var packet in reader.Packets)
                {
                    if (reader[packet.StreamIndex].CanRead)
                    {
                        foreach (var frame in reader[packet.StreamIndex].ReadFrame(packet))
                        {
                            // TODO: converter to bgr24 data
                            var avframeData = frame.ToArray();
                        }
                    }
                }
            }
        }
    }
}