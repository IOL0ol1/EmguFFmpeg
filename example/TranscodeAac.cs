using System;
using FFmpeg.AutoGen;
using FFmpeg.Sharp;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: transcode_aac.c
    /// Transcode the first audio stream of an input file to AAC at 96 kbps in an MP4 container.
    /// Uses AudioResampler for format/rate/channel-layout conversion and AudioFifo re-packetization.
    /// </summary>
    public unsafe class TranscodeAac : ExampleBase
    {
        private const int OutputBitRate  = 96000;
        private const int OutputChannels = 2;

        public TranscodeAac() { Index = 17; Enable = false; }

        public override void Execute()
        {
            var inFile  = args.Length > 0 ? args[0] : "input.mp4";
            var outFile = args.Length > 1 ? args[1] : "out_transcode_aac.m4a";

            // ── Open input ────────────────────────────────────────────────────
            using var demuxer = MediaDemuxer.Open(inFile);
            demuxer.DumpFormat();

            MediaCodec audioCodec = null;
            int audioStreamIdx = demuxer.FindBestStream(AVMediaType.AVMEDIA_TYPE_AUDIO, ref audioCodec);
            if (audioStreamIdx < 0) throw new Exception("No audio stream found");

            using var decoder = MediaDecoder.CreateDecoder(*demuxer.Ref.streams[audioStreamIdx]->codecpar);

            // ── Open output ───────────────────────────────────────────────────
            var aacCodec  = MediaCodec.FindEncoder(AVCodecID.AV_CODEC_ID_AAC);
            var outLayout = 2.ToDefaultChLayout();
            using var encoder = MediaEncoder.Audio()
                .Codec(aacCodec)
                .SampleRate(decoder.Ref.sample_rate)
                .ChannelLayout(outLayout)
                .SampleFormat(aacCodec.GetSampleFormats()[0])
                .Bitrate(OutputBitRate)
                .Flags(ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER) // MP4 stores extradata in the container header
                .Build();

            using var muxer = MediaMuxer.Create(outFile);
            muxer.AddStream(encoder);
            muxer.DumpFormat();
            muxer.WriteHeader();

            // ── Resampler ─────────────────────────────────────────────────────
            using var resampler = AudioResampler.For(decoder, encoder);

            // ── Main loop ─────────────────────────────────────────────────────
            using var frame  = new MediaFrame();
            using var packet = new MediaPacket();

            long ptsCounter = 0;

            foreach (var pkt in demuxer.ReadPackets(packet))
            {
                if (pkt.Ref.stream_index == audioStreamIdx)
                {
                    decoder.SendPacket(pkt).ThrowIfError();
                    pkt.Unref();
                    int ret;
                    while ((ret = decoder.ReceiveFrame(frame)) >= 0)
                    {
                        foreach (var outFrame in resampler.Convert(frame))
                        {
                            outFrame.Ref.pts = ptsCounter;
                            ptsCounter += outFrame.Ref.nb_samples;
                            foreach (var p in encoder.EncodeFrame(outFrame, packet))
                                muxer.WritePacket(p);
                        }
                        frame.Unref();
                    }
                }
            }
            // Flush decoder.
            decoder.SendPacket(null).ThrowIfError();
            {
                int ret;
                while ((ret = decoder.ReceiveFrame(frame)) >= 0)
                {
                    foreach (var outFrame in resampler.Convert(frame))
                    {
                        outFrame.Ref.pts = ptsCounter;
                        ptsCounter += outFrame.Ref.nb_samples;
                        foreach (var p in encoder.EncodeFrame(outFrame, packet))
                            muxer.WritePacket(p);
                    }
                    frame.Unref();
                }
            }
            // Flush resampler.
            foreach (var outFrame in resampler.Flush())
            {
                outFrame.Ref.pts = ptsCounter;
                ptsCounter += outFrame.Ref.nb_samples;
                foreach (var p in encoder.EncodeFrame(outFrame, packet))
                    muxer.WritePacket(p);
            }
            // Flush encoder.
            foreach (var p in encoder.EncodeFrame(null, packet))
                muxer.WritePacket(p);

            muxer.WriteTrailer();
            Console.WriteLine($"Transcoding to AAC complete: '{outFile}'");
        }
    }
}
