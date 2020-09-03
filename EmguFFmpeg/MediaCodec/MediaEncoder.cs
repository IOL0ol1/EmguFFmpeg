using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace EmguFFmpeg
{
    public class MediaEncoder : MediaCodec
    {
        #region static create

        public static MediaEncoder CreateEncode(AVCodecID codecId, int flags, Action<MediaCodec> setBeforeOpen = null, MediaDictionary opts = null)
        {
            MediaEncoder encode = new MediaEncoder(codecId);
            encode.Initialize(setBeforeOpen, flags, opts);
            return encode;
        }

        public static MediaEncoder CreateEncode(string codecName, int flags, Action<MediaCodec> setBeforeOpen = null, MediaDictionary opts = null)
        {
            MediaEncoder encode = new MediaEncoder(codecName);
            encode.Initialize(setBeforeOpen, flags, opts);
            return encode;
        }

        public static MediaEncoder CreateVideoEncode(OutFormat oformat, int width, int height, int fps, long bitRate = 0, AVPixelFormat format = AVPixelFormat.AV_PIX_FMT_NONE)
        {
            return CreateVideoEncode(oformat.VideoCodec, oformat.Flags, width, height, fps, bitRate, format);
        }

        /// <summary>
        /// Create and init video encode
        /// </summary>
        /// <param name="videoCodec"></param>
        /// <param name="flags"><see cref="MediaFormat.Flags"/></param>
        /// <param name="width">width pixel, must be greater than 0</param>
        /// <param name="height">height pixel, must be greater than 0</param>
        /// <param name="fps">fps, must be greater than 0</param>
        /// <param name="bitRate">default is auto bit rate, must be greater than or equal to 0</param>
        /// <param name="format">default is first supported pixel format</param>
        /// <returns></returns>
        public static MediaEncoder CreateVideoEncode(AVCodecID videoCodec, int flags, int width, int height, int fps, long bitRate = 0, AVPixelFormat format = AVPixelFormat.AV_PIX_FMT_NONE)
        {
            unsafe
            {
                return CreateEncode(videoCodec, flags, _ =>
                {
                    AVCodecContext* pCodecContext = _;
                    if (width <= 0 || height <= 0 || fps <= 0 || bitRate < 0)
                        throw new FFmpegException(FFmpegException.NonNegative);
                    if (_.SupportedPixelFmts.Count() <= 0)
                        throw new FFmpegException(FFmpegException.NotSupportCodecId);
                    if (format == AVPixelFormat.AV_PIX_FMT_NONE)
                        format = _.SupportedPixelFmts[0];
                    else if (_.SupportedPixelFmts.Where(__ => __ == format).Count() <= 0)
                        throw new FFmpegException(FFmpegException.NotSupportFormat);
                    pCodecContext->width = width;
                    pCodecContext->height = height;
                    pCodecContext->time_base = new AVRational { num = 1, den = fps };
                    pCodecContext->pix_fmt = format;
                    pCodecContext->bit_rate = bitRate;
                });
            }
        }

        public static MediaEncoder CreateAudioEncode(OutFormat Oformat, ulong channelLayout, int sampleRate, long bitRate = 0, AVSampleFormat format = AVSampleFormat.AV_SAMPLE_FMT_NONE)
        {
            return CreateAudioEncode(Oformat.AudioCodec, Oformat.Flags, channelLayout, sampleRate, bitRate, format);
        }

        public static MediaEncoder CreateAudioEncode(OutFormat Oformat, int channels, int sampleRate, long bitRate = 0, AVSampleFormat format = AVSampleFormat.AV_SAMPLE_FMT_NONE)
        {
            return CreateAudioEncode(Oformat.AudioCodec, Oformat.Flags, (ulong)ffmpeg.av_get_default_channel_layout(channels), sampleRate, bitRate, format);
        }

        public static MediaEncoder CreateAudioEncode(AVCodecID audioCodec, int flags, int channels, int sampleRate = 0, long bitRate = 0, AVSampleFormat format = AVSampleFormat.AV_SAMPLE_FMT_NONE)
        {
            return CreateAudioEncode(audioCodec, flags, FFmpegHelper.GetChannelLayout(channels), sampleRate, bitRate, format);
        }

        /// <summary>
        /// Create and init audio encode
        /// </summary>
        /// <param name="audioCodec"></param>
        /// <param name="flags"><see cref="MediaFormat.Flags"/></param>
        /// <param name="channelLayout">channel layout see <see cref="AVChannelLayout"/></param>
        /// <param name="sampleRate">default is first supported sample rates, must be greater than 0</param>
        /// <param name="bitRate">default is auto bit rate, must be greater than or equal to 0</param>
        /// <param name="format">default is first supported pixel format</param>
        /// <returns></returns>
        public static MediaEncoder CreateAudioEncode(AVCodecID audioCodec, int flags, ulong channelLayout, int sampleRate, long bitRate = 0, AVSampleFormat format = AVSampleFormat.AV_SAMPLE_FMT_NONE)
        {
            unsafe
            {
                return CreateEncode(audioCodec, flags, _ =>
                {
                    AVCodecContext* pCodecContext = _;
                    if (channelLayout <= 0 || sampleRate <= 0 || bitRate < 0)
                        throw new FFmpegException(FFmpegException.NonNegative);
                    if (_.SupportedSampelFmts.Count() <= 0 || _.SupportedSampleRates.Count <= 0)
                        throw new FFmpegException(FFmpegException.NotSupportCodecId);
                    // check or set sampleRate
                    if (sampleRate <= 0)
                        sampleRate = _.SupportedSampleRates[0];
                    else if (_.SupportedSampleRates.Where(__ => __ == sampleRate).Count() <= 0)
                        throw new FFmpegException(FFmpegException.NotSupportSampleRate);
                    // check or set format
                    if (format == AVSampleFormat.AV_SAMPLE_FMT_NONE)
                        format = _.SupportedSampelFmts[0];
                    else if (_.SupportedSampelFmts.Where(__ => __ == format).Count() <= 0)
                        throw new FFmpegException(FFmpegException.NotSupportFormat);
                    // check channelLayout when SupportedChannelLayout.Count() > 0
                    if (_.SupportedChannelLayout.Count() > 0
                             && _.SupportedChannelLayout.Where(__ => __ == channelLayout).Count() <= 0)
                        throw new FFmpegException(FFmpegException.NotSupportChLayout);
                    pCodecContext->sample_rate = sampleRate;
                    pCodecContext->channel_layout = channelLayout;
                    pCodecContext->sample_fmt = format;
                    pCodecContext->channels = ffmpeg.av_get_channel_layout_nb_channels(pCodecContext->channel_layout);
                    pCodecContext->bit_rate = bitRate;
                });
            }
        }

        #endregion

        /// <summary>
        /// Find encoder by id
        /// <para>
        /// Must call <see cref="Initialize(Action{MediaCodec}, int, MediaDictionary)"/> before encode
        /// </para>
        /// </summary>
        /// <param name="codecId">codec id</param>
        public MediaEncoder(AVCodecID codecId)
        {
            unsafe
            {
                if ((pCodec = ffmpeg.avcodec_find_encoder(codecId)) == null && codecId != AVCodecID.AV_CODEC_ID_NONE)
                    throw new FFmpegException(ffmpeg.AVERROR_ENCODER_NOT_FOUND);
            }
        }

        /// <summary>
        /// Find encoder by name
        /// <para>
        /// Must call <see cref="Initialize(Action{MediaCodec}, int, MediaDictionary)"/> before encode
        /// </para>
        /// </summary>
        /// <param name="codecName">codec name</param>
        public MediaEncoder(string codecName)
        {
            unsafe
            {
                if ((pCodec = ffmpeg.avcodec_find_encoder_by_name(codecName)) == null)
                    throw new FFmpegException(ffmpeg.AVERROR_ENCODER_NOT_FOUND);
            }
        }

        internal unsafe MediaEncoder(AVCodec* codec)
        {
            pCodec = codec;
        }

        /// <summary>
        /// alloc <see cref="AVCodecContext"/> and <see cref="ffmpeg.avcodec_open2(AVCodecContext*, AVCodec*, AVDictionary**)"/>
        /// </summary>
        /// <param name="setBeforeOpen">
        /// set <see cref="AVCodecContext"/> after <see cref="ffmpeg.avcodec_alloc_context3(AVCodec*)"/> and before <see cref="ffmpeg.avcodec_open2(AVCodecContext*, AVCodec*, AVDictionary**)"/>
        /// </param>
        /// <param name="flags">
        /// check <see cref="MediaFormat.Flags"/> &amp; <see cref="ffmpeg.AVFMT_GLOBALHEADER"/> set <see cref="ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER"/>
        /// </param>
        /// <param name="opts">options for "avcodec_open2"</param>
        public override void Initialize(Action<MediaCodec> setBeforeOpen = null, int flags = 0, MediaDictionary opts = null)
        {
            unsafe
            {
                pCodecContext = ffmpeg.avcodec_alloc_context3(pCodec);
                setBeforeOpen?.Invoke(this);
                if ((flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
                {
                    pCodecContext->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
                }
                if (pCodec != null)
                {
                    ffmpeg.avcodec_open2(pCodecContext, pCodec, opts).ThrowExceptionIfError();
                }
            }
        }

        /// <summary>
        /// pre process frame
        /// </summary>
        /// <param name="frame"></param>
        private void RemoveSideData(MediaFrame frame)
        {
            unsafe
            {
                if (frame != null)
                {
                    // Make sure Closed Captions will not be duplicated
                    if (AVCodecContext.codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                        ffmpeg.av_frame_remove_side_data(frame, AVFrameSideDataType.AV_FRAME_DATA_A53_CC);
                }
            }
        }

        /// <summary>
        /// TODO: add SubtitleFrame support
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public virtual IEnumerable<MediaPacket> EncodeFrame(MediaFrame frame)
        {
            SendFrame(frame).ThrowExceptionIfError();
            RemoveSideData(frame);
            using (MediaPacket packet = new MediaPacket())
            {
                while (true)
                {
                    int ret = ReceivePacket(packet);
                    if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
                        break;
                    ret.ThrowExceptionIfError();
                    yield return packet;
                }
            }
        }

        public int SendFrame([In] MediaFrame frame)
        {
            unsafe
            {
                return ffmpeg.avcodec_send_frame(pCodecContext, frame);
            }
        }

        public int ReceivePacket([Out] MediaPacket packet)
        {
            unsafe
            {
                return ffmpeg.avcodec_receive_packet(pCodecContext, packet);
            }
        }

        public static MediaEncoder[] Encodes
        {
            get
            {
                unsafe
                {
                    List<MediaEncoder> result = new List<MediaEncoder>();
                    void* i = null;
                    AVCodec* p;
                    while ((p = ffmpeg.av_codec_iterate(&i)) != null)
                    {
                        if (ffmpeg.av_codec_is_encoder(p) != 0)
                            result.Add(new MediaEncoder(p));
                    }

                    return result.ToArray();
                }
            }
        }
    }
}
