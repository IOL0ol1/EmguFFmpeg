using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: vaapi_transcode.c
    /// VAAPI-accelerated video transcoding: decode input video with VAAPI then
    /// re-encode it using a specified VAAPI encoder (e.g. h264_vaapi, vp9_vaapi).
    /// Usage: args[0] = input, args[1] = encode_codec (e.g. h264_vaapi), args[2] = output
    /// </summary>
    public unsafe class VaapiTranscode : ExampleBase
    {
        public VaapiTranscode() { Index = 24; Enable = false; }

        public override void Execute()
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine(
                    "Usage: VaapiTranscode <input_stream> <encode_codec> <output_stream>\n" +
                    "e.g.:  VaapiTranscode input.mp4 h264_vaapi output_h264.mp4");
                return;
            }

            var inFile      = args[0];
            var encoderName = args[1];
            var outFile     = args[2];

            // ── VAAPI device ──────────────────────────────────────────────────
            AVBufferRef* hwDeviceCtx = null;
            int ret = ffmpeg.av_hwdevice_ctx_create(&hwDeviceCtx,
                                                     AVHWDeviceType.AV_HWDEVICE_TYPE_VAAPI,
                                                     null, null, 0);
            if (ret < 0)
            {
                Console.Error.WriteLine($"Failed to create a VAAPI device. Error code: {FFmpegException.GetErrorString(ret)}");
                return;
            }
            var hwDeviceCtxPtr = (IntPtr)hwDeviceCtx;

            // ── Decoder (VAAPI-backed) ─────────────────────────────────────────
            using var demuxer = MediaDemuxer.Open(inFile);

            int videoStream = -1;
            MediaCodec decoder = null;
            for (int i = 0; i < (int)demuxer.Ref.nb_streams; i++)
            {
                if (demuxer.Ref.streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    videoStream = i;
                    decoder     = MediaCodec.FindDecoder(demuxer.Ref.streams[i]->codecpar->codec_id);
                    break;
                }
            }
            if (videoStream < 0 || decoder == null)
                throw new Exception("Cannot find a video stream in the input file");

            var videoSt     = demuxer.Ref.streams[videoStream];
            var codecparPtr = (IntPtr)videoSt->codecpar;

            // VAAPI get_format callback: always prefer AV_PIX_FMT_VAAPI.
            AVCodecContext_get_format getVaapiFormat = (_, pix_fmts) =>
            {
                for (var p = pix_fmts; *p != AVPixelFormat.AV_PIX_FMT_NONE; p++)
                    if (*p == AVPixelFormat.AV_PIX_FMT_VAAPI)
                        return AVPixelFormat.AV_PIX_FMT_VAAPI;
                Console.Error.WriteLine("Unable to decode this file using VA-API.");
                return AVPixelFormat.AV_PIX_FMT_NONE;
            };

            using var dec = MediaDecoder.Create(decoder, ctx =>
            {
                ffmpeg.avcodec_parameters_to_context(ctx, (AVCodecParameters*)codecparPtr).ThrowIfError();
                ctx.Ref.hw_device_ctx = ffmpeg.av_buffer_ref((AVBufferRef*)hwDeviceCtxPtr);
                ctx.Ref.get_format    = getVaapiFormat;
            });

            // ── Encoder (opened lazily after the first decoded frame) ─────────
            var encCodec = MediaCodec.FindEncoder(encoderName)
                           ?? throw new Exception($"Could not find encoder '{encoderName}'");

            AVCodecContext*  encoderCtx = ffmpeg.avcodec_alloc_context3(encCodec);
            if (encoderCtx == null) throw new Exception("Failed to allocate encoder context");

            // ── Output muxer ──────────────────────────────────────────────────
            AVFormatContext* ofmtCtx = null;
            ffmpeg.avformat_alloc_output_context2(&ofmtCtx, null, null, outFile).ThrowIfError();
            ffmpeg.avio_open(&ofmtCtx->pb, outFile, ffmpeg.AVIO_FLAG_WRITE).ThrowIfError();

            bool initialized = false;
            AVStream* ost = null;

            using var encPkt = new MediaPacket();
            using var frame  = new MediaFrame();
            using var decPkt = new MediaPacket();

            // ── Transcode loop ────────────────────────────────────────────────
            foreach (var pkt in demuxer.ReadPackets(decPkt))
            {
                if (pkt.Ref.stream_index != videoStream) continue;
                ret = DecEnc(dec, encoderCtx, encCodec, frame, encPkt, pkt, ofmtCtx,
                             ref initialized, ref ost);
                if (ret < 0) break;
            }

            // Flush decoder.
            DecEnc(dec, encoderCtx, encCodec, frame, encPkt, null, ofmtCtx,
                   ref initialized, ref ost);

            // Flush encoder.
            EncodeWrite(encoderCtx, encPkt, null, ofmtCtx);

            ffmpeg.av_write_trailer(ofmtCtx);

            // ── Cleanup ───────────────────────────────────────────────────────
            ffmpeg.avcodec_free_context(&encoderCtx);

            if (ofmtCtx != null && (ofmtCtx->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
                ffmpeg.avio_closep(&ofmtCtx->pb);
            ffmpeg.avformat_free_context(ofmtCtx);

            ffmpeg.av_buffer_unref(&hwDeviceCtx);
        }

        private static int DecEnc(MediaDecoder dec, AVCodecContext* encoderCtx, MediaCodec encCodec,
                                   MediaFrame frame, MediaPacket encPkt, MediaPacket pkt,
                                   AVFormatContext* ofmtCtx, ref bool initialized, ref AVStream* ost)
        {
            int ret = dec.SendPacket(pkt);
            if (ret < 0)
            {
                Console.Error.WriteLine($"Error during decoding. Error code: {FFmpegException.GetErrorString(ret)}");
                return ret;
            }

            while (ret >= 0)
            {
                frame.Unref();
                ret = dec.ReceiveFrame(frame);
                if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
                    return 0;
                if (ret < 0)
                {
                    Console.Error.WriteLine($"Error while decoding. Error code: {FFmpegException.GetErrorString(ret)}");
                    return ret;
                }

                // Lazily open encoder on first frame (we need hw_frames_ctx from decoder).
                if (!initialized)
                {
                    encoderCtx->hw_frames_ctx = ffmpeg.av_buffer_ref(dec.Ref.hw_frames_ctx);
                    if (encoderCtx->hw_frames_ctx == null)
                        return ffmpeg.AVERROR(12 /*ENOMEM*/);

                    encoderCtx->time_base = ffmpeg.av_inv_q(dec.Ref.framerate);
                    encoderCtx->pix_fmt   = AVPixelFormat.AV_PIX_FMT_VAAPI;
                    encoderCtx->width     = dec.Ref.width;
                    encoderCtx->height    = dec.Ref.height;

                    ret = ffmpeg.avcodec_open2(encoderCtx, encCodec, null);
                    if (ret < 0)
                    {
                        Console.Error.WriteLine($"Failed to open encode codec. Error code: {FFmpegException.GetErrorString(ret)}");
                        return ret;
                    }

                    ost = ffmpeg.avformat_new_stream(ofmtCtx, encCodec);
                    if (ost == null) return ffmpeg.AVERROR(12);
                    ost->time_base = encoderCtx->time_base;

                    ret = ffmpeg.avcodec_parameters_from_context(ost->codecpar, encoderCtx);
                    if (ret < 0)
                    {
                        Console.Error.WriteLine($"Failed to copy stream parameters. Error code: {FFmpegException.GetErrorString(ret)}");
                        return ret;
                    }

                    ret = ffmpeg.avformat_write_header(ofmtCtx, null);
                    if (ret < 0)
                    {
                        Console.Error.WriteLine($"Error while writing stream header. Error code: {FFmpegException.GetErrorString(ret)}");
                        return ret;
                    }

                    initialized = true;
                }

                ret = EncodeWrite(encoderCtx, encPkt, frame, ofmtCtx);
                if (ret < 0)
                    Console.Error.WriteLine("Error during encoding and writing.");
            }
            return ret;
        }

        private static int EncodeWrite(AVCodecContext* encoderCtx, MediaPacket encPkt,
                                        MediaFrame frame, AVFormatContext* ofmtCtx)
        {
            encPkt.Unref();

            int ret = ffmpeg.avcodec_send_frame(encoderCtx, frame);
            if (ret < 0)
            {
                Console.Error.WriteLine($"Error during encoding. Error code: {FFmpegException.GetErrorString(ret)}");
                goto end;
            }

            while (true)
            {
                ret = ffmpeg.avcodec_receive_packet(encoderCtx, encPkt);
                if (ret != 0) break;

                encPkt.Ref.stream_index = 0;
                ffmpeg.av_packet_rescale_ts(encPkt,
                    // Use encoder timebase for rescaling to output stream timebase.
                    encoderCtx->time_base,
                    ofmtCtx->streams[0]->time_base);

                ret = ffmpeg.av_interleaved_write_frame(ofmtCtx, encPkt);
                if (ret < 0)
                {
                    Console.Error.WriteLine($"Error during writing data to output file. Error code: {FFmpegException.GetErrorString(ret)}");
                    return -1;
                }
            }

        end:
            if (ret == ffmpeg.AVERROR_EOF) return 0;
            return (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN)) ? 0 : -1;
        }
    }
}
