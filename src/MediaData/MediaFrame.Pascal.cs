using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    /// <summary>
    /// PascalCase convenience accessors for the most-used <see cref="AVFrame"/> fields.
    /// These are pure passthrough to <see cref="MediaFrame.Ref"/> — use them for readability, use <see cref="MediaFrame.Ref"/>
    /// for everything else (the snake_case escape hatch).
    /// </summary>
    public unsafe partial class MediaFrame
    {
        /// <summary>Video width in pixels.</summary>
        public int Width
        {
            get => pFrame->width;
            set => pFrame->width = value;
        }

        /// <summary>Video height in pixels.</summary>
        public int Height
        {
            get => pFrame->height;
            set => pFrame->height = value;
        }

        /// <summary>Pixel/sample format. For video this is <see cref="AVPixelFormat"/>; for audio <see cref="AVSampleFormat"/>.</summary>
        public int Format
        {
            get => pFrame->format;
            set => pFrame->format = value;
        }

        /// <summary>Pixel format (typed). Convenience for video frames.</summary>
        public AVPixelFormat PixelFormat
        {
            get => (AVPixelFormat)pFrame->format;
            set => pFrame->format = (int)value;
        }

        /// <summary>Sample format (typed). Convenience for audio frames.</summary>
        public AVSampleFormat SampleFormat
        {
            get => (AVSampleFormat)pFrame->format;
            set => pFrame->format = (int)value;
        }

        /// <summary>Presentation timestamp.</summary>
        public long Pts
        {
            get => pFrame->pts;
            set => pFrame->pts = value;
        }

        /// <summary>DTS (best-effort) — for video this is typically the same as <see cref="Pts"/>.</summary>
        public long PacketDts
        {
            get => pFrame->pkt_dts;
            set => pFrame->pkt_dts = value;
        }

        /// <summary>Sample rate (audio). 0 if unset.</summary>
        public int SampleRate
        {
            get => pFrame->sample_rate;
            set => pFrame->sample_rate = value;
        }

        /// <summary>Number of audio samples per channel in this frame.</summary>
        public int NbSamples
        {
            get => pFrame->nb_samples;
            set => pFrame->nb_samples = value;
        }

        /// <summary>Per-plane line size (linesize[0]) — common shortcut.</summary>
        public int LineSize0 => pFrame->linesize[0];

        /// <summary>Time base associated with this frame (set by the producer; not always present).</summary>
        public AVRational TimeBase
        {
            get => pFrame->time_base;
            set => pFrame->time_base = value;
        }

        /// <summary>Audio channel layout.</summary>
        public AVChannelLayout ChannelLayout
        {
            get => pFrame->ch_layout;
            set => pFrame->ch_layout = value;
        }

        /// <summary>Frame number for video, or -1 if not set.</summary>
        public long BestEffortTimestamp
        {
            get => pFrame->best_effort_timestamp;
            set => pFrame->best_effort_timestamp = value;
        }
    }
}
