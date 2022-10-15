using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpegSharp.AppTest
{
    internal class Transcoding : ExampleBase
    {
        public Transcoding(string input, string output)
        {
            parames.Add("input", input);
            parames.Add("output", output);
        }

        public unsafe override void Execute()
        {
            using (var mr = MediaDemuxer.Open(GetParame<string>("input")))
            using (var mw = MediaMuxer.Create(GetParame<string>("output")))
            {
                var decodecs = mr.Select(_ => MediaDecoder.CreateDecoder(_.CodecparRef)).ToList();
                var encodecs = new List<MediaCodecContext>();
            
                decodecs.ForEach(_ => _?.Dispose());
                encodecs.ForEach(_ => _?.Dispose());
            }
        }
    }
}
