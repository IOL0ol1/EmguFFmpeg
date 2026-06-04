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
    public unsafe class QsvDecode : ExampleBase
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
                var st = demuxer.Ref.streams[i];
                if (st->codecpar->codec_id == AVCodecID.AV_CODEC_ID_H264 && videoStreamIdx < 0)
                    videoStreamIdx = i;
                else
                    st->discard = AVDiscard.AVDISCARD_ALL;
            }
            if (videoStreamIdx < 0)
                throw new Exception("No H.264 video stream found in the input file");

            // ── QSV device ────────────────────────────────────────────────────
            AVBufferRef* deviceRef = null;
            ffmpeg.av_hwdevice_ctx_create(&deviceRef, AVHWDeviceType.AV_HWDEVICE_TYPE_QSV,
                                          "auto", null, 0).ThrowIfError();
            var deviceRefPtr = (IntPtr)deviceRef;

            // ── Decoder ───────────────────────────────────────────────────────
            var qsvDecoder = MediaCodec.FindDecoder("h264_qsv")
                             ?? throw new Exception("The QSV decoder (h264_qsv) is not present in libavcodec");

            // get_format callback: always pick AV_PIX_FMT_QSV.
            AVCodecContext_get_format getFormatFn = (_, pix_fmts) =>
            {
                for (var p = pix_fmts; *p != AVPixelFormat.AV_PIX_FMT_NONE; p++)
                    if (*p == AVPixelFormat.AV_PIX_FMT_QSV)
                        return AVPixelFormat.AV_PIX_FMT_QSV;
                Console.Error.WriteLine("The QSV pixel format not offered in get_format()");
                return AVPixelFormat.AV_PIX_FMT_NONE;
            };

            var videoSt     = demuxer.Ref.streams[videoStreamIdx];
            var codecparPtr = (IntPtr)videoSt->codecpar;

            using var decoder = MediaDecoder.Create(qsvDecoder, ctx =>
            {
                ffmpeg.avcodec_parameters_to_context(ctx, (AVCodecParameters*)codecparPtr).ThrowIfError();
                ctx.Ref.hw_device_ctx = ffmpeg.av_buffer_ref((AVBufferRef*)deviceRefPtr);
                ctx.Ref.get_format    = getFormatFn;
            });

            // ── Output ────────────────────────────────────────────────────────
            AVIOContext* outputCtx = null;
            ffmpeg.avio_open(&outputCtx, outFile, ffmpeg.AVIO_FLAG_WRITE).ThrowIfError();

            using var frame   = new MediaFrame();
            using var swFrame = new MediaFrame();
            using var packet  = new MediaPacket();

            // ── Decode loop ───────────────────────────────────────────────────
            int ret = 0;
            foreach (var pkt in demuxer.ReadPackets(packet))
            {
                if (pkt.Ref.stream_index != videoStreamIdx) continue;
                ret = DecodePacket(decoder, frame, swFrame, pkt, outputCtx);
                if (ret < 0) break;
            }
            // Flush decoder.
            DecodePacket(decoder, frame, swFrame, null, outputCtx);

            ffmpeg.av_buffer_unref(&deviceRef);
            ffmpeg.avio_close(outputCtx);
        }

        private static int DecodePacket(MediaDecoder decoder, MediaFrame frame, MediaFrame swFrame,
                                         MediaPacket pkt, AVIOContext* outputCtx)
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

                // Transfer GPU frame → system memory.
                swFrame.Unref();
                ret = ffmpeg.av_hwframe_transfer_data(swFrame, frame, 0);
                if (ret < 0)
                {
                    Console.Error.WriteLine("Error transferring the data to system memory");
                    goto fail;
                }

                // Write each plane to the output file.
                AVFrame* sw = swFrame;
                for (uint i = 0; i < 8 && sw->data[i] != null; i++)
                {
                    int h        = sw->height >> (i > 0 ? 1 : 0);
                    int linesize = ffmpeg.av_image_get_linesize((AVPixelFormat)sw->format, sw->width, (int)i);
                    if (linesize < 0) { ret = linesize; goto fail; }
                    for (int j = 0; j < h; j++)
                        ffmpeg.avio_write(outputCtx, sw->data[i] + j * sw->linesize[i], linesize);
                }

            fail:
                swFrame.Unref();
                frame.Unref();
                if (ret < 0) return ret;
            }
            return 0;
        }
    }
}
