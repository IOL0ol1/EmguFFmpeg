using Emgu.CV.Structure;
using EmguFFmpeg.EmguCV;
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

                    AudioFrame audioFrame = new AudioFrame(AVSampleFormat.AV_SAMPLE_FMT_DBL, 2, 1024, 44100);
                    SampleConverter converter = new SampleConverter(audioFrame);

                    foreach (var frame in reader[audioIndex].ReadFrame(packet))
                    {
                        var b = frame.GetData();
                        foreach (var item in converter.Convert(frame))
                        {
                            var a = item.ToMat();
                            var c = item.GetData();
                        }
                    }
                }
            }
        }
    }
}