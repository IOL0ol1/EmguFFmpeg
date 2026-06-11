using System;
using System.IO;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: demux_decode.c
    /// Demux an input container, decode video and audio streams, write raw frames to output files.
    /// </summary>
    public unsafe class DemuxDecode : ExampleBase
    {
        public DemuxDecode() { Index = 7; Enable = false; }

        public override void Execute()
        {
            var inFile        = args.Length > 0 ? args[0] : "input.mp4";
            var videoOutFile  = args.Length > 1 ? args[1] : "out_video.raw";
            var audioOutFile  = args.Length > 2 ? args[2] : "out_audio.raw";

            using var demuxer = MediaDemuxer.Open(inFile);
            demuxer.DumpFormat();

            // Find best video and audio streams.
            MediaCodec videoCodec = null, audioCodec = null;
            int videoStreamIdx = demuxer.FindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, ref videoCodec);
            int audioStreamIdx = demuxer.FindBestStream(AVMediaType.AVMEDIA_TYPE_AUDIO, ref audioCodec);

            MediaDecoder videoDecoder = null, audioDecoder = null;

            if (videoStreamIdx >= 0 && videoCodec != null)
            {
                videoDecoder = MediaDecoder.CreateDecoder(
                    demuxer[videoStreamIdx].CodecparRef);
                Console.WriteLine($"Demuxing video from '{inFile}' into '{videoOutFile}'");
            }

            if (audioStreamIdx >= 0 && audioCodec != null)
            {
                audioDecoder = MediaDecoder.CreateDecoder(
                    demuxer[audioStreamIdx].CodecparRef);
                Console.WriteLine($"Demuxing audio from '{inFile}' into '{audioOutFile}'");
            }

            if (videoDecoder == null && audioDecoder == null)
                throw new Exception("No decodable streams found");

            int videoFrameCount = 0, audioFrameCount = 0;

            // Allocate frame and packet up-front.
            using var frame  = new MediaFrame();
            using var packet = new MediaPacket();

            using var videoOut = videoDecoder != null ? File.OpenWrite(videoOutFile) : null;
            using var audioOut = audioDecoder != null ? File.OpenWrite(audioOutFile) : null;

            // Allocate raw video buffer once we know the format.
            int videoW = 0, videoH = 0;
            AVPixelFormat pixFmt = AVPixelFormat.AV_PIX_FMT_NONE;
            byte[] videoBuf = null;
            int videoBufSize = 0;

            foreach (var pkt in demuxer.ReadPackets(packet))
            {
                if (pkt.Ref.stream_index == videoStreamIdx && videoDecoder != null)
                {
                    DecodeAndWrite(videoDecoder, pkt, frame,
                        decoded =>
                        {
                            // Lazily allocate raw frame buffer.
                            if (videoBufSize == 0)
                            {
                                videoW = decoded.Ref.width;
                                videoH = decoded.Ref.height;
                                pixFmt = (AVPixelFormat)decoded.Ref.format;
                                videoBufSize = decoded.GetBytesSize(padding: false);
                                videoBuf = new byte[videoBufSize];
                            }

                            Console.WriteLine($"video_frame n:{videoFrameCount++}");
                            decoded.GetBytes(videoBuf, padding: false);

                            videoOut.Write(videoBuf, 0, videoBufSize);
                        });
                }
                else if (pkt.Ref.stream_index == audioStreamIdx && audioDecoder != null)
                {
                    DecodeAndWrite(audioDecoder, pkt, frame,
                        decoded =>
                        {
                            int unpadded = decoded.Ref.nb_samples *
                                ((AVSampleFormat)decoded.Ref.format).GetBytesPerSample();
                            Console.WriteLine($"audio_frame n:{audioFrameCount++} nb_samples:{decoded.Ref.nb_samples}");
                            audioOut.Write(new ReadOnlySpan<byte>(decoded.Ref.extended_data[0], unpadded));
                        });
                }
            }

            // Flush decoders.
            if (videoDecoder != null)
                DecodeAndWrite(videoDecoder, null, frame, decoded =>
                {
                    Console.WriteLine($"video_frame n:{videoFrameCount++}");
                    videoOut.Write(videoBuf, 0, videoBufSize);
                });

            if (audioDecoder != null)
                DecodeAndWrite(audioDecoder, null, frame, decoded =>
                {
                    int unpadded = decoded.Ref.nb_samples *
                        ((AVSampleFormat)decoded.Ref.format).GetBytesPerSample();
                    audioOut.Write(new ReadOnlySpan<byte>(decoded.Ref.extended_data[0], unpadded));
                });

            Console.WriteLine("Demuxing succeeded.");

            if (videoDecoder != null)
                Console.WriteLine($"Play video: ffplay -f rawvideo -pixel_format {pixFmt.GetName()} -video_size {videoW}x{videoH} {videoOutFile}");

            if (audioDecoder != null)
            {
                var sfmt = audioDecoder.Ref.sample_fmt;
                if (sfmt.IsPlanar())
                    sfmt = sfmt.ToPacked();
                string fmtStr = sfmt switch
                {
                    AVSampleFormat.AV_SAMPLE_FMT_U8  => "u8",
                    AVSampleFormat.AV_SAMPLE_FMT_S16 => "s16le",
                    AVSampleFormat.AV_SAMPLE_FMT_S32 => "s32le",
                    AVSampleFormat.AV_SAMPLE_FMT_FLT => "f32le",
                    AVSampleFormat.AV_SAMPLE_FMT_DBL => "f64le",
                    _ => "unknown"
                };

                Console.WriteLine($"Play audio: ffplay -f {fmtStr} -ch_layout {audioDecoder.Ref.ch_layout.Describe()} -sample_rate {audioDecoder.Ref.sample_rate} {audioOutFile}");
            }

            videoDecoder?.Dispose();
            audioDecoder?.Dispose();
        }

        private static void DecodeAndWrite(MediaDecoder decoder, MediaPacket packet, MediaFrame frame, Action<MediaFrame> write)
        {
            foreach (var decoded in decoder.DecodePacket(packet, frame))
                write(decoded);
        }
    }
}
