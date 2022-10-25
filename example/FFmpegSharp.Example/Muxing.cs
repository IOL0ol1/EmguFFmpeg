using System.Collections.Generic;
using System.IO;
using System.Linq;
using FFmpeg.AutoGen;

namespace FFmpegSharp.Example
{
    internal class Muxing : ExampleBase
    {
        public Muxing() : this($"Muxing-output.mp4")
        { 
        }

        public Muxing(params string[] args) : base(args)
        { }

        public override void Execute()
        {
            var filename = args[0];

            using (var oc = MediaMuxer.Create(File.OpenWrite(filename), OutFormat.GuessFormat(null, filename, null)))
            {
                var fmt = oc.Format;
                var encoders = new List<MediaEncoder>();

                /* Add the audio and video streams using the default format codecs
                 * and initialize the codecs. */
                if (fmt.AudioCodec != AVCodecID.AV_CODEC_ID_NONE)
                {
                    var codec = MediaCodec.FindDecoder(fmt.AudioCodec);
                    // audio
                    var abitrate = 64000;
                    var samplefmt = codec.GetSampelFmts().Any() ? codec.GetSampelFmts().First() : AVSampleFormat.AV_SAMPLE_FMT_FLTP;
                    var samplerate = codec.GetSupportedSamplerates().Any() ? codec.GetSupportedSamplerates().First() : 44100;
                    var encoder = MediaEncoder.CreateAudioEncoder(fmt, samplerate, 2.ToDefaultChLayout(), samplefmt, abitrate);
                    encoders.Add(encoder);
                    oc.AddStream(encoder);
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
                    var encoder = MediaEncoder.CreateVideoEncoder(fmt, width, height, fps, pixfmt, vbitrate, _ =>
                    {
                        _.GopSize = 12;
                        if (_.CodecId == AVCodecID.AV_CODEC_ID_MPEG2VIDEO)
                            _.MaxBFrames = 2;
                        if (_.CodecId == AVCodecID.AV_CODEC_ID_MPEG1VIDEO)
                            _.MbDecision = 2;
                    });
                    encoders.Add(encoder);
                    oc.AddStream(encoder);
                }


                oc.DumpFormat();

                encoders.ForEach(_ => _?.Dispose());
            }

        }
    }
}
