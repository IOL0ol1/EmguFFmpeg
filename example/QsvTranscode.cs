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
        private MediaEncoder     _encoder;

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

            // ── Decoder ───────────────────────────────────────────────────────
            using var demuxer = MediaDemuxer.Open(inFile);

            int videoStream = -1;
            MediaCodec qsvDecoder = null;
            for (int i = 0; i < (int)demuxer.Ref.nb_streams && videoStream < 0; i++)
            {
                var st = demuxer[i];
                if (st.CodecparRef.codec_type != AVMediaType.AVMEDIA_TYPE_VIDEO) continue;

                string decoderName = st.CodecparRef.codec_id switch
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

            var videoSt = demuxer[videoStream];

            // InitHWDeviceContext creates the QSV device and wires the get_format
            // callback that always picks AV_PIX_FMT_QSV.
            using var decoder = MediaDecoder.CreateDecoder(videoSt.CodecparRef, qsvDecoder, ctx =>
            {
                ctx.Ref.framerate = demuxer.GuessFrameRate(videoSt);
                if (ctx.InitHWDeviceContext(AVHWDeviceType.AV_HWDEVICE_TYPE_QSV) == 0)
                    throw new Exception("The QSV pixel format not offered in get_format()");
                ctx.Ref.pkt_timebase = videoSt.Ref.time_base;
            });

            // ── Encoder (opened lazily after the first decoded frame) ─────────
            var encCodec = MediaCodec.FindEncoder(encoderName)
                           ?? throw new Exception($"Could not find encoder '{encoderName}'");

            // ── Output muxer ──────────────────────────────────────────────────
            using var muxer = MediaMuxer.Create(outFile);

            using var encPkt = new MediaPacket();
            using var frame  = new MediaFrame();
            using var decPkt = new MediaPacket();

            bool headerWritten = false;

            // ── Transcode loop ────────────────────────────────────────────────
            int ret = 0;
            foreach (var pkt in demuxer.ReadPackets(decPkt))
            {
                if (pkt.Ref.stream_index != videoStream) continue;
                ret = DecodeEncode(decoder, frame, encPkt, encCodec, muxer,
                                   initOptStr, pkt, ref headerWritten);
                if (ret < 0) break;
            }

            // Flush decoder.
            DecodeEncode(decoder, frame, encPkt, encCodec, muxer,
                         initOptStr, null, ref headerWritten);

            // Flush encoder.
            EncodeWrite(encPkt, null, muxer);

            if (headerWritten)
                muxer.WriteTrailer();

            // ── Cleanup ───────────────────────────────────────────────────────
            _encoder?.Dispose();
            _encoder = null;
        }

        private int DecodeEncode(MediaDecoder decoder, MediaFrame frame, MediaPacket encPkt,
                                  MediaCodec encCodec, MediaMuxer muxer, string initOptStr,
                                  MediaPacket pkt, ref bool headerWritten)
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
                if (_encoder == null)
                {
                    using var opts = StrToDict(initOptStr);

                    // Check for "r" (framerate) option.
                    var fpsOpt = opts["r"];

                    _encoder = MediaEncoder.Create(encCodec, c =>
                    {
                        c.AttachHWFramesContext(decoder.GetHWFramesRef());
                        c.Ref.time_base = decoder.Ref.framerate.ToInvert();
                        c.Ref.pix_fmt   = AVPixelFormat.AV_PIX_FMT_QSV;
                        c.Ref.width     = decoder.Ref.width;
                        c.Ref.height    = decoder.Ref.height;

                        if (fpsOpt != null)
                        {
                            c.Ref.framerate = double.Parse(fpsOpt).ToRational(int.MaxValue);
                            c.Ref.time_base = c.Ref.framerate.ToInvert();
                        }
                    }, opts);

                    muxer.AddStream(_encoder);
                    muxer.WriteHeader();
                    headerWritten = true;
                }

                // Rescale pts.
                frame.Ref.pts = frame.Ref.pts.Rescale(decoder.Ref.pkt_timebase, _encoder.Ref.time_base);

                ret = EncodeWrite(encPkt, frame, muxer);
                if (ret < 0)
                    Console.Error.WriteLine($"Error during encoding and writing.");
            }
            return ret;
        }

        private int EncodeWrite(MediaPacket encPkt, MediaFrame frame, MediaMuxer muxer)
        {
            if (_encoder == null) return 0; // no frame ever reached the encoder

            encPkt.Unref();

            // Apply any pending dynamic encoder settings.
            DynamicSetParameter();

            int ret = _encoder.SendFrame(frame);
            if (ret < 0)
            {
                Console.Error.WriteLine($"Error during encoding. Error code: {FFmpegException.GetErrorString(ret)}");
                goto end;
            }

            while (true)
            {
                ret = _encoder.ReceivePacket(encPkt);
                if (ret != 0) break;

                encPkt.Ref.stream_index = 0;
                // Rescale from the encoder timebase to the output stream timebase and write.
                ret = muxer.WritePacket(encPkt, _encoder);
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
            using var opts = StrToDict(optStr);

            // Check for "r" (framerate) option.
            var fpsOpt = opts["r"];
            if (fpsOpt != null)
            {
                _encoder.Ref.framerate = double.Parse(fpsOpt).ToRational(int.MaxValue);
                _encoder.Ref.time_base = _encoder.Ref.framerate.ToInvert();
            }

            // Apply the options to the encoder context and its codec private data.
            _encoder.SetOptions(opts);
        }

        private static MediaDictionary StrToDict(string optStr)
        {
            var dict = new MediaDictionary();
            if (string.IsNullOrEmpty(optStr)) return dict;
            var parts = optStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i + 1 < parts.Length; i += 2)
                dict[parts[i]] = parts[i + 1];
            return dict;
        }
    }
}
