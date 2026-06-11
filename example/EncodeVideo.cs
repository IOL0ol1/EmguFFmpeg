using System;
using System.IO;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: encode_video.c
    /// Encode synthetic YUV420P frames to a video file using a named codec (e.g. "mpeg1video" or "h264").
    /// </summary>
    public unsafe class EncodeVideo : ExampleBase
    {
        public EncodeVideo() { Index = 6; Enable = false; }

        public override void Execute()
        {
            var outFile   = args.Length > 0 ? args[0] : "out.mpg";
            var codecName = args.Length > 1 ? args[1] : "mpeg1video";

            var codec = MediaCodec.FindEncoder(codecName);
            if (codec == null) throw new Exception($"Codec '{codecName}' not found");

            using var encoder = MediaEncoder.Create(codec, ctx =>
            {
                ctx.Ref.bit_rate  = 400_000;
                ctx.Ref.width     = 352;
                ctx.Ref.height    = 288;
                ctx.Ref.time_base = new AVRational { num = 1, den = 25 };
                ctx.Ref.framerate = new AVRational { num = 25, den = 1 };
                ctx.Ref.gop_size  = 10;
                ctx.Ref.max_b_frames = 1;
                ctx.Ref.pix_fmt   = AVPixelFormat.AV_PIX_FMT_YUV420P;

                if (codec.Id == AVCodecID.AV_CODEC_ID_H264)
                    MediaOptions.Set(ctx.Ref.priv_data, "preset", "slow", 0);
            });

            using var frame  = MediaFrame.CreateVideoFrame(encoder.Ref.width, encoder.Ref.height, encoder.Ref.pix_fmt);
            using var packet = new MediaPacket();

            using var outStream = File.OpenWrite(outFile);

            int w = encoder.Ref.width, h = encoder.Ref.height;

            for (int i = 0; i < 25; i++)
            {
                frame.MakeWritable();

                // Fill Y plane.
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        frame.Ref.data[0][y * frame.Ref.linesize[0] + x] = (byte)(x + y + i * 3);

                // Fill Cb/Cr planes.
                for (int y = 0; y < h / 2; y++)
                    for (int x = 0; x < w / 2; x++)
                    {
                        frame.Ref.data[1][y * frame.Ref.linesize[1] + x] = (byte)(128 + y + i * 2);
                        frame.Ref.data[2][y * frame.Ref.linesize[2] + x] = (byte)(64 + x + i * 5);
                    }

                frame.Ref.pts = i;
                Console.WriteLine($"Send frame {frame.Ref.pts,3}");

                WriteEncodedPackets(encoder, frame, packet, outStream);
            }

            // Flush encoder.
            WriteEncodedPackets(encoder, null, packet, outStream);

            // For raw MPEG streams, append the sequence end code.
            var id = codec.Id;
            if (id == AVCodecID.AV_CODEC_ID_MPEG1VIDEO || id == AVCodecID.AV_CODEC_ID_MPEG2VIDEO)
                outStream.Write(new byte[] { 0x00, 0x00, 0x01, 0xB7 });
        }

        private static void WriteEncodedPackets(MediaEncoder encoder, MediaFrame frame, MediaPacket packet, Stream outStream)
        {
            foreach (var pkt in encoder.EncodeFrame(frame, packet))
            {
                Console.WriteLine($"Write packet {pkt.Ref.pts,3} (size={pkt.Ref.size,5})");
                outStream.Write(new ReadOnlySpan<byte>(pkt.Ref.data, pkt.Ref.size));
            }
        }
    }
}
