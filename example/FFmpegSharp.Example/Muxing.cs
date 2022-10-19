using System.Linq;
using FFmpeg.AutoGen;

namespace FFmpegSharp.Example
{
    internal class Muxing : ExampleBase
    {
        public Muxing() : this($"Muxing-output.mp4")
        { }

        public Muxing(params string[] args) : base(args)
        { }

        public override void Execute()
        {
            var filename = args[0];

            var oc = MediaMuxer.Create(filename);

            var fmt = oc.Format;

            /* Add the audio and video streams using the default format codecs
             * and initialize the codecs. */
            if (fmt.AudioCodec != AVCodecID.AV_CODEC_ID_NONE)
            {
                var codec = MediaCodec.FindDecoder(fmt.AudioCodec);
                // audio
                var abitrate = 64000;
                var samplefmt = codec.GetSampelFmts().Any() ? codec.GetSampelFmts().First() : AVSampleFormat.AV_SAMPLE_FMT_FLTP;
                var samplerate = codec.GetSupportedSamplerates().Any() ? codec.GetSupportedSamplerates().First() : 44100;
                oc.AddStream(MediaEncoder.CreateAudioEncoder(fmt, samplerate, 2.ToDefaultChLayout(), samplefmt, abitrate));
            }
            if (fmt.VideoCodec != AVCodecID.AV_CODEC_ID_NONE)
            {
                // video
                var codec = MediaCodec.FindDecoder(fmt.VideoCodec);
                var vbitrate = 400000;
                var width = 352;
                var height = 288;
                var fps = 25;
                var pixfmt = AVPixelFormat.AV_PIX_FMT_YUV420P;

                oc.AddStream(MediaEncoder.CreateVideoEncoder(fmt, width, height, fps, pixfmt, vbitrate, _ =>
                {
                    _.GopSize = 12;
                    if (_.CodecId == AVCodecID.AV_CODEC_ID_MPEG2VIDEO)
                        _.MaxBFrames = 2;
                    if (_.CodecId == AVCodecID.AV_CODEC_ID_MPEG1VIDEO)
                        _.MbDecision = 2;
                }));
            }


            oc.DumpFormat();



        }
    }
}
