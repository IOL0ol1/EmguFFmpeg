using System;
using System.Linq;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    /// <summary>
    /// Fluent builder for <see cref="MediaEncoder"/>. Replaces the legacy 14-overload CreateXxxEncoder API.
    /// <para>
    /// Usage:
    /// <code>
    /// using var encoder = MediaEncoder.Video()
    ///     .Codec(AVCodecID.AV_CODEC_ID_H264)
    ///     .Size(1920, 1080)
    ///     .Fps(30)
    ///     .Bitrate(4_000_000)
    ///     .Configure(c => c.Ref.gop_size = 60)
    ///     .Build();
    /// </code>
    /// </para>
    /// </summary>
    public unsafe abstract class MediaEncoderBuilder
    {
        protected MediaCodec _codec;
        protected MediaOutputFormat _format;
        protected int _bitrate;
        protected int _flags;
        protected Action<MediaCodecContext> _configure;
        protected MediaDictionary _opts;

        protected void SetGlobalHeaderIfNeeded(MediaCodecContext c)
        {
            if (_format != null && (_format.Ref.flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
                c.Ref.flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
            c.Ref.flags |= _flags;
        }
    }

    /// <summary>Video encoder builder.</summary>
    public unsafe class VideoEncoderBuilder : MediaEncoderBuilder
    {
        private int _width;
        private int _height;
        private AVRational _frameRate;
        private AVPixelFormat _pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE;
        // HW-specific:
        private bool _useHW;
        private AVPixelFormat _hwPixelFormat;
        private AVPixelFormat _swPixelFormat;
        private AVHWDeviceType _hwDeviceType;
        private string _hwDevice;
        private AVBufferRef* _existingDeviceRef;
        private AVBufferRef* _existingFramesRef;

        public VideoEncoderBuilder Codec(MediaCodec codec) { _codec = codec ?? throw new ArgumentNullException(nameof(codec)); return this; }
        public VideoEncoderBuilder Codec(AVCodecID id) => Codec(MediaCodec.FindEncoder(id) ?? throw new ArgumentException($"No encoder found for {id}"));
        public VideoEncoderBuilder Codec(string name) => Codec(MediaCodec.FindEncoder(name) ?? throw new ArgumentException($"No encoder found for '{name}'"));

        public VideoEncoderBuilder OutputFormat(MediaOutputFormat format)
        {
            _format = format;
            if (_codec == null && format != null)
                _codec = MediaCodec.FindEncoder(format.Ref.video_codec);
            return this;
        }

        public VideoEncoderBuilder Size(int width, int height) { _width = width; _height = height; return this; }
        public VideoEncoderBuilder Fps(AVRational frameRate) { _frameRate = frameRate; return this; }
        public VideoEncoderBuilder Fps(double fps) { _frameRate = fps.ToRational(); return this; }
        public VideoEncoderBuilder PixelFormat(AVPixelFormat fmt) { _pixelFormat = fmt; return this; }
        public VideoEncoderBuilder Bitrate(int bitsPerSec) { _bitrate = bitsPerSec; return this; }
        public VideoEncoderBuilder Flags(int flags) { _flags = flags; return this; }
        public VideoEncoderBuilder Configure(Action<MediaCodecContext> configure) { _configure = configure; return this; }
        public VideoEncoderBuilder Options(MediaDictionary opts) { _opts = opts; return this; }

        /// <summary>Enable hardware-accelerated encoding (e.g. NVENC, QSV, VAAPI, AMF).</summary>
        public VideoEncoderBuilder UseHardware(AVPixelFormat hwPixelFormat, AVPixelFormat swPixelFormat, AVHWDeviceType deviceType, string device = null)
        {
            _useHW = true;
            _hwPixelFormat = hwPixelFormat;
            _swPixelFormat = swPixelFormat;
            _hwDeviceType = deviceType;
            _hwDevice = device;
            return this;
        }

        /// <summary>Reuse an externally-created device ref (e.g. decoder's hw_device_ctx) for HW→HW transcoding.</summary>
        public VideoEncoderBuilder UseHardwareDevice(AVBufferRef* deviceRef) { _existingDeviceRef = deviceRef; return this; }
        public VideoEncoderBuilder UseHardwareFrames(AVBufferRef* framesRef) { _existingFramesRef = framesRef; return this; }

        public MediaEncoder Build()
        {
            if (_codec == null) throw new InvalidOperationException("Specify a codec via Codec(...) or OutputFormat(...).");
            if (_width <= 0 || _height <= 0) throw new InvalidOperationException("Specify frame size via Size(width, height).");
            if (_frameRate.den == 0) throw new InvalidOperationException("Specify frame rate via Fps(...).");

            if (_useHW || _existingDeviceRef != null)
            {
                return MediaEncoder.CreateHWVideoEncoder(_codec, _width, _height, _frameRate,
                    _hwPixelFormat, _swPixelFormat, _hwDeviceType, _hwDevice,
                    _existingDeviceRef, _existingFramesRef,
                    _bitrate, c => { SetGlobalHeaderIfNeeded(c); _configure?.Invoke(c); }, _opts);
            }

            return MediaEncoder.Create(_codec, c =>
            {
                c.Ref.width = _width;
                c.Ref.height = _height;
                c.Ref.time_base = _frameRate.ToInvert();
                c.Ref.framerate = _frameRate;
                c.Ref.pix_fmt = _pixelFormat == AVPixelFormat.AV_PIX_FMT_NONE
                    ? (new MediaCodec(c.Ref.codec).GetPixelFmts().FirstOrDefault())
                    : _pixelFormat;
                c.Ref.bit_rate = _bitrate;
                SetGlobalHeaderIfNeeded(c);
                _configure?.Invoke(c);
            }, _opts);
        }
    }

    /// <summary>Audio encoder builder.</summary>
    public unsafe class AudioEncoderBuilder : MediaEncoderBuilder
    {
        private int _sampleRate;
        private AVChannelLayout _channelLayout;
        private bool _hasLayout;
        private int _channels;
        private AVSampleFormat _sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE;

        public AudioEncoderBuilder Codec(MediaCodec codec) { _codec = codec ?? throw new ArgumentNullException(nameof(codec)); return this; }
        public AudioEncoderBuilder Codec(AVCodecID id) => Codec(MediaCodec.FindEncoder(id) ?? throw new ArgumentException($"No encoder for {id}"));
        public AudioEncoderBuilder Codec(string name) => Codec(MediaCodec.FindEncoder(name) ?? throw new ArgumentException($"No encoder for '{name}'"));

        public AudioEncoderBuilder OutputFormat(MediaOutputFormat format)
        {
            _format = format;
            if (_codec == null && format != null)
                _codec = MediaCodec.FindEncoder(format.Ref.audio_codec);
            return this;
        }

        public AudioEncoderBuilder SampleRate(int hz) { _sampleRate = hz; return this; }
        public AudioEncoderBuilder ChannelLayout(AVChannelLayout layout) { _channelLayout = layout; _hasLayout = true; return this; }
        public AudioEncoderBuilder Channels(int n) { _channels = n; _hasLayout = false; return this; }
        public AudioEncoderBuilder SampleFormat(AVSampleFormat fmt) { _sampleFormat = fmt; return this; }
        public AudioEncoderBuilder Bitrate(int bitsPerSec) { _bitrate = bitsPerSec; return this; }
        public AudioEncoderBuilder Flags(int flags) { _flags = flags; return this; }
        public AudioEncoderBuilder Configure(Action<MediaCodecContext> configure) { _configure = configure; return this; }
        public AudioEncoderBuilder Options(MediaDictionary opts) { _opts = opts; return this; }

        public MediaEncoder Build()
        {
            if (_codec == null) throw new InvalidOperationException("Specify a codec via Codec(...) or OutputFormat(...).");
            if (_sampleRate <= 0) throw new InvalidOperationException("Specify SampleRate(...).");
            var layout = _hasLayout ? _channelLayout : AVChannelLayoutExtension.ToDefaultChLayout(_channels > 0 ? _channels : 2);
            return MediaEncoder.Create(_codec, c =>
            {
                c.Ref.sample_rate = _sampleRate;
                c.Ref.ch_layout = layout;
                c.Ref.sample_fmt = _sampleFormat == AVSampleFormat.AV_SAMPLE_FMT_NONE
                    ? (new MediaCodec(c.Ref.codec).GetSampleFormats().FirstOrDefault())
                    : _sampleFormat;
                c.Ref.time_base = new AVRational { num = 1, den = _sampleRate };
                c.Ref.bit_rate = _bitrate;
                SetGlobalHeaderIfNeeded(c);
                _configure?.Invoke(c);
            }, _opts);
        }
    }
}
