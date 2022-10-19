using System.Collections.Generic;
using System.Linq;

namespace FFmpegSharp.Example
{
    internal class Transcoding : ExampleBase
    {
        public Transcoding() : this($"video-input.mp4", $"{nameof(Transcoding)}-output.avi")
        { }

        public Transcoding(params string[] args) : base(args)
        { }

        public unsafe override void Execute()
        {
            var input = args[0];
            var output = args[1];
            using (var mr = MediaDemuxer.Open(input))
            //using (var mw = MediaMuxer.Create(output))
            {
                MediaCodec a = null;
                mr.FindBestStream(FFmpeg.AutoGen.AVMediaType.AVMEDIA_TYPE_AUDIO, ref a);
                var decodecs = mr.Select(_ => MediaDecoder.CreateDecoder(_.CodecparRef)).ToList();
                var encodecs = new List<MediaCodecContext>();

                decodecs.ForEach(_ => _?.Dispose());
                encodecs.ForEach(_ => _?.Dispose());
            }
        }
    }
}
