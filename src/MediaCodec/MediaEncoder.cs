using System.Linq;
using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    public unsafe abstract class MediaEncoder
    {

        #region Video
        public static MediaCodecContext CreateVideoEncoder(
            OutFormat format,
            int width,
            int height,
            AVRational frameRate,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            MediaDictionary opts = null)
        {
            return new MediaCodecContext(MediaCodec.GetEncoder(format.VideoCodec)).Open(_ =>
            {
                _.Width = width;
                _.Height = height;
                _.TimeBase = frameRate.ToInvert();
                _.FrameRate = frameRate;
                if (pixelFormat == AVPixelFormat.AV_PIX_FMT_NONE)
                {
                    var pixelFmts = new MediaCodec(_.AVCodecContext.codec).GetSupportedPixelFmts();
                    pixelFormat = pixelFmts.FirstOrDefault();
                }
                _.PixFmt = pixelFormat;

                _.BitRate = bitrate;
                if ((format.Flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
                    _.Flag |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
            }, null, opts);
        }

        public static MediaCodecContext CreateVideoEncoder(
            MediaCodec codec,
            int width,
            int height,
            AVRational frameRate,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            MediaDictionary opts = null)
        {
            return new MediaCodecContext(codec).Open(_ =>
            {
                _.Width = width;
                _.Height = height;
                _.TimeBase = frameRate.ToInvert();
                _.FrameRate = frameRate;
                if (pixelFormat == AVPixelFormat.AV_PIX_FMT_NONE)
                {
                    var pixelFmts = new MediaCodec(_.AVCodecContext.codec).GetSupportedPixelFmts();
                    pixelFormat = pixelFmts.FirstOrDefault();
                }
                _.PixFmt = pixelFormat;
                _.BitRate = bitrate;
                _.Flag |= flags;
            }, null, opts);
        }

        public static MediaCodecContext CreateVideoEncoder(
            MediaCodec codec,
            int width,
            int height,
            double fps,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            MediaDictionary opts = null)
        {
            return CreateVideoEncoder(codec, width, height, fps.ToRational(), pixelFormat, bitrate, flags, opts);
        }

        public static MediaCodecContext CreateVideoEncoder(
            OutFormat format,
            int width,
            int height,
            double fps,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0, MediaDictionary opts = null)
        {
            return CreateVideoEncoder(format, width, height, fps.ToRational(), pixelFormat, bitrate, opts);
        }

        public static MediaCodecContext CreateVideoEncoder(
            AVCodecID codecID,
            int width,
            int height,
            double fps,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER, MediaDictionary opts = null)
        {
            return CreateVideoEncoder(MediaCodec.GetEncoder(codecID), width, height, fps.ToRational(), pixelFormat, bitrate, flags, opts);
        }

        public static MediaCodecContext CreateVideoEncoder(
            AVCodecID codecID,
            int width,
            int height,
            AVRational frameRate,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            MediaDictionary opts = null)
        {
            return CreateVideoEncoder(MediaCodec.GetEncoder(codecID), width, height, frameRate, pixelFormat, bitrate, flags, opts);
        }

        public static MediaCodecContext CreateVideoEncoder(
            string codecName,
            int width,
            int height,
            double fps,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            MediaDictionary opts = null)
        {
            return CreateVideoEncoder(MediaCodec.GetEncoder(codecName), width, height, fps.ToRational(), pixelFormat, bitrate, flags, opts);
        }

        public static MediaCodecContext CreateVideoEncoder(
            string codecName,
            int width,
            int height,
            AVRational frameRate,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            MediaDictionary opts = null)
        {
            return CreateVideoEncoder(MediaCodec.GetEncoder(codecName), width, height, frameRate, pixelFormat, bitrate, flags, opts);
        }
        #endregion

        #region Audio

        public static MediaCodecContext CreateAudioEncoder(
            OutFormat format,
            int sampleRate,
            AVChannelLayout chLayout,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            MediaDictionary opts = null)
        {
            return new MediaCodecContext(MediaCodec.GetEncoder(format.VideoCodec)).Open(_ =>
            {
                _.SampleRate = sampleRate;
                _.ChLayout = chLayout;
                if (sampleFormat == AVSampleFormat.AV_SAMPLE_FMT_NONE)
                {
                    var sampleFmts = new MediaCodec(_.AVCodecContext.codec).GetSupportedSampelFmts();
                    sampleFormat = sampleFmts.FirstOrDefault();
                }
                _.SampleFmt = sampleFormat;
                _.TimeBase = new AVRational { num = 1, den = sampleRate };
                _.BitRate = bitrate;
                if ((format.Flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
                    _.Flag |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
            }, null, opts);
        }

        public static MediaCodecContext CreateAudioEncoder(
            OutFormat format,
            int sampleRate,
            int nbChannels,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            MediaDictionary opts = null)
        {
            return CreateAudioEncoder(format, sampleRate, AVChannelLayoutExtension.Default(nbChannels), sampleFormat, bitrate, opts);
        }

        public static MediaCodecContext CreateAudioEncoder(
            MediaCodec codec,
            int sampleRate,
            AVChannelLayout chLayout,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            MediaDictionary opts = null)
        {
            return new MediaCodecContext(codec).Open(_ =>
            {
                _.SampleRate = sampleRate;
                _.ChLayout = chLayout;
                if (sampleFormat == AVSampleFormat.AV_SAMPLE_FMT_NONE)
                {
                    var sampleFmts = new MediaCodec(_.AVCodecContext.codec).GetSupportedSampelFmts();
                    sampleFormat = sampleFmts.FirstOrDefault();
                }
                _.SampleFmt = sampleFormat;
                _.TimeBase = new AVRational { num = 1, den = sampleRate };
                _.BitRate = bitrate;
                _.Flag |= flags;
            }, null, opts);
        }
        public static MediaCodecContext CreateAudioEncoder(
            MediaCodec codec,
            int sampleRate,
            int nbChannels,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            MediaDictionary opts = null)
        {
            return CreateAudioEncoder(codec, sampleRate, AVChannelLayoutExtension.Default(nbChannels), sampleFormat, bitrate, flags, opts);
        }

        public static MediaCodecContext CreateAudioEncoder(
            AVCodecID codecID,
            int sampleRate,
            int nbChannels,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            MediaDictionary opts = null)
        {
            return CreateAudioEncoder(MediaCodec.GetEncoder(codecID), sampleRate, AVChannelLayoutExtension.Default(nbChannels), sampleFormat, bitrate, flags, opts);
        }

        public static MediaCodecContext CreateAudioEncoder(
            AVCodecID codecID,
            int sampleRate,
            AVChannelLayout chLayout,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            MediaDictionary opts = null)
        {
            return CreateAudioEncoder(MediaCodec.GetEncoder(codecID), sampleRate, chLayout, sampleFormat, bitrate, flags, opts);
        }

        public static MediaCodecContext CreateAudioEncoder(
            string codecName,
            int sampleRate,
            int nbChannels,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            MediaDictionary opts = null)
        {
            return CreateAudioEncoder(MediaCodec.GetEncoder(codecName), sampleRate, AVChannelLayoutExtension.Default(nbChannels), sampleFormat, bitrate, flags, opts);
        }

        public static MediaCodecContext CreateAudioEncoder(
            string codecName,
            int sampleRate,
            AVChannelLayout chLayout,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            MediaDictionary opts = null)
        {
            return CreateAudioEncoder(MediaCodec.GetEncoder(codecName), sampleRate, chLayout, sampleFormat, bitrate, flags, opts);
        }

        #endregion

    }
}
