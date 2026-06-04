using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp.Example
{
    /// <summary>
    /// Maps to FFmpeg example: qsv_transcode.c
    /// Intel QSV-accelerated video transcoding with support for dynamically changing
    /// encoder options at specified frame numbers.
    /// Usage: args[0] = input, args[1] = encoder (e.g. h264_qsv), args[2] = output,
    ///        args[3] = "key value ..." initial options,
    ///        then pairs: args[4]=frame_num, args[5]="key value ...", ...
    /// </summary>
    public unsafe class QsvTranscode : ExampleBase
    {
        // Dynamic encoder setting: apply an option string at a given frame number.
        private struct DynamicSetting
        {
            public int    FrameNumber;
            public string OptStr;
        }

        private DynamicSetting[] _settings;
        private int              _currentSetting;
        private int              _frameNumber;
        private AVCodecContext*  _encoderCtx;

        public QsvTranscode() { Index = 22; Enable = false; }

        public override void Execute()
        {
            if (args.Length < 4 || (args.Length - 4) % 2 != 0)
            {
                Console.Error.WriteLine(
                    "Usage: QsvTranscode <input> <encoder> <output> \"<initial options>\" " +
                    "[<frame_number> \"<options>\"]...");
                return;
            }

            int settingCount = (args.Length - 4) / 2;
            _settings = new DynamicSetting[settingCount];
            for (int i = 0; i < settingCount; i++)
            {
                _settings[i].FrameNumber = int.Parse(args[4 + i * 2]);
                _settings[i].OptStr      = args[5 + i * 2];
            }
            _currentSetting = 0;
            _frameNumber    = 0;

            var inFile      = args[0];
            var encoderName = args[1];
            var outFile     = args[2];
            var initOptStr  = args[3];

            // ── QSV hardware device ───────────────────────────────────────────
            AVBufferRef* hwDeviceCtx = null;
            ffmpeg.av_hwdevice_ctx_create(&hwDeviceCtx, AVHWDeviceType.AV_HWDEVICE_TYPE_QSV,
                                          null, null, 0).ThrowIfError();
            var hwDeviceCtxPtr = (IntPtr)hwDeviceCtx;

            // ── Decoder ───────────────────────────────────────────────────────
            using var demuxer = MediaDemuxer.Open(inFile);

            int videoStream = -1;
            MediaCodec qsvDecoder = null;
            for (int i = 0; i < (int)demuxer.Ref.nb_streams && videoStream < 0; i++)
            {
                var st = demuxer.Ref.streams[i];
                if (st->codecpar->codec_type != AVMediaType.AVMEDIA_TYPE_VIDEO) continue;

                string decoderName = st->codecpar->codec_id switch
                {
                    AVCodecID.AV_CODEC_ID_H264       => "h264_qsv",
                    AVCodecID.AV_CODEC_ID_HEVC        => "hevc_qsv",
                    AVCodecID.AV_CODEC_ID_VP9         => "vp9_qsv",
                    AVCodecID.AV_CODEC_ID_VP8         => "vp8_qsv",
                    AVCodecID.AV_CODEC_ID_AV1         => "av1_qsv",
                    AVCodecID.AV_CODEC_ID_MPEG2VIDEO  => "mpeg2_qsv",
                    AVCodecID.AV_CODEC_ID_MJPEG       => "mjpeg_qsv",
                    _                                 => null
                };
                if (decoderName == null)
                {
                    Console.Error.WriteLine("Codec is not supported by QSV");
                    return;
                }

                qsvDecoder = MediaCodec.FindDecoder(decoderName);
                videoStream = i;
            }
            if (videoStream < 0 || qsvDecoder == null)
                throw new Exception("Cannot find a QSV-capable video stream in the input");

            var videoSt     = demuxer.Ref.streams[videoStream];
            var codecparPtr = (IntPtr)videoSt->codecpar;

            AVCodecContext_get_format getFormatFn = (_, pix_fmts) =>
            {
                for (var p = pix_fmts; *p != AVPixelFormat.AV_PIX_FMT_NONE; p++)
                    if (*p == AVPixelFormat.AV_PIX_FMT_QSV)
                        return AVPixelFormat.AV_PIX_FMT_QSV;
                Console.Error.WriteLine("The QSV pixel format not offered in get_format()");
                return AVPixelFormat.AV_PIX_FMT_NONE;
            };

            using var decoder = MediaDecoder.Create(qsvDecoder, ctx =>
            {
                ffmpeg.avcodec_parameters_to_context(ctx, (AVCodecParameters*)codecparPtr).ThrowIfError();
                ctx.Ref.framerate    = ffmpeg.av_guess_frame_rate(demuxer, videoSt, null);
                ctx.Ref.hw_device_ctx = ffmpeg.av_buffer_ref((AVBufferRef*)hwDeviceCtxPtr);
                ctx.Ref.pkt_timebase = videoSt->time_base;
                ctx.Ref.get_format   = getFormatFn;
            });

            // ── Encoder (opened lazily after the first decoded frame) ─────────
            var encCodec = MediaCodec.FindEncoder(encoderName)
                           ?? throw new Exception($"Could not find encoder '{encoderName}'");

            _encoderCtx = ffmpeg.avcodec_alloc_context3(encCodec);
            if (_encoderCtx == null) throw new Exception("Failed to allocate encoder context");

            // ── Output muxer ──────────────────────────────────────────────────
            AVFormatContext* ofmtCtx = null;
            ffmpeg.avformat_alloc_output_context2(&ofmtCtx, null, null, outFile).ThrowIfError();

            ffmpeg.avio_open(&ofmtCtx->pb, outFile, ffmpeg.AVIO_FLAG_WRITE).ThrowIfError();

            using var encPkt = new MediaPacket();
            using var frame  = new MediaFrame();
            using var decPkt = new MediaPacket();

            bool headerWritten = false;

            // ── Transcode loop ────────────────────────────────────────────────
            int ret = 0;
            foreach (var pkt in demuxer.ReadPackets(decPkt))
            {
                if (pkt.Ref.stream_index != videoStream) continue;
                ret = DecodeEncode(decoder, frame, encPkt, encCodec, ofmtCtx,
                                   initOptStr, pkt, ref headerWritten, hwDeviceCtxPtr);
                if (ret < 0) break;
            }

            // Flush decoder.
            DecodeEncode(decoder, frame, encPkt, encCodec, ofmtCtx,
                         initOptStr, null, ref headerWritten, hwDeviceCtxPtr);

            // Flush encoder.
            EncodeWrite(encPkt, null, ofmtCtx);

            if (headerWritten)
                ffmpeg.av_write_trailer(ofmtCtx);

            // ── Cleanup ───────────────────────────────────────────────────────
            var tmpEncoderCtx = _encoderCtx;
            ffmpeg.avcodec_free_context(&tmpEncoderCtx);

            if (ofmtCtx != null && (ofmtCtx->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
                ffmpeg.avio_closep(&ofmtCtx->pb);
            ffmpeg.avformat_free_context(ofmtCtx);

            ffmpeg.av_buffer_unref(&hwDeviceCtx);
        }

        private int DecodeEncode(MediaDecoder decoder, MediaFrame frame, MediaPacket encPkt,
                                  MediaCodec encCodec, AVFormatContext* ofmtCtx, string initOptStr,
                                  MediaPacket pkt, ref bool headerWritten, IntPtr hwDeviceCtxPtr)
        {
            int ret = decoder.SendPacket(pkt);
            if (ret < 0)
            {
                Console.Error.WriteLine($"Error during decoding. Error code: {FFmpegException.GetErrorString(ret)}");
                return ret;
            }

            while (ret >= 0)
            {
                frame.Unref();
                ret = decoder.ReceiveFrame(frame);
                if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
                    return 0;
                if (ret < 0)
                {
                    Console.Error.WriteLine($"Error while decoding. Error code: {FFmpegException.GetErrorString(ret)}");
                    return ret;
                }

                // Lazily open encoder on first decoded frame (once hw_frames_ctx is available).
                if (_encoderCtx->hw_frames_ctx == null)
                {
                    _encoderCtx->hw_frames_ctx = ffmpeg.av_buffer_ref(decoder.Ref.hw_frames_ctx);
                    if (_encoderCtx->hw_frames_ctx == null)
                        return ffmpeg.AVERROR(12 /*ENOMEM*/);

                    _encoderCtx->time_base = ffmpeg.av_inv_q(decoder.Ref.framerate);
                    _encoderCtx->pix_fmt   = AVPixelFormat.AV_PIX_FMT_QSV;
                    _encoderCtx->width     = decoder.Ref.width;
                    _encoderCtx->height    = decoder.Ref.height;

                    AVDictionary* opts = null;
                    StrToDict(initOptStr, &opts);

                    // Check for "r" (framerate) option.
                    var rEntry = ffmpeg.av_dict_get(opts, "r", null, 0);
                    if (rEntry != null)
                    {
                        double fps = double.Parse(((IntPtr)rEntry->value).PtrToStringUTF8() ?? "25");
                        _encoderCtx->framerate = ffmpeg.av_d2q(fps, int.MaxValue);
                        _encoderCtx->time_base = ffmpeg.av_inv_q(_encoderCtx->framerate);
                    }

                    ffmpeg.avcodec_open2(_encoderCtx, encCodec, &opts).ThrowIfError();
                    ffmpeg.av_dict_free(&opts);

                    var ost = ffmpeg.avformat_new_stream(ofmtCtx, encCodec);
                    if (ost == null) return ffmpeg.AVERROR(12);
                    ost->time_base = _encoderCtx->time_base;
                    ffmpeg.avcodec_parameters_from_context(ost->codecpar, _encoderCtx).ThrowIfError();

                    ffmpeg.avformat_write_header(ofmtCtx, null).ThrowIfError();
                    headerWritten = true;
                }

                // Rescale pts.
                AVFrame* f = frame;
                f->pts = ffmpeg.av_rescale_q(f->pts, decoder.Ref.pkt_timebase, _encoderCtx->time_base);

                ret = EncodeWrite(encPkt, frame, ofmtCtx);
                if (ret < 0)
                    Console.Error.WriteLine($"Error during encoding and writing.");
            }
            return ret;
        }

        private int EncodeWrite(MediaPacket encPkt, MediaFrame frame, AVFormatContext* ofmtCtx)
        {
            encPkt.Unref();

            // Apply any pending dynamic encoder settings.
            DynamicSetParameter();

            int ret = ffmpeg.avcodec_send_frame(_encoderCtx, frame);
            if (ret < 0)
            {
                Console.Error.WriteLine($"Error during encoding. Error code: {FFmpegException.GetErrorString(ret)}");
                goto end;
            }

            while (true)
            {
                ret = ffmpeg.avcodec_receive_packet(_encoderCtx, encPkt);
                if (ret != 0) break;

                encPkt.Ref.stream_index = 0;
                ffmpeg.av_packet_rescale_ts(encPkt, _encoderCtx->time_base,
                                            ofmtCtx->streams[0]->time_base);
                ret = ffmpeg.av_interleaved_write_frame(ofmtCtx, encPkt);
                if (ret < 0)
                {
                    Console.Error.WriteLine($"Error during writing data to output file. Error code: {FFmpegException.GetErrorString(ret)}");
                    return ret;
                }
            }

        end:
            if (ret == ffmpeg.AVERROR_EOF) return 0;
            return (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN)) ? 0 : -1;
        }

        private void DynamicSetParameter()
        {
            _frameNumber++;
            if (_currentSetting >= _settings.Length) return;
            if (_frameNumber != _settings[_currentSetting].FrameNumber) return;

            var optStr = _settings[_currentSetting++].OptStr;
            AVDictionary* opts = null;
            StrToDict(optStr, &opts);

            ffmpeg.av_opt_set_dict(_encoderCtx, &opts);
            ffmpeg.av_opt_set_dict(_encoderCtx->priv_data, &opts);

            var rEntry = ffmpeg.av_dict_get(opts, "r", null, 0);
            if (rEntry != null)
            {
                double fps = double.Parse(((IntPtr)rEntry->value).PtrToStringUTF8() ?? "25");
                _encoderCtx->framerate = ffmpeg.av_d2q(fps, int.MaxValue);
                _encoderCtx->time_base = ffmpeg.av_inv_q(_encoderCtx->framerate);
            }
            ffmpeg.av_dict_free(&opts);
        }

        private static void StrToDict(string optStr, AVDictionary** dict)
        {
            if (string.IsNullOrEmpty(optStr)) return;
            var parts = optStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i + 1 < parts.Length; i += 2)
                ffmpeg.av_dict_set(dict, parts[i], parts[i + 1], 0);
        }
    }
}
