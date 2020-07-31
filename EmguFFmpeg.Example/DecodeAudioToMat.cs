using Emgu.CV;

using EmguFFmpeg.EmgucvExtern;

using FFmpeg.AutoGen;

using System.Linq;

namespace EmguFFmpeg.Example
{
    public class DecodeAudioToMat  
    {
        public DecodeAudioToMat(string inputFile)
        {
            using (MediaReader reader = new MediaReader(inputFile))
            {
                foreach (var packet in reader.ReadPacket())
                {
                    // audio maybe have one more stream, e.g. 0 is mp3 audio, 1 is mpeg cover
                    var audioIndex = reader.Where(_ => _.Codec.AVCodecContext.codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO).First().Index;

                    AudioFrame audioFrame = new AudioFrame(AVSampleFormat.AV_SAMPLE_FMT_S16P, 2, 1024, 44100);
                    SampleConverter converter = new SampleConverter(audioFrame);

                    foreach (var frame in reader[audioIndex].ReadFrame(packet))
                    {
                        Mat mat = frame.ToMat();
                    }
                }
            }
        }
    }
}