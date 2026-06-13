using System;
using System.IO;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example.Other
{
    /// <summary>
    /// Extract the first audio stream of a media file to raw PCM (f32le) and, in parallel, to a WAV file.
    /// Uses <see cref="AudioResampler"/> to convert the decoder output (often planar, e.g. fltp) to packed float.
    /// </summary>
    internal class Mp4ToPcm : ExampleBase
    {
        public Mp4ToPcm() : this($"video-input.mp4", $"{nameof(Mp4ToPcm)}-output.pcm", $"{nameof(Mp4ToPcm)}-output.wav")
        { Index = 34; Enable = false; }

        public Mp4ToPcm(params string[] args) : base(args)
        { }

        public override void Execute()
        {
            var srcFilename = args[0];
            var pcmDstFilename = args[1];
            var wavDstFilename = args[2];

            using var pcmOutput = File.Create(pcmDstFilename);
            using var demuxer = MediaDemuxer.Open(srcFilename);
            demuxer.DumpFormat();

            MediaCodec codec = null;
            int audioIndex = demuxer.FindBestStream(AVMediaType.AVMEDIA_TYPE_AUDIO, ref codec);
            if (audioIndex < 0) throw new Exception("No audio stream found");

            using var decoder = MediaDecoder.CreateDecoder(demuxer[audioIndex].CodecparRef);

            using var muxer = MediaMuxer.Create(wavDstFilename);
            using var encoder = MediaEncoder.Audio()
                .Codec(AVCodecID.AV_CODEC_ID_PCM_F32LE)
                .SampleRate(decoder.Ref.sample_rate)
                .ChannelLayout(decoder.Ref.ch_layout)
                .SampleFormat(AVSampleFormat.AV_SAMPLE_FMT_FLT)
                .Build();
            muxer.AddStream(encoder);
            muxer.WriteHeader();

            // decoder output (variable size, possibly planar) → packed f32 frames sized for the encoder
            using var resampler = AudioResampler.For(decoder, encoder);
            using var packet = new MediaPacket();
            using var frame = new MediaFrame();

            foreach (var pkt in demuxer.ReadPackets(packet))
            {
                if (pkt.Ref.stream_index != audioIndex) continue;
                foreach (var decoded in decoder.DecodePacket(pkt, frame))
                    foreach (var pcm in resampler.Convert(decoded))
                        using (pcm) WritePcm(pcm, pcmOutput, encoder, muxer);
            }
            // flush decoder → resampler → encoder
            foreach (var decoded in decoder.DecodePacket(null, frame))
                foreach (var pcm in resampler.Convert(decoded))
                    using (pcm) WritePcm(pcm, pcmOutput, encoder, muxer);
            foreach (var pcm in resampler.Flush())
                using (pcm) WritePcm(pcm, pcmOutput, encoder, muxer);
            muxer.FlushCodecs(new[] { encoder });
            muxer.WriteTrailer();

            Console.WriteLine("Play the raw output with the command:");
            Console.WriteLine($"ffplay -f f32le -ac {decoder.Ref.ch_layout.nb_channels} -ar {decoder.Ref.sample_rate} {pcmDstFilename}");
        }

        private static unsafe void WritePcm(MediaFrame pcm, Stream pcmOutput, MediaEncoder encoder, MediaMuxer muxer)
        {
            // packed f32le → one interleaved plane
            int bytes = pcm.Ref.nb_samples * pcm.Ref.ch_layout.nb_channels * ffmpeg.av_get_bytes_per_sample((AVSampleFormat)pcm.Ref.format);
            pcmOutput.Write(new ReadOnlySpan<byte>(pcm.Ref.extended_data[0], bytes));
            foreach (var wavPkt in encoder.EncodeFrame(pcm))
                muxer.WritePacket(wavPkt, encoder.Ref.time_base);
        }
    }
}
