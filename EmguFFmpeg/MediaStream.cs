using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;

namespace EmguFFmpeg
{
    public unsafe class MediaStream
    {
        public MediaCodec Codec { get; set; }

        internal MediaStream(AVStream* stream)
        {
            pStream = stream;
        }

        public bool CanRead => Codec is null ? false : Codec.IsDecoder;
        public bool CanWrite => Codec is null ? false : Codec.IsEncoder;

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

        public IEnumerable<MediaFrame> ReadFrame(MediaPacket packet)
        {
            if (!CanRead)
                throw new NotSupportedException();
            if (packet.StreamIndex != Index)
                throw new ArgumentException();
            return (Codec as MediaDecode).DecodePacket(packet);
        }

        public IEnumerable<MediaPacket> WriteFrame(MediaFrame frame)
        {
            if (!CanWrite)
                throw new NotSupportedException();
            foreach (var packet in (Codec as MediaEncode).EncodeFrame(frame))
            {
                FixPacket(packet);
                yield return packet;
            }
        }

        /// <summary>
        /// convert packet pts from <see cref="AVCodecContext.time_base"/> to <see
        /// cref="AVStream.time_base"/> and set <see cref="AVPacket.stream_index"/>
        /// </summary>
        /// <param name="packet"></param>
        public void FixPacket(MediaPacket packet)
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
                throw new ArgumentOutOfRangeException(nameof(pts), pts, "");
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

        protected AVStream* pStream = null;
    }
}