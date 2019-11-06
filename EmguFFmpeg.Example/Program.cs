using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace EmguFFmpeg.Example
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // copy ffmpeg binarys to ./bin
            FFmpegHelper.RegisterBinaries();
            FFmpegHelper.SetupLogging();
            Console.WriteLine("Hello FFmpeg!");

            using (MediaWriter writer = new MediaWriter(@"C:\Users\Admin\Desktop\output.mp3"))
            using (MediaReader reader = new MediaReader(@"C:\Users\Admin\Desktop\input.mp3"))
            {
                writer.AddStream(MediaEncode.CreateAudioEncode(writer.Format, AVChannelLayout.AV_CH_LAYOUT_STEREO));
                writer.Initialize();

                AudioFrame dstFrame = AudioFrame.CreateFrameByCodec(writer[0].Codec);
                AudioFrameConverter converter = new AudioFrameConverter(dstFrame);
                foreach (var packet in reader.ReadPacket())
                {
                    int audioIndex = reader.First(_ => _.Codec.Type == AVMediaType.AVMEDIA_TYPE_AUDIO).Index;
                    foreach (var frame in reader[audioIndex].ReadFrame(packet))
                    {
                        foreach (var outframe in converter.Convert3(frame))
                        {
                            foreach (var outpacket in writer[0].WriteFrame(outframe))
                            {
                                writer.WritePacket(outpacket);
                            }
                        }
                    }
                }
                writer.FlushMuxer();
            }

            // No media files provided
            new List<IExample>()
            {
                new DecodeAudio("input.mp3"),
                new CreateVideo("output.mp4"),
                new ReadDevice(),
                new Remuxing("input.mp3"),
                new RtmpPull("rtmp://127.0.0.0/live/stream"),
            }.ForEach(_ =>
            {
                try
                {
                    _.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            });

            Console.ReadKey();
        }
    }
}