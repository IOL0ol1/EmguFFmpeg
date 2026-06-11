using System;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    // AVMotionVector is not in FFmpeg.AutoGen 8.1.0 — define it manually from libavutil/motion_vector.h
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct AVMotionVector
    {
        public int    source;       // negative = past ref, positive = future ref
        public byte   w;
        public byte   h;
        public short  src_x;
        public short  src_y;
        public short  dst_x;
        public short  dst_y;
        public ulong  flags;
        public int    motion_x;
        public int    motion_y;
        public ushort motion_scale;
    }

    /// <summary>
    /// Maps to FFmpeg example: extract_mvs.c
    /// Decode a video file with the export_mvs flag set and print each
    /// motion vector's metadata to stdout.
    /// </summary>
    public unsafe class ExtractMvs : ExampleBase
    {
        public ExtractMvs() { Index = 15; Enable = false; }

        public override void Execute()
        {
            var inFile = args.Length > 0 ? args[0] : "input.mp4";

            using var demuxer = MediaDemuxer.Open(inFile);
            demuxer.DumpFormat();

            MediaCodec videoCodec = null;
            int videoStreamIdx = demuxer.FindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, ref videoCodec);
            if (videoStreamIdx < 0) throw new Exception("No video stream found");

            // Open decoder with flags2=+export_mvs.
            using var exportMvsOpts = new MediaDictionary { ["flags2"] = "+export_mvs" };
            using var decoder = MediaDecoder.CreateDecoder(
                demuxer[videoStreamIdx].CodecparRef,
                null,
                exportMvsOpts);

            Console.WriteLine("framenum,source,blockw,blockh,srcx,srcy,dstx,dsty,flags,motion_x,motion_y,motion_scale");

            using var frame  = new MediaFrame();
            using var packet = new MediaPacket();
            int frameCount = 0;

            foreach (var pkt in demuxer.ReadPackets(packet))
            {
                if (pkt.Ref.stream_index != videoStreamIdx) continue;
                decoder.SendPacket(pkt).ThrowIfError();
                DrainDecoder(decoder, frame, ref frameCount);
            }
            // Flush.
            decoder.SendPacket(null).ThrowIfError();
            DrainDecoder(decoder, frame, ref frameCount);
        }

        private static void DrainDecoder(MediaDecoder decoder, MediaFrame frame, ref int frameCount)
        {
            int ret;
            while ((ret = decoder.ReceiveFrame(frame)) >= 0)
            {
                frameCount++;
                var sd = frame.GetSideData(AVFrameSideDataType.AV_FRAME_DATA_MOTION_VECTORS);
                if (sd != null)
                {
                    var mvs = (AVMotionVector*)sd->data;
                    int count = (int)(sd->size / (ulong)sizeof(AVMotionVector));
                    for (int i = 0; i < count; i++)
                    {
                        var mv = mvs + i;
                        Console.WriteLine(
                            $"{frameCount},{mv->source,3},{mv->w,3},{mv->h,4},{mv->src_x,5},{mv->src_y,5}," +
                            $"{mv->dst_x,5},{mv->dst_y,5},0x{mv->flags:x16},{mv->motion_x,5},{mv->motion_y,5},{mv->motion_scale,5}");
                    }
                }
                frame.Unref();
            }
        }
    }
}
