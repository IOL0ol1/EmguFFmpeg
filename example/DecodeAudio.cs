using System;
using System.IO;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: decode_audio.c
    /// Decode data from an MP2 elementary stream and write raw interleaved PCM to a file.
    /// </summary>
    public unsafe class DecodeAudio : ExampleBase
    {
        public DecodeAudio() { Index = 3; Enable = false; }

        public override void Execute()
        {
            var inFile  = args.Length > 0 ? args[0] : "test.mp2";
            var outFile = args.Length > 1 ? args[1] : "test.pcm";

            var codec = MediaCodec.FindDecoder(AVCodecID.AV_CODEC_ID_MP2);
            if (codec == null) throw new Exception("MP2 decoder not found");

            using var parser  = new MediaCodecParserContext(AVCodecID.AV_CODEC_ID_MP2);
            using var decoder = MediaDecoder.Create(codec);
            using var frame   = new MediaFrame();

            using var outStream = File.OpenWrite(outFile);
            using var inStream  = File.OpenRead(inFile);

            // Parse the raw MP2 stream into packets and decode each one.
            foreach (var packet in parser.ParsePackets(decoder, inStream))
            {
                using (packet)
                    WriteDecodedFrames(decoder, frame, packet, outStream);
            }

            // Flush decoder.
            WriteDecodedFrames(decoder, frame, null, outStream);

            // Print ffplay playback hint.
            var sfmt = decoder.Ref.sample_fmt;
            if (sfmt.IsPlanar())
            {
                Console.WriteLine($"Warning: planar format detected. Only first channel written.");
                sfmt = sfmt.ToPacked();
            }

            string fmtStr = sfmt switch
            {
                AVSampleFormat.AV_SAMPLE_FMT_U8  => "u8",
                AVSampleFormat.AV_SAMPLE_FMT_S16 => "s16le",
                AVSampleFormat.AV_SAMPLE_FMT_S32 => "s32le",
                AVSampleFormat.AV_SAMPLE_FMT_FLT => "f32le",
                AVSampleFormat.AV_SAMPLE_FMT_DBL => "f64le",
                _ => "unknown"
            };

            Console.WriteLine("Play the output audio file with the command:");
            Console.WriteLine($"ffplay -f {fmtStr} -ac {decoder.Ref.ch_layout.nb_channels} -ar {decoder.Ref.sample_rate} {outFile}");
        }

        private static void WriteDecodedFrames(MediaDecoder decoder, MediaFrame frame, MediaPacket packet, Stream outStream)
        {
            foreach (var decodedFrame in decoder.DecodePacket(packet, frame))
            {
                int dataSize = decoder.Ref.sample_fmt.GetBytesPerSample();
                for (int i = 0; i < decodedFrame.Ref.nb_samples; i++)
                    for (int ch = 0; ch < decoder.Ref.ch_layout.nb_channels; ch++)
                    {
                        var span = new ReadOnlySpan<byte>(decodedFrame.Ref.data[(uint)ch] + dataSize * i, dataSize);
                        outStream.Write(span);
                    }
            }
        }
    }
}
