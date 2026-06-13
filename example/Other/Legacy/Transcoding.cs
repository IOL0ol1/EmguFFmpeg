using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example.Legacy
{
    /// <summary>
    /// Minimal video transcode: demux → decode → swscale → re-encode with the output
    /// container's default video codec (audio/other streams are dropped).
    /// For the full filter-graph pipeline including audio, see <see cref="Transcode"/>.
    /// </summary>
    internal class Transcoding : ExampleBase
    {
        public Transcoding() : this($"video-input.mp4", $"{nameof(Transcoding)}-output.avi")
        { Index = 49; Enable = false; }

        public Transcoding(params string[] args) : base(args)
        { }

        public override void Execute()
        {
            var input = args[0];
            var output = args[1];

            using var demuxer = MediaDemuxer.Open(input);
            MediaCodec vCodec = null;
            int vIndex = demuxer.FindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, ref vCodec);
            if (vIndex < 0) throw new Exception("No video stream found");

            using var decoder = MediaDecoder.CreateDecoder(demuxer[vIndex].CodecparRef);
            var fps = demuxer.GuessFrameRate(demuxer[vIndex]);

            using var muxer = MediaMuxer.Create(output);
            using var encoder = MediaEncoder.Video()
                .OutputFormat(muxer.Format)
                .Size(decoder.Ref.width, decoder.Ref.height)
                .Fps(fps)
                .Build();
            muxer.AddStream(encoder);
            muxer.WriteHeader();

            using var sws = new Swscale();
            using var dst = MediaFrame.CreateVideoFrame(encoder.Ref.width, encoder.Ref.height, encoder.Ref.pix_fmt);
            using var packet = new MediaPacket();
            using var frame = new MediaFrame();

            long pts = 0;
            void EncodeOne(MediaFrame decoded)
            {
                dst.MakeWritable(); // the encoder may still hold a reference to dst
                sws.Convert(decoded, dst);
                dst.Ref.pts = pts++;
                foreach (var p in encoder.EncodeFrame(dst))
                    muxer.WritePacket(p, encoder.Ref.time_base); // auto-rescales encoder.time_base → stream time_base
            }

            foreach (var pkt in demuxer.ReadPackets(packet))
            {
                if (pkt.Ref.stream_index != vIndex) continue;
                foreach (var decoded in decoder.DecodePacket(pkt, frame))
                    EncodeOne(decoded);
            }
            // flush decoder, then encoder
            foreach (var decoded in decoder.DecodePacket(null, frame))
                EncodeOne(decoded);
            muxer.FlushCodecs(new[] { encoder });
            muxer.WriteTrailer();

            Console.WriteLine($"Transcoding complete: '{input}' → '{output}'");
        }
    }
}
