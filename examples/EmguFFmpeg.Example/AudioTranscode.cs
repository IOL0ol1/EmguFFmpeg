using System.Linq;
using FFmpeg.AutoGen;

namespace EmguFFmpeg.Example
{
    public class AudioTranscode
    {
        /// <summary>
        /// transcode audio
        /// </summary>
        /// <param name="input">input audio file</param>
        /// <param name="output">output audio file</param>
        /// <param name="outChannels">output audio file channels</param>
        /// <param name="outSampleRate">output audio file sample rate</param>
        public AudioTranscode(string input, string output, int outChannels = 2, int outSampleRate = 44100)
        {
            using (MediaWriter writer = new MediaWriter(output))
            using (MediaReader reader = new MediaReader(input))
            {
                int audioIndex = reader.First(_ => _.Codec.Type == AVMediaType.AVMEDIA_TYPE_AUDIO).Index;

                writer.AddStream(MediaEncoder.CreateAudioEncode(writer.Format, outChannels, outSampleRate));
                writer.Initialize();

                AudioFrame dst = AudioFrame.CreateFrameByCodec(writer[0].Codec);
                SampleConverter converter = new SampleConverter(dst);
                long pts = 0;
                foreach (var packet in reader.ReadPacket())
                {
                    foreach (var srcframe in reader[audioIndex].ReadFrame(packet))
                    {
                        foreach (var dstframe in converter.Convert(srcframe))
                        {
                            pts += dstframe.AVFrame.nb_samples;
                            dstframe.Pts = pts; // audio's pts is total samples, pts can only increase.
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