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

        /// <summary>Stream index in the parent format context.</summary>
        public int Index => pStream->index;
        /// <summary>Stream id (format-specific).</summary>
        public int Id => pStream->id;
        /// <summary>Time base used by stream's pts/dts.</summary>
        public AVRational TimeBase => pStream->time_base;
        /// <summary>Average frame rate (estimated by FFmpeg).</summary>
        public AVRational AverageFrameRate => pStream->avg_frame_rate;
        /// <summary>Real base frame rate (estimated by FFmpeg).</summary>
        public AVRational RealFrameRate => pStream->r_frame_rate;
        /// <summary>Sample aspect ratio.</summary>
        public AVRational SampleAspectRatio => pStream->sample_aspect_ratio;
        /// <summary>Stream disposition flags (AV_DISPOSITION_*).</summary>
        public int Disposition => pStream->disposition;
        /// <summary>Estimated stream duration in <see cref="TimeBase"/> units.</summary>
        public long Duration => pStream->duration;
        /// <summary>Number of frames in this stream if known, else 0.</summary>
        public long NbFrames => pStream->nb_frames;
        /// <summary>First DTS value (stream-start offset).</summary>
        public long StartTime => pStream->start_time;
        /// <summary>Media type of this stream (video / audio / subtitle / data).</summary>
        public AVMediaType MediaType => pStream->codecpar->codec_type;

        /// <summary>Metadata dictionary (borrowed — do not Dispose).</summary>
        public MediaDictionary Metadata => pStream->metadata == null
            ? null
            : new MediaDictionary(pStream->metadata, leaveOpen: true);

        /// <summary>Convenience for the most common metadata entry.</summary>
        public string LanguageTag => Metadata?["language"];

        /// <summary>True if this stream contains an attached picture (e.g. album art) reachable via <see cref="AttachedPicture"/>.</summary>
        public bool HasAttachedPicture => (pStream->disposition & ffmpeg.AV_DISPOSITION_ATTACHED_PIC) != 0;

        /// <summary>
        /// Returns a cloned packet containing the attached picture for this stream, or null when not present.
        /// </summary>
        public MediaPacket AttachedPicture
        {
            get
            {
                if (!HasAttachedPicture) return null;
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
