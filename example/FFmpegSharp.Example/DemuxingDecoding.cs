using System;
using System.IO;
using System.Linq;
using FFmpeg.AutoGen;

namespace FFmpegSharp.Example
{
    internal class DemuxingDecoding : ExampleBase
    {
        public DemuxingDecoding() : this("path-to-your-input.mp4", "path-to-your-out.v", "path-to-your-out.a")
        { }

        public DemuxingDecoding(params string[] args) : base(args)
        { }

        public override void Execute()
        {
            var srcFilename = args[0];
            var videoDstFilename = args[1];
            var audioDstFilename = args[2];

            using (var audioOutput = File.Create(audioDstFilename))
            using (var videoOutput = File.Create(videoDstFilename))
            using (var fmtctx = MediaDemuxer.Open(srcFilename))
            {
                var videoStream = fmtctx.FirstOrDefault(_ => _.CodecparRef.codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO);
                var audioStream = fmtctx.FirstOrDefault(_ => _.CodecparRef.codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO);
                using (var videoDecCtx = MediaDecoder.CreateDecoder(videoStream.CodecparRef))
                using (var audioDecCtx = MediaDecoder.CreateDecoder(audioStream.CodecparRef))
                // Use align = 1 AVFrame instead uint8_t *video_dst_data, less and safer code.
                using (var videoDstData = MediaFrame.CreateVideoFrame(videoDecCtx.Width, videoDecCtx.Height, videoDecCtx.PixFmt, 1))
                using (var pkt = new MediaPacket())
                using (var frame = new MediaFrame())
                {
                    fmtctx.DumpFormat();
                    foreach (var packet in fmtctx.ReadPackets(pkt))
                    {
                        if (packet.StreamIndex == videoStream.Index)
                        {
                            foreach (var f in videoDecCtx.DecodePacket(packet, frame))
                            {
                                WriteVideoOut(f, videoDstData, videoOutput);
                            }
                        }
                        if (packet.StreamIndex == audioStream.Index)
                        {
                            foreach (var f in audioDecCtx.DecodePacket(packet, frame))
                            {
                                WriteAudioOut(f, audioOutput);
                            }
                        }
                    }
                    // flush the decoders
                    foreach (var f in videoDecCtx.DecodePacket(null, frame))
                    {
                        WriteVideoOut(f, videoDstData, videoOutput);
                    }
                    foreach (var f in audioDecCtx.DecodePacket(null, frame))
                    {
                        WriteAudioOut(f, audioOutput);
                    }
                    Console.WriteLine("Demuxing succeeded.");

                    // video play command
                    Console.WriteLine($"Play the output video file with the command:\n" +
                           $"ffplay -f rawvideo -pix_fmt {videoDecCtx.PixFmt.GetName()} -video_size {videoDecCtx.Width}x{videoDecCtx.Height} {videoDstFilename}\n");

                    // TODO: audio play command
                    var audioCtx = audioDecCtx;
                    var sfmt = audioCtx.SampleFmt;
                    var n_channels = audioCtx.ChLayout.nb_channels;
                    if (ffmpeg.av_sample_fmt_is_planar(sfmt) != 0)
                    {
                        var packed = ffmpeg.av_get_sample_fmt_name(sfmt);
                        Console.WriteLine("Warning: the sample format the decoder produced is planar " +
                        $"({(packed != null ? packed : "?")}). This example will output the first channel only.\n");
                        sfmt = ffmpeg.av_get_packed_sample_fmt(sfmt);
                        n_channels = 1;
                    }

                }
            }

        }


        private unsafe void WriteVideoOut(MediaFrame f, MediaFrame of, Stream stream)
        {
            of.MakeWritable();
            var dstData = new byte_ptrArray4();
            dstData.UpdateFrom(of.Data);
            var dstLinesize = new int_array4();
            dstLinesize.UpdateFrom(of.Linesize);
            var srcData = new byte_ptrArray4();
            srcData.UpdateFrom(f.Data);
            var srcLinesize = new int_array4();
            srcLinesize.UpdateFrom(f.Linesize);
            ffmpeg.av_image_copy(ref dstData, ref dstLinesize, ref srcData, srcLinesize, (AVPixelFormat)f.Format, f.Width, f.Height);
            var videoDstBufferSize = ffmpeg.av_image_get_buffer_size((AVPixelFormat)of.Format, of.Width, of.Height, 1);
            stream.Write(new ReadOnlySpan<byte>(dstData[0], videoDstBufferSize));
        }

        private unsafe void WriteAudioOut(MediaFrame f, Stream stream)
        {
            var unpadded_linesize = f.NbSamples * ffmpeg.av_get_bytes_per_sample((AVSampleFormat)f.Format);
            stream.Write(new ReadOnlySpan<byte>(f.Ref.extended_data[0], unpadded_linesize));
        }

    }
}
