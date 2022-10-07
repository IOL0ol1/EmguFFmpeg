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
            var d1 = new MediaDictionary();
            d1.Add("111", "asdgfgh");
            d1.Add("1232", "asdfg");

            var d2 = d1.Copy();
            var a = d2["111"];
            d2["111"] = "sadf";
            var b = d1["111"];



            using (var muxer = new MediaWriter(@"E:\path-to-your.mp4"))
            {
                using (var vEncoder = MediaEncoder.CreateVideoEncoder(muxer.Format, 800, 600, 30))
                {
                    AVStream* vStream = muxer.AddStream(vEncoder);

                    //    muxer.WriteHeader();

                    //    using (var vFrame = MediaFrame.CreateVideoFrame(800, 600, vEncoder.PixFmt))
                    //    {
                    //        for (int i = 0; i < 30; i++)
                    //        {
                    //            ffmpeg.av_frame_make_writable(vFrame);
                    //            FillYuv420P(vFrame, i);
                    //            foreach (var packet in vEncoder.EncodeFrame(vFrame))
                    //            {
                    //                packet.StreamIndex = vStream->index;
                    //                muxer.WritePacket(packet, vEncoder.AVCodecContext.time_base);
                    //            }
                    //        }
                    //    }
                    //    muxer.FlushCodecs(new[] { vEncoder });
                    muxer.WriteHeader();
                    muxer.WriteHeader();
                    muxer.WriteHeader();
                    muxer.WriteHeader();
                    muxer.WriteTrailer();
                    muxer.WriteTrailer();
                    muxer.WriteTrailer();
                }
            }

            using (var demuxer = new MediaReader(@"E:\path-to-your.mp4"))
            {
                var decoders = demuxer.Select(_ => MediaDecoder.CreateDecoderByCodecpar(_.CodecparSafe)).ToList();
                foreach (var packet in demuxer.ReadPackets())
                {
                    if (decoders[packet.StreamIndex] != null)
                    {
                        foreach (var frame in decoders[packet.StreamIndex].DecodePacket(packet))
                        {
                            Trace.TraceInformation($"{decoders[packet.StreamIndex].CodecType}\t{frame.PktDts}\t{frame.Height}\t{frame.Width}\t{frame.NbSamples}");
                        }
                    }
                }
                decoders.ForEach(_ => _?.Dispose());
            }

            using (var muxer = new MediaWriter(@"E:\path-to-your.mp2"))
            {
                var ch = AVChannelLayoutExtension.Default(2);
                using (var audio = MediaEncoder.CreateAudioEncoder(muxer.Format, 44100, ch))
                {
                    AVStream* aStream = muxer.AddStream(audio);

                    muxer.WriteHeader();

                    var pts = 0;
                    using (var aFrame = MediaFrame.CreateAudioFrame(ch.nb_channels, audio.FrameSize, audio.SampleFmt))
                    {
                        /* encode a single tone sound */
                        var t = 0d;
                        var tincr = 2 * Math.PI * 440.0 / audio.SampleRate;
                        for (int i = 0; i < 200; i++)
                        {

                            UInt16* samples = (UInt16*)((AVFrame*)aFrame)->data[0];

                            for (int j = 0; j < audio.FrameSize; j++)
                            {
                                samples[2 * j] = (UInt16)(Math.Sin(t) * 10000);

                                for (int k = 1; k < audio.ChLayout.nb_channels; k++)
                                    samples[2 * j + k] = samples[2 * j];
                                t += tincr;
                            }
                            pts += aFrame.NbSamples;
                            aFrame.Pts = pts;
                            foreach (var packet in audio.EncodeFrame(aFrame))
                            {
                                muxer.WritePacket(packet);
                            }
                        }
                    }
                    muxer.FlushCodecs(new[] { audio });
                }
            }



        }


        /// <summary>
        /// Fill frame
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="i"></param>
        private static unsafe void FillYuv420P(MediaFrame frame, long i)
        {
            int linesize0 = frame.Linesize[0];
            int linesize1 = frame.Linesize[1];
            int linesize2 = frame.Linesize[2];

            byte* data0 = (byte*)frame.Data[0];
            byte* data1 = (byte*)frame.Data[1];
            byte* data2 = (byte*)frame.Data[2];

            /* prepare a dummy image */
            /* Y */
            for (int y = 0; y < frame.Height; y++)
            {
                for (int x = 0; x < frame.Width; x++)
                {
                    data0[y * linesize0 + x] = (byte)(x + y + i * 3);
                }
            }

            /* Cb and Cr */
            for (int y = 0; y < frame.Height / 2; y++)
            {
                for (int x = 0; x < frame.Width / 2; x++)
                {
                    data1[y * linesize1 + x] = (byte)(128 + y + i * 2);
                    data2[y * linesize2 + x] = (byte)(64 + x + i * 5);
                }
            }

            frame.Pts = i;
        }
    }
}

