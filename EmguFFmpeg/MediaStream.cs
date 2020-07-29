using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;
using System.Linq;

namespace EmguFFmpeg
{
    public class MediaStream
    {
        public MediaCodec Codec { get; set; }

        internal unsafe MediaStream(AVStream* stream)
        {
            pStream = stream;
        }

        public AVRational TimeBase
        {
            get { unsafe { return pStream->time_base; } }
            set { unsafe { pStream->time_base = value; } }
        }

        public long StartTime
        {
            get { unsafe { return pStream->start_time; } }
            set { unsafe { pStream->start_time = value; } }
        }

        public long Duration
        {
            get { unsafe { return pStream->duration; } }
            set { unsafe { pStream->duration = value; } }
        }

        public long FirstDts
        {
            get { unsafe { return pStream->first_dts; } }
            set { unsafe { pStream->first_dts = value; } }
        }

        public long CurDts
        {
            get { unsafe { return pStream->cur_dts; } }
            set { unsafe { pStream->cur_dts = value; } }
        }

        public AVStream Stream { get { unsafe { return *pStream; } } }

        public int Index { get { unsafe { return pStream->index; } } }

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
            foreach (var item in (Codec as MediaDecode).DecodePacket(packet))
            {
                yield return item;
            }
        }

        /// <summary>
        /// Write a fram by <see cref="Codec"/>.
        /// <para><see cref="MediaEncode.EncodeFrame(MediaFrame)"/></para>
        /// <para><see cref="FixPacket(MediaPacket)"/></para>
        /// </summary>
        /// <param name="frame"></param>
        /// <exception cref="FFmpegException"/>
        /// <returns></returns>
        public IEnumerable<MediaPacket> WriteFrame(MediaFrame frame)
        {
            if (!HasEncoder)
                throw new FFmpegException(ffmpeg.AVERROR_ENCODER_NOT_FOUND);
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
            unsafe
            {
                ffmpeg.av_packet_rescale_ts(packet, Codec.AVCodecContext.time_base, Stream.time_base);
                packet.StreamIndex = Stream.index;
            }
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

        public unsafe static implicit operator AVStream*(MediaStream value)
        {
            if (value == null) return null;
            return value.pStream;
        }

        protected unsafe AVStream* pStream = null;
    }
}