using System;
using FFmpeg.AutoGen;


namespace FFmpeg.Sharp
{
    public unsafe partial class MediaStream
    {
        /// <summary>
        /// Direct access to the codec parameters of this stream.
        /// </summary>
        public ref AVCodecParameters CodecparRef => ref *pStream->codecpar;

        /// <summary>Metadata dictionary (borrowed — do not Dispose).</summary>
        public MediaDictionary Metadata => pStream->metadata == null
            ? null
            : new MediaDictionary(pStream->metadata, leaveOpen: true);

        /// <summary>
        /// Returns a cloned packet containing the attached picture (e.g. album art) for this stream, or null when not present.
        /// </summary>
        public MediaPacket AttachedPicture
        {
            get
            {
                if ((pStream->disposition & ffmpeg.AV_DISPOSITION_ATTACHED_PIC) == 0) return null;
                AVPacket* p = &pStream->attached_pic;
                AVPacket* clone = ffmpeg.av_packet_clone(p);
                return clone == null ? null : new MediaPacket(clone, leaveOpen: false);
            }
        }

        /// <summary>
        /// Convert a pts (in this stream's time base) to <see cref="TimeSpan"/>. Throws if <paramref name="pts"/> &lt; 0.
        /// </summary>
        /// <exception cref="FFmpegException"/>
        public TimeSpan ToTimeSpan(long pts)
        {
            return TimeSpan.FromSeconds(pts * ffmpeg.av_q2d(pStream->time_base));
        }

        /// <summary>Non-throwing TimeSpan conversion. Returns false on negative pts.</summary>
        public bool TryToTimeSpan(long pts, out TimeSpan timeSpan)
        {
            timeSpan = TimeSpan.Zero;
            if (pts < 0)
                return false;
            timeSpan = TimeSpan.FromSeconds(pts * ffmpeg.av_q2d(pStream->time_base));
            return true;
        }
    }
}
