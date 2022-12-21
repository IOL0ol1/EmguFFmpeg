using System;
using System.IO;
using System.Linq;
using FFmpeg.AutoGen;

namespace FFmpegSharp.Example.Other
{
    internal class Mp4ToPcm : ExampleBase
    {

        public Mp4ToPcm() : this($"video-input.mp4", $"{nameof(Mp4ToPcm)}-output.pcm", $"{nameof(Mp4ToPcm)}-output.wav")
        {
            Index = -99999;
        }

        public Mp4ToPcm(params string[] args) : base(args)
        { }


        public override void Execute()
        {
            // you can remove all line with "// it's for wav output"
            var srcFilename = args[0]; // a mp4 file
            var audioDstFilename = args[1]; // a pcm output file
            var wavDstFilename = args[2]; // it's for wav output

            using (var audioOutput = File.Create(audioDstFilename))
            using (var fmtctx = MediaDemuxer.Open(srcFilename))
            using (var wavOut = MediaMuxer.Create(wavDstFilename)) // it's for wav output
            {
                fmtctx.DumpFormat();
                var audioSrcStream = fmtctx.FirstOrDefault(_ => _.CodecparRef.codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO);
                using (var audioDecCtx = MediaDecoder.CreateDecoder(audioSrcStream.CodecparRef))
                /// it's important to use audio format conversion!!!
                using (var converter = SampleConverter.Create(audioDecCtx.ChLayout, audioDecCtx.SampleRate, AVSampleFormat.AV_SAMPLE_FMT_S16, audioDecCtx.FrameSize))
                using (var audioEncCtx = MediaEncoder.CreateAudioEncoder(wavOut.Format, audioDecCtx.SampleRate, audioDecCtx.ChLayout, AVSampleFormat.AV_SAMPLE_FMT_S16)) // it's for wav output
                using (var pkt = new MediaPacket())
                using (var frame = new MediaFrame())
                {
                    wavOut.AddStream(audioEncCtx); // it's for wav output
                    wavOut.WriteHeader(); // it's for wav output
                    foreach (var packet in fmtctx.ReadPackets(pkt))
                    {
                        if (packet.StreamIndex == audioSrcStream.Index)
                        {
                            foreach (var f in audioDecCtx.DecodePacket(packet, frame))
                            {
                                foreach (var o in converter.Convert(f))
                                {
                                    WriteAudioOut(o, audioOutput); // o.Data is PCM data
                                    foreach (var wavpkt in audioEncCtx.EncodeFrame(o)) // it's for wav output
                                        wavOut.WritePacket(wavpkt);  // it's for wav output
                                }
                            }
                        }
                    }
                }
            }
        }

        private unsafe void WriteAudioOut(MediaFrame f, Stream stream)
        {
            var unpadded_linesize = f.NbSamples * ffmpeg.av_get_bytes_per_sample((AVSampleFormat)f.Format);
            stream.Write(new ReadOnlySpan<byte>(f.Ref.extended_data[0], unpadded_linesize));
        }
    }
}
