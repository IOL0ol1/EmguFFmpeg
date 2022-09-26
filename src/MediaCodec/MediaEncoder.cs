using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    public unsafe class MediaEncoder : MediaCodecContext
    {

        #region Video
        public static MediaCodecContext CreateVideoEncoder(
            OutFormat format,
            int width,
            int height,
            AVRational frameRate,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0)
        {
            return new MediaCodecContext(MediaCodec.GetEncoder(format.VideoCodec)).Open(_ =>
            {
                _.Width = width;
                _.Height = height;
                _.TimeBase = frameRate.ToInvert();
                _.FrameRate = frameRate;
                if (pixelFormat == AVPixelFormat.AV_PIX_FMT_NONE)
                {
                    var pixelFmts = MediaCodec.FromNative(_.AVCodecContext.codec).GetSupportedPixelFmts();
                    pixelFormat = pixelFmts.FirstOrDefault();
                }
                _.PixFmt = pixelFormat;

                _.BitRate = bitrate;
                if ((format.Flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
                    _.Flag |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
            });
        }

        public static MediaCodecContext CreateVideoEncoder(
            MediaCodec codec,
            int width,
            int height,
            AVRational frameRate,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER)
        {
            return new MediaCodecContext(codec).Open(_ =>
            {
                _.Width = width;
                _.Height = height;
                _.TimeBase = frameRate.ToInvert();
                _.FrameRate = frameRate;
                if (pixelFormat == AVPixelFormat.AV_PIX_FMT_NONE)
                {
                    var pixelFmts = MediaCodec.FromNative(_.AVCodecContext.codec).GetSupportedPixelFmts();
                    pixelFormat = pixelFmts.FirstOrDefault();
                }
                _.PixFmt = pixelFormat;
                _.BitRate = bitrate;
                _.Flag |= flags;
            });
        }

        public static MediaCodecContext CreateVideoEncoder(
            OutFormat format,
            int width,
            int height,
            double fps,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0)
        {
            return CreateVideoEncoder(format, width, height, fps.ToRational(), pixelFormat, bitrate);
        }

        public static MediaCodecContext CreateVideoEncoder(
            AVCodecID codecID,
            int width,
            int height,
            double fps,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER)
        {
            return CreateVideoEncoder(MediaCodec.GetEncoder(codecID), width, height, fps.ToRational(), pixelFormat, bitrate, flags);
        }

        public static MediaCodecContext CreateVideoEncoder(
            AVCodecID codecID,
            int width,
            int height,
            AVRational frameRate,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER)
        {
            return CreateVideoEncoder(MediaCodec.GetEncoder(codecID), width, height, frameRate, pixelFormat, bitrate, flags);
        }

        public static MediaCodecContext CreateVideoEncoder(
            string codecName,
            int width,
            int height,
            double fps,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER)
        {
            return CreateVideoEncoder(MediaCodec.GetEncoder(codecName), width, height, fps.ToRational(), pixelFormat, bitrate, flags);
        }

        public static MediaCodecContext CreateVideoEncoder(
            string codecName,
            int width,
            int height,
            AVRational frameRate,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER)
        {
            return CreateVideoEncoder(MediaCodec.GetEncoder(codecName), width, height, frameRate, pixelFormat, bitrate, flags);
        }
        #endregion

        #region Audio

        public static MediaCodecContext CreateAudioEncoder(
            OutFormat format,
            int sampleRate,
            AVChannelLayout chLayout,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0)
        {
            return new MediaCodecContext(MediaCodec.GetEncoder(format.AudioCodec)).Open(_ =>
            {
                _.SampleRate = sampleRate;
                _.ChLayout = chLayout;
                if (sampleFormat == AVSampleFormat.AV_SAMPLE_FMT_NONE)
                {
                    var sampleFmts = MediaCodec.FromNative(_.AVCodecContext.codec).GetSupportedSampelFmts();
                    sampleFormat = sampleFmts.FirstOrDefault();
                }
                _.SampleFmt = sampleFormat;
                _.TimeBase = new AVRational { num = 1, den = sampleRate };
                _.BitRate = bitrate;
            });
        }

        public static MediaCodecContext CreateAudioEncoder(
            MediaCodec codec,
            int sampleRate,
            AVChannelLayout chLayout,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0)
        {
            return new MediaCodecContext(codec).Open(_ =>
            {
                _.SampleRate = sampleRate;
                _.ChLayout = chLayout;
                if (sampleFormat == AVSampleFormat.AV_SAMPLE_FMT_NONE)
                {
                    var sampleFmts = MediaCodec.FromNative(_.AVCodecContext.codec).GetSupportedSampelFmts();
                    sampleFormat = sampleFmts.FirstOrDefault();
                }
                _.SampleFmt = sampleFormat;
                _.BitRate = bitrate;
            });
        }

        #endregion

    }
}
