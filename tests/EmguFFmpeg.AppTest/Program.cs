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
            using (var muxer = new MediaWriter(@"E:\path-to-your.mp4"))
            {
                using (var video = MediaEncoder.CreateVideoEncoder(muxer.Format, 800, 600, 30))
                {
                    AVStream* vStream = muxer.AddStream(video);

                    muxer.WriteHeader();

                    using (var vFrame = MediaFrame.CreateVideoFrame(800, 600, video.PixFmt))
                    {
                        for (int i = 0; i < 3000; i++)
                        {
                            ffmpeg.av_frame_make_writable(vFrame);
                            FillYuv420P(vFrame, i);
                            foreach (var packet in video.EncodeFrame(vFrame))
                            {
                                packet.StreamIndex = vStream->index;
                                muxer.WritePacket(packet);
                            }
                        }

                        ///* encode a single tone sound */
                        //var t = 0d;
                        //var tincr = 2 * Math.PI * 440.0 / audio.SampleRate;
                        //for (int i = 0; i < 200; i++)
                        //{
                        //    ffmpeg.av_frame_make_writable(aFrame);
                        //    AVFrame* frame = aFrame;
                        //    byte* samples = frame->data[0];

                        //    for (int j = 0; j < audio.AVCodecContext.frame_size; j++)
                        //    {
                        //        samples[2 * j] = (byte)(Math.Sin(t) * 10000);

                        //        for (int k = 1; k < audio.AVCodecContext.ch_layout.nb_channels; k++)
                        //            samples[2 * j + k] = samples[2 * j];
                        //        t += tincr;
                        //    }
                        //    foreach (var packet in audio.EncodeFrame(aFrame))
                        //    {
                        //        packet.StreamIndex = aStream->index;
                        //        muxer.WritePacket(packet);
                        //    }
                        //}
                    }
                    muxer.WriteTrailer(new[] { video });
                }
            }

            using (var demuxer = new MediaReader(@"E:\path-to-your.mp4"))
            {
                var codecs = demuxer.Select(_ => _.CreateDefaultCodecContext()).ToList();
                foreach (var packet in demuxer.ReadPackets())
                {
                    if (codecs[packet.StreamIndex] != null)
                    {
                        foreach (var frame in codecs[packet.StreamIndex].DecodePacket(packet))
                        {
                            Trace.TraceInformation($"{codecs[packet.StreamIndex].CodecType}\t{frame.Pts}\t{frame.Height}\t{frame.Width}\t{frame.NbSamples}");
                        }
                    }
                }
                codecs.ForEach(_ => _?.Dispose());
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

