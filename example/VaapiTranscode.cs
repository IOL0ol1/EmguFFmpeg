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

            // ── Decoder (VAAPI-backed) ─────────────────────────────────────────
            using var demuxer = MediaDemuxer.Open(inFile);

            int videoStream = -1;
            MediaCodec decoder = null;
            for (int i = 0; i < (int)demuxer.Ref.nb_streams; i++)
            {
                if (demuxer[i].CodecparRef.codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    videoStream = i;
                    decoder     = MediaCodec.FindDecoder(demuxer[i].CodecparRef.codec_id);
                    break;
                }
            }
            if (videoStream < 0 || decoder == null)
                throw new Exception("Cannot find a video stream in the input file");

            var videoSt = demuxer[videoStream];

            // InitHWDeviceContext creates the VAAPI device and wires the get_format
            // callback that always picks AV_PIX_FMT_VAAPI.
            using var dec = MediaDecoder.CreateDecoder(videoSt.CodecparRef, decoder, ctx =>
            {
                if (ctx.InitHWDeviceContext(AVHWDeviceType.AV_HWDEVICE_TYPE_VAAPI) == 0)
                    throw new Exception("Unable to decode this file using VA-API.");
            });

            // ── Encoder (opened lazily after the first decoded frame) ─────────
            var encCodec = MediaCodec.FindEncoder(encoderName)
                           ?? throw new Exception($"Could not find encoder '{encoderName}'");

            MediaEncoder encoder = null;

            // ── Output muxer ──────────────────────────────────────────────────
            using var muxer = MediaMuxer.Create(outFile);

            bool initialized = false;

            using var encPkt = new MediaPacket();
            using var frame  = new MediaFrame();
            using var decPkt = new MediaPacket();

            // ── Transcode loop ────────────────────────────────────────────────
            int ret;
            foreach (var pkt in demuxer.ReadPackets(decPkt))
            {
                if (pkt.Ref.stream_index != videoStream) continue;
                ret = DecEnc(dec, ref encoder, encCodec, frame, encPkt, pkt, muxer,
                             ref initialized);
                if (ret < 0) break;
            }

            // Flush decoder.
            DecEnc(dec, ref encoder, encCodec, frame, encPkt, null, muxer,
                   ref initialized);

            // Flush encoder.
            EncodeWrite(encoder, encPkt, null, muxer);

            if (initialized)
                muxer.WriteTrailer();

            // ── Cleanup ───────────────────────────────────────────────────────
            encoder?.Dispose();
        }

        private static int DecEnc(MediaDecoder dec, ref MediaEncoder encoder, MediaCodec encCodec,
                                   MediaFrame frame, MediaPacket encPkt, MediaPacket pkt,
                                   MediaMuxer muxer, ref bool initialized)
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
                    encoder = MediaEncoder.Create(encCodec, c =>
                    {
                        c.AttachHWFramesContext(dec.GetHWFramesRef());
                        c.Ref.time_base = dec.Ref.framerate.ToInvert();
                        c.Ref.pix_fmt   = AVPixelFormat.AV_PIX_FMT_VAAPI;
                        c.Ref.width     = dec.Ref.width;
                        c.Ref.height    = dec.Ref.height;
                    });

                    muxer.AddStream(encoder);
                    muxer.WriteHeader();

                    initialized = true;
                }

                ret = EncodeWrite(encoder, encPkt, frame, muxer);
                if (ret < 0)
                    Console.Error.WriteLine("Error during encoding and writing.");
            }
            return ret;
        }

        private static int EncodeWrite(MediaEncoder encoder, MediaPacket encPkt,
                                        MediaFrame frame, MediaMuxer muxer)
        {
            if (encoder == null) return 0; // no frame ever reached the encoder

            encPkt.Unref();

            int ret = encoder.SendFrame(frame);
            if (ret < 0)
            {
                Console.Error.WriteLine($"Error during encoding. Error code: {FFmpegException.GetErrorString(ret)}");
                goto end;
            }

            while (true)
            {
                ret = encoder.ReceivePacket(encPkt);
                if (ret != 0) break;

                encPkt.Ref.stream_index = 0;
                // Rescale from the encoder timebase to the output stream timebase and write.
                ret = muxer.WritePacket(encPkt, encoder);
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
