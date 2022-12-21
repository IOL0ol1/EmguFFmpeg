using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using Microsoft.VisualBasic;
using OpenCvSharp;

namespace FFmpegSharp.Example.Other.Net
{
    internal class TSClient : ExampleBase
    {
        public override async void Execute()
        {

            BlockingCollection<MediaFrame> frames = new BlockingCollection<MediaFrame>();
            EncodeLoop encodeLoop = new EncodeLoop(frames, 800, 600, 30d, "udp://localhost:8888");
            encodeLoop.Start();

            var fs = new GdiGrabLoop().GetFrames();
            foreach (var f in fs)
            {
                frames.Add(f);
            }
        }
    }


    public class GdiGrabLoop
    {
        public GdiGrabLoop()
        {

        }


        public IEnumerable<MediaFrame> GetFrames()
        {
            using (var demuxer = MediaDemuxer.Open("desktop",InFormat.Get("gdigrab")))
            {
                var v = demuxer.Select(_ => MediaDecoder.CreateDecoder(_.CodecparRef, _ => _.ThreadCount = 10)).ToList();
                foreach (var pkt in demuxer.ReadPackets())
                {
                    if(pkt.StreamIndex == 0)
                    {
                        foreach (var frame in v[0].DecodePacket(pkt))
                        {
                            yield return frame;
                        }
                    }
                }
                v.ForEach(_ => _?.Dispose());
            }
        }
    }


    public class EncodeLoop
    {
        private readonly BlockingCollection<MediaFrame> _frames;
        private readonly string _dst;
        private readonly int _width;
        private readonly int _height;
        private readonly double _fps; 
        public EncodeLoop(
            BlockingCollection<MediaFrame> frames,
            int width,
            int height,
            double fps, 
            string dst)
        {
            _frames = frames;
            _width = width;
            _height = height;
            _fps = fps; 
            _dst = dst;
        }



        public async void Start()
        {
            await Task.Run(() =>
            {
                using (var muxer = MediaMuxer.Create(_dst, OutFormat.Get("mpegts")))
                using (var vEncoder = MediaEncoder.CreateVideoEncoder(muxer.Format, _width, _height, _fps, otherSettings: _ => _.ThreadCount = 10))
                {
                    muxer.AddStream(vEncoder);
                    muxer.WriteHeader();

                    MediaFrame frame;
                    while ((frame = _frames.Take()) != null)
                    {
                        foreach (var packet in vEncoder.EncodeFrame(frame))
                        {
                            muxer.WritePacket(packet);
                        }
                        frame.Dispose();
                    }
                    muxer.FlushCodecs(new[] { vEncoder });
                    muxer.WriteTrailer();
                }
            });
        }

    }

}
