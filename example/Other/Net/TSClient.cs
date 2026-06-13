using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example.Other.Net
{
    /// <summary>
    /// Capture the desktop with gdigrab and stream it as MPEG-TS over UDP.
    /// </summary>
    internal class TSClient : ExampleBase
    {
        public TSClient() { Index = 35; Enable = false; }

        public override void Execute()
        {
            using BlockingCollection<MediaFrame> frames = new BlockingCollection<MediaFrame>(boundedCapacity: 8);
            var encodeLoop = new EncodeLoop(frames, 800, 600, 30d, "udp://localhost:8888");
            var encodeTask = encodeLoop.Start();

            foreach (var f in GdiGrabLoop.GetFrames())
            {
                // The decoder reuses the yielded frame between iterations — hand the consumer its own reference.
                frames.Add(f.Clone());
            }
            frames.CompleteAdding();
            encodeTask.Wait();
        }
    }


    public class GdiGrabLoop
    {
        public static IEnumerable<MediaFrame> GetFrames()
        {
            using var demuxer = MediaDemuxer.Open("desktop", MediaInputFormat.FindFormat("gdigrab"));
            using var decoder = new MediaDecoder(MediaCodec.FindDecoder(demuxer[0].CodecparRef.codec_id));
            decoder.SetCodecParameters(ref demuxer[0].CodecparRef);
            decoder.Ref.thread_count = 0;
            decoder.Open();
            foreach (var pkt in demuxer.ReadPackets())
            {
                if (pkt.Ref.stream_index != 0) continue;
                foreach (var frame in decoder.DecodePacket(pkt))
                {
                    yield return frame;
                }
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

        public Task Start() => Task.Run(() =>
        {
            using var muxer = MediaMuxer.Create(_dst, MediaOutputFormat.GuessFormat("mpegts", null, null));
            using var vEncoder = MediaEncoder.Video()
                .OutputFormat(muxer.Format)
                .Size(_width, _height)
                .Fps(_fps)
                .Configure(_ => _.Ref.thread_count = 0)
                .Build();
            muxer.AddStream(vEncoder);
            muxer.WriteHeader();

            // gdigrab delivers BGRA desktop-sized frames — scale/convert to the encoder's format.
            using var sws = new Swscale();
            using var dst = MediaFrame.CreateVideoFrame(_width, _height, vEncoder.Ref.pix_fmt);

            long pts = 0;
            foreach (var frame in _frames.GetConsumingEnumerable())
            {
                using (frame)
                {
                    dst.MakeWritable(); // the encoder may still hold a reference to dst
                    sws.Convert(frame, dst);
                    dst.Ref.pts = pts++;
                    foreach (var packet in vEncoder.EncodeFrame(dst))
                    {
                        muxer.WritePacket(packet, vEncoder.Ref.time_base);
                    }
                }
            }
            muxer.FlushCodecs([vEncoder]);
            muxer.WriteTrailer();
        });
    }
}
