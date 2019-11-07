using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmguFFmpeg.Example.Example
{
    public class AudioTranscode
    {
        public AudioTranscode(string input, string output)
        {
            using (MediaWriter writer = new MediaWriter(output))
            using (MediaReader reader = new MediaReader(input))
            {
                int audioIndex = reader.First(_ => _.Codec.Type == AVMediaType.AVMEDIA_TYPE_AUDIO).Index;

                var dstChannels = reader[audioIndex].Codec.AVCodecContext.channels;
                var dstSampleRate = reader[audioIndex].Codec.AVCodecContext.sample_rate;
                writer.AddStream(MediaEncode.CreateAudioEncode(writer.Format, dstChannels, dstSampleRate));
                writer.Initialize();

                AudioFrame dst = AudioFrame.CreateFrameByCodec(writer[0].Codec);
                SampleConverter converter = new SampleConverter(dst);
                long i = 0;
                foreach (var packet in reader.ReadPacket())
                {
                    foreach (var srcframe in reader[audioIndex].ReadFrame(packet))
                    {
                        foreach (var dstframe in converter.Convert(srcframe))
                        {
                            i += dstframe.NbSamples;
                            dstframe.Pts = i;
                            foreach (var outpacket in writer[0].WriteFrame(dstframe))
                            {
                                writer.WritePacket(outpacket);
                            }
                        }
                    }
                }
                writer.FlushMuxer();
            }
        }
    }
}