using System;
using System.Collections.Generic;
using System.Linq;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    public unsafe partial class MediaEncoder : MediaCodecContext
    {
        /// <summary>Fluent builder for video encoders. Prefer this over the legacy <c>CreateVideoEncoder(...)</c> overloads.</summary>
        public static VideoEncoderBuilder Video() => new VideoEncoderBuilder();
        /// <summary>Fluent builder for audio encoders. Prefer this over the legacy <c>CreateAudioEncoder(...)</c> overloads.</summary>
        public static AudioEncoderBuilder Audio() => new AudioEncoderBuilder();

        #region Video
        public static MediaEncoder CreateVideoEncoder(
            MediaOutputFormat format,
            int width,
            int height,
            AVRational frameRate,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            Action<MediaCodecContext> otherSettings = null,
            MediaDictionary opts = null)
        {
            return Create(MediaCodec.FindEncoder(format.Ref.video_codec), c =>
            {
                c.Ref.width = width;
                c.Ref.height = height;
                c.Ref.time_base = frameRate.ToInvert();
                c.Ref.framerate = frameRate;
                if (pixelFormat == AVPixelFormat.AV_PIX_FMT_NONE)
                {
                    var pixelFmts = new MediaCodec(c.Ref.codec).GetPixelFmts();
                    pixelFormat = pixelFmts.FirstOrDefault();
                }
                c.Ref.pix_fmt = pixelFormat;

                c.Ref.bit_rate = bitrate;
                if ((format.Ref.flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
                    c.Ref.flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
                otherSettings?.Invoke(c);
            }, opts);
        }

        public static MediaEncoder CreateVideoEncoder(
            MediaCodec codec,
            int width,
            int height,
            AVRational frameRate,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContext> otherSettings = null,
            MediaDictionary opts = null)
        {
            return Create(codec, c =>
            {
                c.Ref.width = width;
                c.Ref.height = height;
                c.Ref.time_base = frameRate.ToInvert();
                c.Ref.framerate = frameRate;
                if (pixelFormat == AVPixelFormat.AV_PIX_FMT_NONE)
                {
                    var pixelFmts = new MediaCodec(c.Ref.codec).GetPixelFmts();
                    pixelFormat = pixelFmts.FirstOrDefault();
                }
                c.Ref.pix_fmt = pixelFormat;
                c.Ref.bit_rate = bitrate;
                c.Ref.flags |= flags;
                otherSettings?.Invoke(c);
            }, opts);
        }

        public static MediaEncoder CreateVideoEncoder(
            MediaCodec codec,
            int width,
            int height,
            double fps,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContext> otherSettings = null,
            MediaDictionary opts = null)
        {
            return CreateVideoEncoder(codec, width, height, fps.ToRational(), pixelFormat, bitrate, flags, otherSettings, opts);
        }

        public static MediaEncoder CreateVideoEncoder(
            MediaOutputFormat format,
            int width,
            int height,
            double fps,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            Action<MediaCodecContext> otherSettings = null,
            MediaDictionary opts = null)
        {
            return CreateVideoEncoder(format, width, height, fps.ToRational(), pixelFormat, bitrate, otherSettings, opts);
        }

        public static MediaEncoder CreateVideoEncoder(
            AVCodecID codecID,
            int width,
            int height,
            double fps,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContext> otherSettings = null,
            MediaDictionary opts = null)
        {
            return CreateVideoEncoder(MediaCodec.FindEncoder(codecID), width, height, fps.ToRational(), pixelFormat, bitrate, flags, otherSettings, opts);
        }

        public static MediaEncoder CreateVideoEncoder(
            AVCodecID codecID,
            int width,
            int height,
            AVRational frameRate,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContext> otherSettings = null,
            MediaDictionary opts = null)
        {
            return CreateVideoEncoder(MediaCodec.FindEncoder(codecID), width, height, frameRate, pixelFormat, bitrate, flags, otherSettings, opts);
        }

        public static MediaEncoder CreateVideoEncoder(
            string codecName,
            int width,
            int height,
            double fps,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContext> otherSettings = null,
            MediaDictionary opts = null)
        {
            return CreateVideoEncoder(MediaCodec.FindEncoder(codecName), width, height, fps.ToRational(), pixelFormat, bitrate, flags, otherSettings, opts);
        }

        public static MediaEncoder CreateVideoEncoder(
            string codecName,
            int width,
            int height,
            AVRational frameRate,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContext> otherSettings = null,
            MediaDictionary opts = null)
        {
            return CreateVideoEncoder(MediaCodec.FindEncoder(codecName), width, height, frameRate, pixelFormat, bitrate, flags, otherSettings, opts);
        }
        #endregion

        #region Audio

        public static MediaEncoder CreateAudioEncoder(
            MediaOutputFormat format,
            int sampleRate,
            AVChannelLayout chLayout,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            Action<MediaCodecContext> otherSettings = null,
            MediaDictionary opts = null)
        {
            return Create(MediaCodec.FindEncoder(format.Ref.audio_codec), c =>
            {
                c.Ref.sample_rate = sampleRate;
                c.Ref.ch_layout = chLayout;
                if (sampleFormat == AVSampleFormat.AV_SAMPLE_FMT_NONE)
                {
                    var sampleFmts = new MediaCodec(c.Ref.codec).GetSampleFormats();
                    sampleFormat = sampleFmts.FirstOrDefault();
                }
                c.Ref.sample_fmt = sampleFormat;
                c.Ref.time_base = new AVRational { num = 1, den = sampleRate };
                c.Ref.bit_rate = bitrate;
                if ((format.Ref.flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
                    c.Ref.flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
                otherSettings?.Invoke(c);
            }, opts);
        }

        public static MediaEncoder CreateAudioEncoder(
            MediaOutputFormat format,
            int sampleRate,
            int nbChannels,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            Action<MediaCodecContext> otherSettings = null,
            MediaDictionary opts = null)
        {
            return CreateAudioEncoder(format, sampleRate, AVChannelLayoutExtension.ToDefaultChLayout(nbChannels), sampleFormat, bitrate, otherSettings, opts);
        }

        public static MediaEncoder CreateAudioEncoder(
            MediaCodec codec,
            int sampleRate,
            AVChannelLayout chLayout,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContext> otherSettings = null,
            MediaDictionary opts = null)
        {
            return Create(codec, c =>
            {
                c.Ref.sample_rate = sampleRate;
                c.Ref.ch_layout = chLayout;
                if (sampleFormat == AVSampleFormat.AV_SAMPLE_FMT_NONE)
                {
                    var sampleFmts = new MediaCodec(c.Ref.codec).GetSampleFormats();
                    sampleFormat = sampleFmts.FirstOrDefault();
                }
                c.Ref.sample_fmt = sampleFormat;
                c.Ref.time_base = new AVRational { num = 1, den = sampleRate };
                c.Ref.bit_rate = bitrate;
                c.Ref.flags |= flags;
                otherSettings?.Invoke(c);
            }, opts);
        }
        public static MediaEncoder CreateAudioEncoder(
            MediaCodec codec,
            int sampleRate,
            int nbChannels,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContext> otherSettings = null,
            MediaDictionary opts = null)
        {
            return CreateAudioEncoder(codec, sampleRate, AVChannelLayoutExtension.ToDefaultChLayout(nbChannels), sampleFormat, bitrate, flags, otherSettings, opts);
        }

        public static MediaEncoder CreateAudioEncoder(
            AVCodecID codecID,
            int sampleRate,
            int nbChannels,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContext> otherSettings = null,
            MediaDictionary opts = null)
        {
            return CreateAudioEncoder(MediaCodec.FindEncoder(codecID), sampleRate, AVChannelLayoutExtension.ToDefaultChLayout(nbChannels), sampleFormat, bitrate, flags, otherSettings, opts);
        }

        public static MediaEncoder CreateAudioEncoder(
            AVCodecID codecID,
            int sampleRate,
            AVChannelLayout chLayout,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContext> otherSettings = null,
            MediaDictionary opts = null)
        {
            return CreateAudioEncoder(MediaCodec.FindEncoder(codecID), sampleRate, chLayout, sampleFormat, bitrate, flags, otherSettings, opts);
        }

        public static MediaEncoder CreateAudioEncoder(
            string codecName,
            int sampleRate,
            int nbChannels,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContext> otherSettings = null,
            MediaDictionary opts = null)
        {
            return CreateAudioEncoder(MediaCodec.FindEncoder(codecName), sampleRate, AVChannelLayoutExtension.ToDefaultChLayout(nbChannels), sampleFormat, bitrate, flags, otherSettings, opts);
        }

        public static MediaEncoder CreateAudioEncoder(
            string codecName,
            int sampleRate,
            AVChannelLayout chLayout,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContext> otherSettings = null,
            MediaDictionary opts = null)
        {
            return CreateAudioEncoder(MediaCodec.FindEncoder(codecName), sampleRate, chLayout, sampleFormat, bitrate, flags, otherSettings, opts);
        }

        #endregion

        public MediaEncoder(AVCodecContext* pAVCodecContext, bool leaveOpen)
            : base(pAVCodecContext, leaveOpen)
        { }

        public MediaEncoder(MediaCodec codec = null)
            : base(codec)
        { }

        #region Create

        public static MediaEncoder Create(MediaCodec codec, Action<MediaCodecContext> beforeOpenSetting, MediaDictionary opts = null)
        {
            var output = new MediaEncoder(codec);
            beforeOpenSetting?.Invoke(output);
            if (opts == null)
            {
                ffmpeg.avcodec_open2(output, codec, null).ThrowIfError();
            }
            else
            {
                fixed (AVDictionary** pOpts = &opts.pDictionary)
                    ffmpeg.avcodec_open2(output, codec, pOpts).ThrowIfError();
            }
            return output;
        }

        public static MediaEncoder CreateEncoder(AVCodecParameters codecParameters, Action<MediaCodecContext> action = null, MediaDictionary opts = null)
        {
            var codec = MediaCodec.FindEncoder(codecParameters.codec_id);
            AVCodecParameters* pCodecParameters = &codecParameters;
            // If codec_id is AV_CODEC_ID_NONE return null
            return codec == null
                ? null
                : Create(codec, _ =>
                {
                    ffmpeg.avcodec_parameters_to_context(_, pCodecParameters).ThrowIfError();
                    action?.Invoke(_);
                }, opts);
        }
        #endregion

        #region HW Video Encoder

        /// <summary>
        /// Create a hardware-accelerated video encoder.
        /// <para>
        /// Wires <c>hw_device_ctx</c> and <c>hw_frames_ctx</c> for you. The encoder consumes frames whose pixel
        /// format matches <paramref name="hwPixelFormat"/> (e.g. <c>AV_PIX_FMT_D3D11</c>, <c>AV_PIX_FMT_CUDA</c>,
        /// <c>AV_PIX_FMT_VAAPI</c>), allocated via <see cref="MediaFrame.AllocateOnHWFrames"/> using the
        /// <see cref="MediaCodecContext.GetHWFramesRef"/> exposed on the returned encoder.
        /// </para>
        /// <para>
        /// To implement HW→HW transcoding, share the device buffer from the decoder via
        /// <see cref="MediaCodecContext.AttachHWDevice"/> and pass it as <c>existingDeviceRef</c>.
        /// </para>
        /// </summary>
        /// <param name="codec">A HW-capable encoder (e.g. h264_nvenc, hevc_qsv, h264_vaapi).</param>
        /// <param name="width">Frame width.</param>
        /// <param name="height">Frame height.</param>
        /// <param name="frameRate">Frame rate (also used as 1/time_base).</param>
        /// <param name="hwPixelFormat">HW surface pixel format the encoder accepts.</param>
        /// <param name="swPixelFormat">Underlying SW pixel format of the HW frames (typically NV12).</param>
        /// <param name="hwDeviceType">Device type to create when <paramref name="existingDeviceRef"/> is null.</param>
        /// <param name="device">Device specifier (driver-defined, e.g. "0" for CUDA).</param>
        /// <param name="existingDeviceRef">Optional pre-existing hw_device_ctx (e.g. shared with a decoder). Will be av_buffer_ref'd.</param>
        /// <param name="existingFramesRef">Optional pre-existing hw_frames_ctx (e.g. from a filter graph). Skips internal allocation when supplied.</param>
        /// <param name="bitrate">Target bitrate in bits/sec.</param>
        /// <param name="otherSettings">Hook for additional configuration before avcodec_open2.</param>
        /// <param name="opts">Encoder-specific options dictionary.</param>
        public static MediaEncoder CreateHWVideoEncoder(
            MediaCodec codec,
            int width,
            int height,
            AVRational frameRate,
            AVPixelFormat hwPixelFormat,
            AVPixelFormat swPixelFormat,
            AVHWDeviceType hwDeviceType,
            string device = null,
            AVBufferRef* existingDeviceRef = null,
            AVBufferRef* existingFramesRef = null,
            int bitrate = 0,
            Action<MediaCodecContext> otherSettings = null,
            MediaDictionary opts = null)
        {
            if (codec == null) throw new ArgumentNullException(nameof(codec));
            return Create(codec, c =>
            {
                c.Ref.width = width;
                c.Ref.height = height;
                c.Ref.time_base = frameRate.ToInvert();
                c.Ref.framerate = frameRate;
                c.Ref.pix_fmt = hwPixelFormat;
                c.Ref.bit_rate = bitrate;

                // Attach device first (frames context needs a device).
                if (existingDeviceRef != null)
                {
                    c.AttachHWDevice(existingDeviceRef);
                }
                else
                {
                    AVBufferRef* dev = null;
                    ffmpeg.av_hwdevice_ctx_create(&dev, hwDeviceType, device, null, 0).ThrowIfError();
                    c.Ref.hw_device_ctx = dev;
                }

                if (existingFramesRef != null)
                {
                    c.AttachHWFramesContext(existingFramesRef);
                }
                else
                {
                    var framesRef = ffmpeg.av_hwframe_ctx_alloc(c.Ref.hw_device_ctx);
                    if (framesRef == null) throw new FFmpegException("av_hwframe_ctx_alloc returned null");
                    var framesCtx = (AVHWFramesContext*)framesRef->data;
                    framesCtx->format = hwPixelFormat;
                    framesCtx->sw_format = swPixelFormat;
                    framesCtx->width = width;
                    framesCtx->height = height;
                    framesCtx->initial_pool_size = 20;
                    int ret = ffmpeg.av_hwframe_ctx_init(framesRef);
                    if (ret < 0)
                    {
                        ffmpeg.av_buffer_unref(&framesRef);
                        ret.ThrowIfError();
                    }
                    c.Ref.hw_frames_ctx = framesRef;
                }

                otherSettings?.Invoke(c);
            }, opts);
        }

        /// <summary>String-based convenience: looks the codec up by name.</summary>
        public static MediaEncoder CreateHWVideoEncoder(
            string codecName,
            int width,
            int height,
            AVRational frameRate,
            AVPixelFormat hwPixelFormat,
            AVPixelFormat swPixelFormat,
            string hwDeviceTypeName,
            string device = null,
            int bitrate = 0,
            Action<MediaCodecContext> otherSettings = null,
            MediaDictionary opts = null)
        {
            var resolved = ffmpeg.av_hwdevice_find_type_by_name(hwDeviceTypeName);
            if (resolved == AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
                throw new ArgumentException($"Unknown HW device type '{hwDeviceTypeName}'.", nameof(hwDeviceTypeName));
            return CreateHWVideoEncoder(MediaCodec.FindEncoder(codecName), width, height, frameRate,
                hwPixelFormat, swPixelFormat, resolved, device, null, null, bitrate, otherSettings, opts);
        }

        #endregion

        /// <summary>
        /// <see cref="ffmpeg.avcodec_send_frame(AVCodecContext*, AVFrame*)"/>
        /// </summary>
        /// <param name="frame"></param>
        /// <returns>
        /// 0 on success, otherwise negative error code: AVERROR(EAGAIN): input is not accepted
        /// in the current state - user must read output with avcodec_receive_packet() (once
        /// all output is read, the packet should be resent, and the call will not fail with
        /// EAGAIN). AVERROR_EOF: the encoder has been flushed, and no new frames can be
        /// sent to it AVERROR(EINVAL): codec not opened, it is a decoder, or requires flush
        /// AVERROR(ENOMEM): failed to add packet to internal queue, or similar other errors:
        /// legitimate encoding errors</returns>
        public int SendFrame(MediaFrame frame) => ffmpeg.avcodec_send_frame(pCodecContext, frame);

        /// <summary>
        /// <see cref="ffmpeg.avcodec_receive_packet(AVCodecContext*, AVPacket*)"/>
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public int ReceivePacket(MediaPacket packet) => ffmpeg.avcodec_receive_packet(pCodecContext, packet);

        /// <summary>
        /// Encode <paramref name="frame"/> into one or more packets.
        /// <para>
        /// Pass <see langword="null"/> for <paramref name="frame"/> to enter draining mode (signal EOF to the encoder).
        /// </para>
        /// <para>
        /// The yielded <see cref="MediaPacket"/> is reused across iterations — call <see cref="MediaPacket.Clone"/>
        /// before storing it past the next MoveNext.
        /// </para>
        /// </summary>
        /// <param name="frame">Input frame, or <see langword="null"/> to flush.</param>
        /// <param name="inPacket">Optional reusable packet (avoids per-call allocation).</param>
        public IEnumerable<MediaPacket> EncodeFrame(MediaFrame frame, MediaPacket inPacket = null)
        {
            int ret = SendFrame(frame);
            // EAGAIN here means the encoder's internal queue is full; the caller is expected to drain at least one
            // packet before retrying. We treat EOF as "no more frames" but still drain whatever is buffered.
            if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                throw new FFmpegException(ret,
                    "avcodec_send_frame returned EAGAIN. Drain the encoder with ReceivePacket before sending more frames, or use EncodeFrame(frame) only after enumerating the previous call to completion.");
            if (ret != ffmpeg.AVERROR_EOF)
                ret.ThrowIfError();

            MediaPacket packet = inPacket ?? new MediaPacket();
            try
            {
                while (true)
                {
                    int rret = ReceivePacket(packet);
                    if (rret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || rret == ffmpeg.AVERROR_EOF)
                        yield break;
                    rret.ThrowIfError();
                    try
                    {
                        yield return packet;
                    }
                    finally { packet.Unref(); }
                }
            }
            finally
            {
                if (inPacket == null) packet.Dispose();
            }
        }

        /// <summary>
        /// Make a frame writable. Call BEFORE re-using a frame buffer that was previously fed to an encoder.
        /// </summary>
        public static int MakeWritable(MediaFrame frame)
            => frame == null ? 0 : ffmpeg.av_frame_make_writable(frame).ThrowIfError();
    }
}
