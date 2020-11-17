using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    public unsafe class MediaStream
    {
        public MediaCodecContext Codec { get; set; }

        public static MediaStream FromNative(IntPtr pAVStream)
        {
            return FromNative((AVStream*)pAVStream);
        }

        public  static MediaStream FromNative(AVStream* stream)
        {
            return new MediaStream(stream);
        }

        internal MediaStream(AVStream* stream)
        {
            pStream = stream;
        }

        public AVRational TimeBase
        {
            get => pStream->time_base;
            set => pStream->time_base = value;
        }

        public long StartTime
        {
            get => pStream->start_time;
            set => pStream->start_time = value;
        }

        public long Duration
        {
            get => pStream->duration;
            set => pStream->duration = value;
        }

        public long FirstDts
        {
            get => pStream->first_dts;
            set => pStream->first_dts = value;
        }

        public long CurDts
        {
            get => pStream->cur_dts;
            set => pStream->cur_dts = value;
        }

        public AVStream Stream => *pStream;

        /// <summary>
        /// stream index in AVFormatContext
        /// </summary>
        public int Index => pStream->index;

        /// <summary>
        /// <see cref="ffmpeg.av_codec_is_decoder(AVCodec*)"/>
        /// </summary>
        public bool HasDecoder => Codec == null ? false : Codec.IsDecoder;

        /// <summary>
        /// <see cref="ffmpeg.av_codec_is_encoder(AVCodec*)"/>
        /// </summary>
        public bool HasEncoder => Codec == null ? false : Codec.IsEncoder;

        /// <summary>
        /// Read a fram from <see cref="MediaPacket"/>
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public IEnumerable<MediaFrame> ReadFrame(MediaPacket packet)
        {
            if (!HasDecoder)
                yield break;
            if (packet.StreamIndex != Index)
                yield break;
            foreach (var item in Codec.DecodePacket(packet))
            {
                yield return item;
            }
        }

        /// <summary>
        /// Write a fram by <see cref="Codec"/>.
        /// <para><see cref="MediaEncoder.EncodeFrame(MediaFrame)"/></para>
        /// <para><see cref="PacketRescaleTs(MediaPacket)"/></para>
        /// </summary>
        /// <param name="frame"></param>
        /// <exception cref="FFmpegException"/>
        /// <returns></returns>

        public IEnumerable<MediaPacket> WriteFrame(MediaFrame frame)
        {
            if (!HasEncoder)
                throw new FFmpegException(ffmpeg.AVERROR_ENCODER_NOT_FOUND);
            foreach (var packet in GetCodecContext().EncodeFrame(frame))
            {
                PacketRescaleTs(packet);
                packet.StreamIndex = Stream.index;
                yield return packet;
            }
        }

        private MediaCodecContext GetCodecContext()
        {
            return MediaCodecContext.FromNative(pStream->codec, false);
        }

        /// <summary>
        /// convert packet pts from <see cref="AVCodecContext.time_base"/> to <see
        /// cref="AVStream.time_base"/> and set <see cref="AVPacket.stream_index"/>
        /// </summary>
        /// <param name="packet"></param>
        public void PacketRescaleTs(MediaPacket packet)
        {
            ffmpeg.av_packet_rescale_ts(packet, pStream->codec->time_base, Stream.time_base);
        }

        /// <summary>
        /// Convert to TimeSpan use <see cref="TimeBase"/>.
        /// </summary>
        /// <remarks>
        /// throw exception when <paramref name="pts"/> &lt; 0.
        /// </remarks>
        /// <param name="pts"></param>
        /// <exception cref="FFmpegException"/>
        /// <returns></returns>
        public TimeSpan ToTimeSpan(long pts)
        {
            if (pts < 0)
                throw new FFmpegException(FFmpegException.PtsOutOfRange);
            return TimeSpan.FromSeconds(pts * ffmpeg.av_q2d(TimeBase));
        }

        /// <summary>
        /// Convert to TimeSpan use <see cref="TimeBase"/>
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public bool TryToTimeSpan(long pts, out TimeSpan timeSpan)
        {
            timeSpan = TimeSpan.Zero;
            if (pts < 0)
                return false;
            timeSpan = TimeSpan.FromSeconds(pts * ffmpeg.av_q2d(TimeBase));
            return true;
        }

        /// <summary>
        /// [Unsafe]
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVStream*(MediaStream value)
        {
            if (value == null) return null;
            return value.pStream;
        }

        private AVStream* pStream = null;
    }
}
