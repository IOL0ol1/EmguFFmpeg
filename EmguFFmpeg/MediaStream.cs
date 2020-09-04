using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;
using System.Linq;

namespace EmguFFmpeg
{
    public unsafe class MediaStream
    {
        public MediaCodec Codec { get; set; }

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

        public int Index => pStream->index;

        public bool HasDecoder => Codec == null ? false : Codec.IsDecoder;

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
            foreach (var item in (Codec as MediaDecoder).DecodePacket(packet))
            {
                yield return item;
            }
        }

        /// <summary>
        /// Write a fram by <see cref="Codec"/>.
        /// <para><see cref="MediaEncoder.EncodeFrame(MediaFrame)"/></para>
        /// <para><see cref="RescalePacketTs(MediaPacket)"/></para>
        /// </summary>
        /// <param name="frame"></param>
        /// <exception cref="FFmpegException"/>
        /// <returns></returns>
        public IEnumerable<MediaPacket> WriteFrame(MediaFrame frame)
        {
            if (!HasEncoder)
                throw new FFmpegException(ffmpeg.AVERROR_ENCODER_NOT_FOUND);
            foreach (var packet in (Codec as MediaEncoder).EncodeFrame(frame))
            {
                RescalePacketTs(packet);
                yield return packet;
            }
        }

        /// <summary>
        /// convert packet pts from <see cref="AVCodecContext.time_base"/> to <see
        /// cref="AVStream.time_base"/> and set <see cref="AVPacket.stream_index"/>
        /// </summary>
        /// <param name="packet"></param>
        public void RescalePacketTs(MediaPacket packet)
        {
            ffmpeg.av_packet_rescale_ts(packet, Codec.AVCodecContext.time_base, Stream.time_base);
            packet.StreamIndex = Stream.index;
        }

        /// <summary>
        /// Convert to TimeSpan use <see cref="TimeBase"/>
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        public TimeSpan ToTimeSpan(long pts)
        {
            if (pts < 0)
                throw new FFmpegException(FFmpegException.PtsOutOfRange);
            return TimeSpan.FromSeconds(pts * ffmpeg.av_q2d(TimeBase));
        }

        public bool TryToTimeSpan(long pts, out TimeSpan timeSpan)
        {
            timeSpan = TimeSpan.Zero;
            if (pts < 0)
                return false;
            timeSpan = TimeSpan.FromSeconds(pts * ffmpeg.av_q2d(TimeBase));
            return true;
        }

        public static implicit operator AVStream*(MediaStream value)
        {
            if (value == null) return null;
            return value.pStream;
        }

        private AVStream* pStream = null;
    }
}
