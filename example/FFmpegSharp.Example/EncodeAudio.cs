using System;
using System.IO;
using System.Linq;
using FFmpeg.AutoGen;

namespace FFmpegSharp.Example
{
    public class EncodeAudio : ExampleBase
    {
        public EncodeAudio() : this("path-to-your.mp2")
        { }

        public EncodeAudio(params string[] args) : base(args)
        { }

        public unsafe override void Execute()
        {
            var outputFile = args[0];

            using (FileStream os = File.Create(outputFile))
            using (MediaPacket pkt = new MediaPacket())
            {
                var codec = MediaCodec.FindEncoder(AVCodecID.AV_CODEC_ID_MP2);
                var bitrate = 64000;
                var sampleRate = select_sample_rate(codec);
                var chLayout = select_channel_layout(codec);
                var sampleFmt = AVSampleFormat.AV_SAMPLE_FMT_S16;
                if (!codec.GetSampelFmts().Any(_1 => _1 == AVSampleFormat.AV_SAMPLE_FMT_S16))
                    Console.WriteLine($"Encoder does not support sample format {AVSampleFormat.AV_SAMPLE_FMT_S16.GetName()}");
                using (var encoder = MediaEncoder.CreateAudioEncoder(codec, sampleRate, chLayout, sampleFmt, bitrate))
                using (var frame = MediaFrame.CreateAudioFrame(encoder.Context.ChLayout, encoder.Context.FrameSize, encoder.Context.SampleFmt))
                {
                    double t, tincr;
                    for (int i = 0; i < 25; i++)
                    {
                        AVFrame* pframe = frame;
                        AVCodecContext* c = encoder.Context;
                        /* encode a single tone sound */
                        t = 0;
                        tincr = 2 * Math.PI * 440.0 / c->sample_rate;
                        for (i = 0; i < 200; i++)
                        {
                            ushort* samples = (ushort*)(void*)pframe->data[0];
                            for (var j = 0; j < c->frame_size; j++)
                            {
                                samples[2 * j] = (ushort)(Math.Sin(t) * 10000);
                                for (var k = 1; k < c->ch_layout.nb_channels; k++)
                                    samples[2 * j + k] = samples[2 * j];
                                t += tincr;
                            }
                            foreach (var item in encoder.EncodeFrame(frame, pkt))
                            {
                                os.Write(new ReadOnlySpan<byte>(item.Ref.data, item.Ref.size));
                            }
                        }
                    }
                    foreach (var item in encoder.EncodeFrame(null, pkt))
                    {
                        os.Write(new ReadOnlySpan<byte>(item.Ref.data, item.Ref.size));
                    }
                }
            }
        }

        /* just pick the highest supported samplerate */
        static int select_sample_rate(MediaCodec codec)
        {
            int best_samplerate = 0;

            var srs = codec.GetSupportedSamplerates();
            if (!srs.Any())
                return 44100;

            foreach (var p in srs)
            {
                if (best_samplerate == 0 || Math.Abs(44100 - p) < Math.Abs(44100 - best_samplerate))
                    best_samplerate = p;
            }
            return best_samplerate;
        }

        /* select layout with the highest channel count */
        static AVChannelLayout select_channel_layout(MediaCodec codec)
        {

            if (!codec.GetChLayouts().Any())
            {
                var a = new AVChannelLayout { nb_channels = 2, order = AVChannelOrder.AV_CHANNEL_ORDER_NATIVE };
                a.u.mask = ffmpeg.AV_CH_LAYOUT_STEREO;
                return a;
            }

            int best_nb_channels = 0;
            AVChannelLayout best_ch_layout = default;
            foreach (var p in codec.GetChLayouts())
            {
                int nb_channels = p.nb_channels;

                if (nb_channels > best_nb_channels)
                {
                    best_ch_layout = p;
                    best_nb_channels = nb_channels;
                }
            }
            return best_ch_layout;
        }
    }
}
