using System.Linq;
using FFmpeg.AutoGen;

namespace EmguFFmpeg.Example
{
    public class RtmpPull
    {
        public RtmpPull(string input)
        {
            MediaDictionary options = new MediaDictionary();
            options.Add("stimeout", "30000000"); // set connect timeout 30s

            using (MediaReader reader = new MediaReader(input, null, options))
            {
                var codecContext = reader.Where(_ => _.Codec.Type == AVMediaType.AVMEDIA_TYPE_VIDEO).First().Codec.AVCodecContext;

                PixelConverter videoFrameConverter = new PixelConverter(AVPixelFormat.AV_PIX_FMT_BGR24, codecContext.width, codecContext.height);
                foreach (var packet in reader.ReadPacket())
                {
                    foreach (var frame in reader[packet.StreamIndex].ReadFrame(packet))
                    {
                        // TODO
                    }
                }
            }
        }
    }
}