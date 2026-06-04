using System;
using System.IO;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: vaapi_encode.c
    /// VAAPI-accelerated H.264 encoding: read raw NV12 frames from a file and
    /// write the encoded H.264 bitstream to an output file.
    /// Usage: args[0] = width, args[1] = height, args[2] = input.nv12, args[3] = output.h264
    /// </summary>
    public unsafe class VaapiEncode : ExampleBase
    {
        public VaapiEncode() { Index = 23; Enable = false; }

        public override void Execute()
        {
            if (args.Length < 4)
            {
                Console.Error.WriteLine("Usage: VaapiEncode <width> <height> <input.nv12> <output.h264>");
                return;
            }

            int    width   = int.Parse(args[0]);
            int    height  = int.Parse(args[1]);
            var    inFile  = args[2];
            var    outFile = args[3];
            int    size    = width * height;

            // ── VAAPI device ──────────────────────────────────────────────────
            AVBufferRef* hwDeviceCtx = null;
            int err = ffmpeg.av_hwdevice_ctx_create(&hwDeviceCtx,
                                                     AVHWDeviceType.AV_HWDEVICE_TYPE_VAAPI,
                                                     null, null, 0);
            if (err < 0)
            {
                Console.Error.WriteLine($"Failed to create a VAAPI device. Error code: {FFmpegException.GetErrorString(err)}");
                return;
            }

            // ── Encoder ───────────────────────────────────────────────────────
            var codec = MediaCodec.FindEncoder("h264_vaapi")
                        ?? throw new Exception("Could not find encoder h264_vaapi");

            var avctxPtr = ffmpeg.avcodec_alloc_context3(codec);
            if (avctxPtr == null) throw new Exception("Failed to allocate encoder context");

            avctxPtr->width                  = width;
            avctxPtr->height                 = height;
            avctxPtr->time_base              = new AVRational { num = 1, den = 25 };
            avctxPtr->framerate              = new AVRational { num = 25, den = 1 };
            avctxPtr->sample_aspect_ratio    = new AVRational { num = 1, den = 1 };
            avctxPtr->pix_fmt                = AVPixelFormat.AV_PIX_FMT_VAAPI;

            // Set up the hw_frames_ctx with NV12 SW format.
            err = SetHwFrameCtx(avctxPtr, hwDeviceCtx, width, height);
            if (err < 0)
            {
                Console.Error.WriteLine("Failed to set hwframe context.");
                goto close;
            }

            err = ffmpeg.avcodec_open2(avctxPtr, codec, null);
            if (err < 0)
            {
                Console.Error.WriteLine($"Cannot open video encoder codec. Error code: {FFmpegException.GetErrorString(err)}");
                goto close;
            }

            // ── Encode loop ───────────────────────────────────────────────────
            using (var fin  = new FileStream(inFile,  FileMode.Open,   FileAccess.Read))
            using (var fout = new FileStream(outFile, FileMode.Create, FileAccess.Write))
            {
                var swFrame = ffmpeg.av_frame_alloc();
                var hwFrame = ffmpeg.av_frame_alloc();

                while (true)
                {
                    // Allocate a software frame for NV12 data.
                    swFrame->width  = width;
                    swFrame->height = height;
                    swFrame->format = (int)AVPixelFormat.AV_PIX_FMT_NV12;
                    err = ffmpeg.av_frame_get_buffer(swFrame, 0);
                    if (err < 0) break;

                    // Read Y plane (width * height bytes).
                    var yBuf = new byte[size];
                    if (fin.Read(yBuf, 0, size) < size) break;
                    fixed (byte* pY = yBuf)
                        Buffer.MemoryCopy(pY, swFrame->data[0u], size, size);

                    // Read UV plane (width * height / 2 bytes).
                    var uvBuf = new byte[size / 2];
                    if (fin.Read(uvBuf, 0, size / 2) < size / 2) break;
                    fixed (byte* pUV = uvBuf)
                        Buffer.MemoryCopy(pUV, swFrame->data[1u], size / 2, size / 2);

                    // Allocate a HW frame and transfer SW → HW.
                    err = ffmpeg.av_hwframe_get_buffer(avctxPtr->hw_frames_ctx, hwFrame, 0);
                    if (err < 0)
                    {
                        Console.Error.WriteLine($"Error code: {FFmpegException.GetErrorString(err)}.");
                        break;
                    }
                    if (hwFrame->hw_frames_ctx == null) { err = ffmpeg.AVERROR(12); break; }

                    err = ffmpeg.av_hwframe_transfer_data(hwFrame, swFrame, 0);
                    if (err < 0)
                    {
                        Console.Error.WriteLine($"Error while transferring frame data to surface. Error code: {FFmpegException.GetErrorString(err)}.");
                        break;
                    }

                    err = EncodeWrite(avctxPtr, hwFrame, fout);
                    if (err != 0 && err != ffmpeg.AVERROR(ffmpeg.EAGAIN))
                    {
                        Console.Error.WriteLine("Failed to encode.");
                        break;
                    }

                    ffmpeg.av_frame_unref(hwFrame);
                    ffmpeg.av_frame_unref(swFrame);
                }

                // Flush encoder.
                err = EncodeWrite(avctxPtr, null, fout);
                if (err == ffmpeg.AVERROR_EOF) err = 0;

                ffmpeg.av_frame_free(&swFrame);
                ffmpeg.av_frame_free(&hwFrame);
            }

        close:
            ffmpeg.avcodec_free_context(&avctxPtr);
            ffmpeg.av_buffer_unref(&hwDeviceCtx);
        }

        private static int SetHwFrameCtx(AVCodecContext* ctx, AVBufferRef* hwDeviceCtx,
                                          int width, int height)
        {
            var hwFramesRef = ffmpeg.av_hwframe_ctx_alloc(hwDeviceCtx);
            if (hwFramesRef == null)
            {
                Console.Error.WriteLine("Failed to create VAAPI frame context.");
                return -1;
            }

            var framesCtx = (AVHWFramesContext*)hwFramesRef->data;
            framesCtx->format             = AVPixelFormat.AV_PIX_FMT_VAAPI;
            framesCtx->sw_format          = AVPixelFormat.AV_PIX_FMT_NV12;
            framesCtx->width              = width;
            framesCtx->height             = height;
            framesCtx->initial_pool_size  = 20;

            int err = ffmpeg.av_hwframe_ctx_init(hwFramesRef);
            if (err < 0)
            {
                Console.Error.WriteLine($"Failed to initialize VAAPI frame context. Error code: {FFmpegException.GetErrorString(err)}");
                ffmpeg.av_buffer_unref(&hwFramesRef);
                return err;
            }

            ctx->hw_frames_ctx = ffmpeg.av_buffer_ref(hwFramesRef);
            if (ctx->hw_frames_ctx == null) err = ffmpeg.AVERROR(12 /*ENOMEM*/);
            ffmpeg.av_buffer_unref(&hwFramesRef);
            return err;
        }

        private static int EncodeWrite(AVCodecContext* avctx, AVFrame* frame, Stream fout)
        {
            var encPkt = ffmpeg.av_packet_alloc();
            int ret    = ffmpeg.avcodec_send_frame(avctx, frame);
            if (ret < 0)
            {
                Console.Error.WriteLine($"Error code: {FFmpegException.GetErrorString(ret)}");
                goto end;
            }

            while (true)
            {
                ret = ffmpeg.avcodec_receive_packet(avctx, encPkt);
                if (ret != 0) break;

                encPkt->stream_index = 0;
                var data = new byte[encPkt->size];
                System.Runtime.InteropServices.Marshal.Copy((IntPtr)encPkt->data, data, 0, encPkt->size);
                fout.Write(data, 0, data.Length);
                ffmpeg.av_packet_unref(encPkt);
            }

        end:
            ffmpeg.av_packet_free(&encPkt);
            return ret;
        }
    }
}
