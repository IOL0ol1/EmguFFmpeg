namespace FFmpegSharp.Example
{
    internal class HWDecode : ExampleBase
    {
        public HWDecode() : base("","video-input.mp4","HWDecode-output.bin")
        { }

        public override void Execute()
        {
            using (var md = MediaDemuxer.Open(args[0]))
            using (var packet = new MediaPacket())
            using (var frame = new MediaFrame())
            using (var sw_frame = new MediaFrame())
            {
                MediaCodec decoder = null;
                var a = md.FindBestStream(FFmpeg.AutoGen.AVMediaType.AVMEDIA_TYPE_VIDEO, ref decoder);
                var vEncoder = MediaCodecContext.Create(decoder, _ =>
                {
                    //decoder.GetHWConfigs();
                });
            }
        }
    }
}
