using System;
using System.IO;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: qsv_decode.c
    /// Intel QSV-accelerated H.264 decoding: output frames are held in GPU video surfaces,
    /// then transferred to system memory and written raw to an output file.
    /// Usage: args[0] = input.h264/mp4, args[1] = output.raw
    /// </summary>
    public class QsvDecode : ExampleBase
    {
        public QsvDecode() { Index = 21; Enable = false; }

        public override void Execute()
        {
            var inFile  = args.Length > 0 ? args[0] : "input.mp4";
            var outFile = args.Length > 1 ? args[1] : "out_qsv.raw";

            // ── Open input ────────────────────────────────────────────────────
            using var demuxer = MediaDemuxer.Open(inFile);

            // Find the first H.264 video stream; discard all others.
            int videoStreamIdx = -1;
            for (int i = 0; i < (int)demuxer.Ref.nb_streams; i++)
            {
                if (demuxer[i].CodecparRef.codec_id == AVCodecID.AV_CODEC_ID_H264 && videoStreamIdx < 0)
                    videoStreamIdx = i;
                else
                    demuxer[i].Ref.discard = AVDiscard.AVDISCARD_ALL;
            }
            if (videoStreamIdx < 0)
                throw new Exception("No H.264 video stream found in the input file");

            // ── Decoder ───────────────────────────────────────────────────────
            var qsvDecoder = MediaCodec.FindDecoder("h264_qsv")
                             ?? throw new Exception("The QSV decoder (h264_qsv) is not present in libavcodec");

            // InitHWDeviceContext creates the QSV device (an "auto" session) and wires the
            // get_format callback that always picks AV_PIX_FMT_QSV.
            using var decoder = MediaDecoder.CreateDecoder(demuxer[videoStreamIdx].CodecparRef, qsvDecoder, ctx =>
            {
                if (ctx.InitHWDeviceContext(AVHWDeviceType.AV_HWDEVICE_TYPE_QSV, "auto") == 0)
                    throw new Exception("The QSV pixel format not offered by the decoder's HW configs");
            });

            // ── Output ────────────────────────────────────────────────────────
            using var output = MediaIOContext.Open(outFile, ffmpeg.AVIO_FLAG_WRITE);

            using var frame   = new MediaFrame();
            using var swFrame = new MediaFrame();
            using var packet  = new MediaPacket();

            // ── Decode loop ───────────────────────────────────────────────────
            int ret = 0;
            foreach (var pkt in demuxer.ReadPackets(packet))
            {
                if (pkt.Ref.stream_index != videoStreamIdx) continue;
                ret = DecodePacket(decoder, frame, swFrame, pkt, output);
                if (ret < 0) break;
            }
            // Flush decoder.
            DecodePacket(decoder, frame, swFrame, null, output);
        }

        private static int DecodePacket(MediaDecoder decoder, MediaFrame frame, MediaFrame swFrame,
                                         MediaPacket pkt, MediaIOContext output)
        {
            int ret = decoder.SendPacket(pkt);
            if (ret < 0)
            {
                Console.Error.WriteLine("Error during decoding");
                return ret;
            }

            while (ret >= 0)
            {
                ret = decoder.ReceiveFrame(frame);
                if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
                    break;
                if (ret < 0)
                {
                    Console.Error.WriteLine("Error during decoding");
                    return ret;
                }

                // Transfer GPU frame → system memory, then write the raw frame
                // (planes tightly packed, padding stripped) to the output file.
                swFrame.Unref();
                try
                {
                    MediaCodecContext.HWFrameTransferData(swFrame, frame);
                    output.Write(swFrame.GetBytes(padding: false));
                }
                catch (FFmpegException e)
                {
                    Console.Error.WriteLine("Error transferring the data to system memory");
                    ret = e.ErrorCode;
                }
                finally
                {
                    swFrame.Unref();
                    frame.Unref();
                }
                if (ret < 0) return ret;
            }
            return 0;
        }
    }
}
