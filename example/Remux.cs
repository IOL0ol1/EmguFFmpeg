using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: remux.c
    /// Copy all audio/video/subtitle streams from one container to another without transcoding.
    /// </summary>
    public unsafe class Remux : ExampleBase
    {
        public Remux() { Index = 12; Enable = false; }

        public override void Execute()
        {
            var inFile  = args.Length > 0 ? args[0] : "input.mp4";
            var outFile = args.Length > 1 ? args[1] : "output_remux.mkv";

            using var demuxer = MediaDemuxer.Open(inFile);
            demuxer.DumpFormat();

            using var muxer = MediaMuxer.Create(outFile);

            // Build stream mapping (skip non-A/V/subtitle).
            int nbStreams = (int)demuxer.Ref.nb_streams;
            var streamMapping = new int[nbStreams];
            int outStreamIdx = 0;

            for (int i = 0; i < nbStreams; i++)
            {
                var par = demuxer.Ref.streams[i]->codecpar;
                var t   = par->codec_type;
                if (t != AVMediaType.AVMEDIA_TYPE_AUDIO &&
                    t != AVMediaType.AVMEDIA_TYPE_VIDEO &&
                    t != AVMediaType.AVMEDIA_TYPE_SUBTITLE)
                {
                    streamMapping[i] = -1;
                    continue;
                }

                streamMapping[i] = outStreamIdx++;
                var codecpar = *par;
                var outStream = muxer.AddStream(codecpar);
                outStream.Ref.codecpar->codec_tag = 0;
            }

            muxer.DumpFormat();
            muxer.WriteHeader();

            using var packet = new MediaPacket();
            foreach (var pkt in demuxer.ReadPackets(packet))
            {
                int si = pkt.Ref.stream_index;
                if (si >= nbStreams || streamMapping[si] < 0)
                    continue;

                var inStream  = demuxer.Ref.streams[si];
                var newSi     = streamMapping[si];
                pkt.Ref.stream_index = newSi;

                var outStream = muxer.Ref.streams[newSi];
                Console.WriteLine($"in: pts={pkt.Ref.pts} stream_index={si}");
                ffmpeg.av_packet_rescale_ts(pkt, inStream->time_base, outStream->time_base);
                pkt.Ref.pos = -1;
                Console.WriteLine($"out: pts={pkt.Ref.pts} stream_index={newSi}");

                muxer.WritePacket(pkt);
            }

            muxer.WriteTrailer();
            Console.WriteLine($"Remux complete: '{inFile}' → '{outFile}'");
        }
    }
}
