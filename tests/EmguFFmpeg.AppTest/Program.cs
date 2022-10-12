using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FFmpeg.AutoGen;


namespace EmguFFmpeg.AppTest
{
    internal unsafe class Program
    {
        private static void Main(string[] args)
        {
            new CreateMPEG4().Execute();
            new EncodeAudio().Execute();
            new EncodeVideo().Execute();
            new DecodeAudio().Execute();

            new AvioReading().Execute();




            //using (var demuxer = new MediaDemuxer(@"E:\path-to-your.mp4"))
            //using (var muxer = new MediaMuxer(@"E:\path-to-your.avi"))
            //{
            //    var decoders = demuxer.Select(_ => MediaDecoder.CreateDecoder(_.CodecparPoint)).ToList();
            //    decoders.ForEach(_ => muxer.AddStream(_));

            //    foreach (var packet in demuxer.ReadPackets())
            //    {
            //        if (decoders[packet.StreamIndex] != null)
            //        {
            //            foreach (var frame in decoders[packet.StreamIndex].DecodePacket(packet))
            //            {

            //            }
            //        }
            //    }
            //    decoders.ForEach(_ => _?.Dispose());
            //}

            //using (var muxer = new MediaMuxer(@"E:\path-to-your.mp2"))
            //{
            //    var ch = AVChannelLayoutExtension.Default(2);
            //    using (var audio = MediaEncoder.CreateAudioEncoder(muxer.Format, 44100, ch))
            //    {
            //        AVStream* aStream = muxer.AddStream(audio);

            //        muxer.WriteHeader();

            //        var pts = 0;
            //        using (var aFrame = MediaFrame.CreateAudioFrame(ch.nb_channels, audio.Context.FrameSize, audio.Context.SampleFmt))
            //        {
            //            /* encode a single tone sound */
            //            var t = 0d;
            //            var tincr = 2 * Math.PI * 440.0 / audio.Context.SampleRate;
            //            for (int i = 0; i < 200; i++)
            //            {

            //                UInt16* samples = (UInt16*)((AVFrame*)aFrame)->data[0];

            //                for (int j = 0; j < audio.Context.FrameSize; j++)
            //                {
            //                    samples[2 * j] = (UInt16)(Math.Sin(t) * 10000);

            //                    for (int k = 1; k < audio.Context.ChLayout.nb_channels; k++)
            //                        samples[2 * j + k] = samples[2 * j];
            //                    t += tincr;
            //                }
            //                pts += aFrame.NbSamples;
            //                aFrame.Pts = pts;
            //                foreach (var packet in audio.EncodeFrame(aFrame))
            //                {
            //                    muxer.WritePacket(packet);
            //                }
            //            }
            //        }
            //        muxer.FlushCodecs(new[] { audio });
            //    }
            //}



        }



    }
}

