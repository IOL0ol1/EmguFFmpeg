using System;
using System.Collections.Generic;
using System.Linq;
using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    public unsafe class MediaEncoder : IDisposable
    {
        private bool disposedValue;



        #region Video
        public static MediaEncoder CreateVideoEncoder(
            OutFormat format,
            int width,
            int height,
            AVRational frameRate,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            Action<MediaCodecContextSettings> contextSettings = null,
            MediaDictionary opts = null)
        {
            return new MediaEncoder(new MediaCodecContext(MediaCodec.FindEncoder(format.VideoCodec)).Open(_ =>
            {
                _.Width = width;
                _.Height = height;
                _.TimeBase = frameRate.ToInvert();
                _.Framerate = frameRate;
                if (pixelFormat == AVPixelFormat.AV_PIX_FMT_NONE)
                {
                    var pixelFmts = new MediaCodec(_.Context.codec).GetPixelFmts();
                    pixelFormat = pixelFmts.FirstOrDefault();
                }
                _.PixFmt = pixelFormat;

                _.BitRate = bitrate;
                if ((format.Flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
                    _.Flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
                contextSettings?.Invoke(_);
            }, null, opts));
        }

        public static MediaEncoder CreateVideoEncoder(
            MediaCodec codec,
            int width,
            int height,
            AVRational frameRate,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContextSettings> contextSettings = null,
            MediaDictionary opts = null)
        {
            return new MediaEncoder(new MediaCodecContext(codec).Open(_ =>
            {
                _.Width = width;
                _.Height = height;
                _.TimeBase = frameRate.ToInvert();
                _.Framerate = frameRate;
                if (pixelFormat == AVPixelFormat.AV_PIX_FMT_NONE)
                {
                    var pixelFmts = new MediaCodec(_.Context.codec).GetPixelFmts();
                    pixelFormat = pixelFmts.FirstOrDefault();
                }
                _.PixFmt = pixelFormat;
                _.BitRate = bitrate;
                _.Flags |= flags;
                contextSettings?.Invoke(_);
            }, null, opts));
        }

        public static MediaEncoder CreateVideoEncoder(
            MediaCodec codec,
            int width,
            int height,
            double fps,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContextSettings> contextSettings = null,
            MediaDictionary opts = null)
        {
            return CreateVideoEncoder(codec, width, height, fps.ToRational(), pixelFormat, bitrate, flags, contextSettings, opts);
        }

        public static MediaEncoder CreateVideoEncoder(
            OutFormat format,
            int width,
            int height,
            double fps,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            Action<MediaCodecContextSettings> contextSettings = null,
            int bitrate = 0, MediaDictionary opts = null)
        {
            return CreateVideoEncoder(format, width, height, fps.ToRational(), pixelFormat, bitrate, contextSettings, opts);
        }

        public static MediaEncoder CreateVideoEncoder(
            AVCodecID codecID,
            int width,
            int height,
            double fps,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContextSettings> contextSettings = null,
            MediaDictionary opts = null)
        {
            return CreateVideoEncoder(MediaCodec.FindEncoder(codecID), width, height, fps.ToRational(), pixelFormat, bitrate, flags, contextSettings, opts);
        }

        public static MediaEncoder CreateVideoEncoder(
            AVCodecID codecID,
            int width,
            int height,
            AVRational frameRate,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContextSettings> contextSettings = null,
            MediaDictionary opts = null)
        {
            return CreateVideoEncoder(MediaCodec.FindEncoder(codecID), width, height, frameRate, pixelFormat, bitrate, flags, contextSettings, opts);
        }

        public static MediaEncoder CreateVideoEncoder(
            string codecName,
            int width,
            int height,
            double fps,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContextSettings> contextSettings = null,
            MediaDictionary opts = null)
        {
            return CreateVideoEncoder(MediaCodec.FindEncoder(codecName), width, height, fps.ToRational(), pixelFormat, bitrate, flags, contextSettings, opts);
        }

        public static MediaEncoder CreateVideoEncoder(
            string codecName,
            int width,
            int height,
            AVRational frameRate,
            AVPixelFormat pixelFormat = AVPixelFormat.AV_PIX_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContextSettings> contextSettings = null,
            MediaDictionary opts = null)
        {
            return CreateVideoEncoder(MediaCodec.FindEncoder(codecName), width, height, frameRate, pixelFormat, bitrate, flags, contextSettings, opts);
        }
        #endregion

        #region Audio

        public static MediaEncoder CreateAudioEncoder(
            OutFormat format,
            int sampleRate,
            AVChannelLayout chLayout,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            Action<MediaCodecContextSettings> contextSettings = null,
            MediaDictionary opts = null)
        {
            return new MediaEncoder(new MediaCodecContext(MediaCodec.FindEncoder(format.VideoCodec)).Open(_ =>
            {
                _.SampleRate = sampleRate;
                _.ChLayout = chLayout;
                if (sampleFormat == AVSampleFormat.AV_SAMPLE_FMT_NONE)
                {
                    var sampleFmts = new MediaCodec(_.Context.codec).GetSampelFmts();
                    sampleFormat = sampleFmts.FirstOrDefault();
                }
                _.SampleFmt = sampleFormat;
                _.TimeBase = new AVRational { num = 1, den = sampleRate };
                _.BitRate = bitrate;
                if ((format.Flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
                    _.Flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
                contextSettings?.Invoke(_);
            }, null, opts));
        }

        public static MediaEncoder CreateAudioEncoder(
            OutFormat format,
            int sampleRate,
            int nbChannels,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            Action<MediaCodecContextSettings> contextSettings = null,
            MediaDictionary opts = null)
        {
            return CreateAudioEncoder(format, sampleRate, AVChannelLayoutExtension.Default(nbChannels), sampleFormat, bitrate, contextSettings, opts);
        }

        public static MediaEncoder CreateAudioEncoder(
            MediaCodec codec,
            int sampleRate,
            AVChannelLayout chLayout,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContextSettings> contextSettings = null,
            MediaDictionary opts = null)
        {
            return new MediaEncoder(new MediaCodecContext(codec).Open(_ =>
            {
                _.SampleRate = sampleRate;
                _.ChLayout = chLayout;
                if (sampleFormat == AVSampleFormat.AV_SAMPLE_FMT_NONE)
                {
                    var sampleFmts = new MediaCodec(_.Context.codec).GetSampelFmts();
                    sampleFormat = sampleFmts.FirstOrDefault();
                }
                _.SampleFmt = sampleFormat;
                _.TimeBase = new AVRational { num = 1, den = sampleRate };
                _.BitRate = bitrate;
                _.Flags |= flags;
                contextSettings?.Invoke(_);
            }, null, opts));
        }
        public static MediaEncoder CreateAudioEncoder(
            MediaCodec codec,
            int sampleRate,
            int nbChannels,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContextSettings> contextSettings = null,
            MediaDictionary opts = null)
        {
            return CreateAudioEncoder(codec, sampleRate, AVChannelLayoutExtension.Default(nbChannels), sampleFormat, bitrate, flags, contextSettings, opts);
        }

        public static MediaEncoder CreateAudioEncoder(
            AVCodecID codecID,
            int sampleRate,
            int nbChannels,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContextSettings> contextSettings = null,
            MediaDictionary opts = null)
        {
            return CreateAudioEncoder(MediaCodec.FindEncoder(codecID), sampleRate, AVChannelLayoutExtension.Default(nbChannels), sampleFormat, bitrate, flags, contextSettings, opts);
        }

        public static MediaEncoder CreateAudioEncoder(
            AVCodecID codecID,
            int sampleRate,
            AVChannelLayout chLayout,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContextSettings> contextSettings = null,
            MediaDictionary opts = null)
        {
            return CreateAudioEncoder(MediaCodec.FindEncoder(codecID), sampleRate, chLayout, sampleFormat, bitrate, flags, contextSettings, opts);
        }

        public static MediaEncoder CreateAudioEncoder(
            string codecName,
            int sampleRate,
            int nbChannels,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContextSettings> contextSettings = null,
            MediaDictionary opts = null)
        {
            return CreateAudioEncoder(MediaCodec.FindEncoder(codecName), sampleRate, AVChannelLayoutExtension.Default(nbChannels), sampleFormat, bitrate, flags, contextSettings, opts);
        }

        public static MediaEncoder CreateAudioEncoder(
            string codecName,
            int sampleRate,
            AVChannelLayout chLayout,
            AVSampleFormat sampleFormat = AVSampleFormat.AV_SAMPLE_FMT_NONE,
            int bitrate = 0,
            int flags = ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER,
            Action<MediaCodecContextSettings> contextSettings = null,
            MediaDictionary opts = null)
        {
            return CreateAudioEncoder(MediaCodec.FindEncoder(codecName), sampleRate, chLayout, sampleFormat, bitrate, flags, contextSettings, opts);
        }

        #endregion

        public static MediaEncoder CreateEncoder(AVCodecParameters codecParameters, MediaDictionary opts = null)
        {
            var codec = MediaCodec.FindEncoder(codecParameters.codec_id);
            AVCodecParameters* pCodecParameters = &codecParameters;
            // If codec_id is AV_CODEC_ID_NONE return null
            return codec == null
                ? null
                : new MediaEncoder(MediaCodecContext.Create(_ => ffmpeg.avcodec_parameters_to_context(_, pCodecParameters).ThrowIfError(), codec, opts));
        }

        public static implicit operator MediaCodecContext(MediaEncoder value)
        {
            if (value == null) return null;
            return value.Context;
        }

        public static implicit operator AVCodecContext*(MediaEncoder value)
        {
            if (value == null) return null;
            return value.Context;
        }

        public MediaCodecContext Context { get; private set; }

        public MediaEncoder(MediaCodecContext context, bool isDisposeByOwner = true)
        {
            Context = context;
            disposedValue = !isDisposeByOwner;
        }

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
        public int SendFrame(MediaFrame frame) => ffmpeg.avcodec_send_frame(Context, frame);

        /// <summary>
        /// <see cref="ffmpeg.avcodec_receive_packet(AVCodecContext*, AVPacket*)"/>
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public int ReceivePacket(MediaPacket packet) => ffmpeg.avcodec_receive_packet(Context, packet);



        /// <summary>
        /// encode frame to packet
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="inPacket"></param>
        /// <returns></returns>
        public IEnumerable<MediaPacket> EncodeFrame(MediaFrame frame, MediaPacket inPacket = null)
        {
            int ret = SendFrame(frame);
            if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
                yield break;
            ret.ThrowIfError();
            MediaPacket packet = inPacket ?? new MediaPacket();
            try
            {
                while (true)
                {
                    ret = ReceivePacket(packet);
                    if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
                        yield break;
                    try
                    {
                        ret.ThrowIfError();
                        yield return packet;
                    }
                    finally { packet.Unref(); }
                }
            }
            finally
            {
                if (inPacket == null) packet.Dispose();
                frame?.MakeWritable();
            }
        }



        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // nothing
                }

                Context?.Dispose();
                disposedValue = true;
            }
        }

        ~MediaEncoder()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }
}
