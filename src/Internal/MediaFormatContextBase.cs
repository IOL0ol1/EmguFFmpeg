using FFmpeg.AutoGen;
namespace FFmpegSharp.Internal
{
    public abstract unsafe partial class MediaFormatContextBase
    {
        protected AVFormatContext* pFormatContext = null;

        public static implicit operator AVFormatContext*(MediaFormatContextBase value)
        {
            if(value == null) return null;
            return value.pFormatContext;
        }

        public MediaFormatContextBase(AVFormatContext* value)
        {
            pFormatContext = value;
        }

        public AVFormatContext Ref => *pFormatContext;

        public int CtxFlags
        {
            get=> pFormatContext->ctx_flags;
            set=> pFormatContext->ctx_flags = value;
        }

        public uint NbStreams
        {
            get=> pFormatContext->nb_streams;
            set=> pFormatContext->nb_streams = value;
        }

        public long StartTime
        {
            get=> pFormatContext->start_time;
            set=> pFormatContext->start_time = value;
        }

        public long Duration
        {
            get=> pFormatContext->duration;
            set=> pFormatContext->duration = value;
        }

        public long BitRate
        {
            get=> pFormatContext->bit_rate;
            set=> pFormatContext->bit_rate = value;
        }

        public uint PacketSize
        {
            get=> pFormatContext->packet_size;
            set=> pFormatContext->packet_size = value;
        }

        public int MaxDelay
        {
            get=> pFormatContext->max_delay;
            set=> pFormatContext->max_delay = value;
        }

        public int Flags
        {
            get=> pFormatContext->flags;
            set=> pFormatContext->flags = value;
        }

        public long Probesize
        {
            get=> pFormatContext->probesize;
            set=> pFormatContext->probesize = value;
        }

        public long MaxAnalyzeDuration
        {
            get=> pFormatContext->max_analyze_duration;
            set=> pFormatContext->max_analyze_duration = value;
        }

        public int Keylen
        {
            get=> pFormatContext->keylen;
            set=> pFormatContext->keylen = value;
        }

        public uint NbPrograms
        {
            get=> pFormatContext->nb_programs;
            set=> pFormatContext->nb_programs = value;
        }

        public AVCodecID VideoCodecId
        {
            get=> pFormatContext->video_codec_id;
            set=> pFormatContext->video_codec_id = value;
        }

        public AVCodecID AudioCodecId
        {
            get=> pFormatContext->audio_codec_id;
            set=> pFormatContext->audio_codec_id = value;
        }

        public AVCodecID SubtitleCodecId
        {
            get=> pFormatContext->subtitle_codec_id;
            set=> pFormatContext->subtitle_codec_id = value;
        }

        public uint MaxIndexSize
        {
            get=> pFormatContext->max_index_size;
            set=> pFormatContext->max_index_size = value;
        }

        public uint MaxPictureBuffer
        {
            get=> pFormatContext->max_picture_buffer;
            set=> pFormatContext->max_picture_buffer = value;
        }

        public uint NbChapters
        {
            get=> pFormatContext->nb_chapters;
            set=> pFormatContext->nb_chapters = value;
        }

        public long StartTimeRealtime
        {
            get=> pFormatContext->start_time_realtime;
            set=> pFormatContext->start_time_realtime = value;
        }

        public int FpsProbeSize
        {
            get=> pFormatContext->fps_probe_size;
            set=> pFormatContext->fps_probe_size = value;
        }

        public int ErrorRecognition
        {
            get=> pFormatContext->error_recognition;
            set=> pFormatContext->error_recognition = value;
        }

        public AVIOInterruptCB InterruptCallback
        {
            get=> pFormatContext->interrupt_callback;
            set=> pFormatContext->interrupt_callback = value;
        }

        public int Debug
        {
            get=> pFormatContext->debug;
            set=> pFormatContext->debug = value;
        }

        public long MaxInterleaveDelta
        {
            get=> pFormatContext->max_interleave_delta;
            set=> pFormatContext->max_interleave_delta = value;
        }

        public int StrictStdCompliance
        {
            get=> pFormatContext->strict_std_compliance;
            set=> pFormatContext->strict_std_compliance = value;
        }

        public int EventFlags
        {
            get=> pFormatContext->event_flags;
            set=> pFormatContext->event_flags = value;
        }

        public int MaxTsProbe
        {
            get=> pFormatContext->max_ts_probe;
            set=> pFormatContext->max_ts_probe = value;
        }

        public int AvoidNegativeTs
        {
            get=> pFormatContext->avoid_negative_ts;
            set=> pFormatContext->avoid_negative_ts = value;
        }

        public int TsId
        {
            get=> pFormatContext->ts_id;
            set=> pFormatContext->ts_id = value;
        }

        public int AudioPreload
        {
            get=> pFormatContext->audio_preload;
            set=> pFormatContext->audio_preload = value;
        }

        public int MaxChunkDuration
        {
            get=> pFormatContext->max_chunk_duration;
            set=> pFormatContext->max_chunk_duration = value;
        }

        public int MaxChunkSize
        {
            get=> pFormatContext->max_chunk_size;
            set=> pFormatContext->max_chunk_size = value;
        }

        public int UseWallclockAsTimestamps
        {
            get=> pFormatContext->use_wallclock_as_timestamps;
            set=> pFormatContext->use_wallclock_as_timestamps = value;
        }

        public int AvioFlags
        {
            get=> pFormatContext->avio_flags;
            set=> pFormatContext->avio_flags = value;
        }

        public AVDurationEstimationMethod DurationEstimationMethod
        {
            get=> pFormatContext->duration_estimation_method;
            set=> pFormatContext->duration_estimation_method = value;
        }

        public long SkipInitialBytes
        {
            get=> pFormatContext->skip_initial_bytes;
            set=> pFormatContext->skip_initial_bytes = value;
        }

        public uint CorrectTsOverflow
        {
            get=> pFormatContext->correct_ts_overflow;
            set=> pFormatContext->correct_ts_overflow = value;
        }

        public int Seek2any
        {
            get=> pFormatContext->seek2any;
            set=> pFormatContext->seek2any = value;
        }

        public int FlushPackets
        {
            get=> pFormatContext->flush_packets;
            set=> pFormatContext->flush_packets = value;
        }

        public int ProbeScore
        {
            get=> pFormatContext->probe_score;
            set=> pFormatContext->probe_score = value;
        }

        public int FormatProbesize
        {
            get=> pFormatContext->format_probesize;
            set=> pFormatContext->format_probesize = value;
        }

        public int IoRepositioned
        {
            get=> pFormatContext->io_repositioned;
            set=> pFormatContext->io_repositioned = value;
        }

        public int MetadataHeaderPadding
        {
            get=> pFormatContext->metadata_header_padding;
            set=> pFormatContext->metadata_header_padding = value;
        }

        public long OutputTsOffset
        {
            get=> pFormatContext->output_ts_offset;
            set=> pFormatContext->output_ts_offset = value;
        }

        public AVCodecID DataCodecId
        {
            get=> pFormatContext->data_codec_id;
            set=> pFormatContext->data_codec_id = value;
        }

        public int MaxStreams
        {
            get=> pFormatContext->max_streams;
            set=> pFormatContext->max_streams = value;
        }

        public int SkipEstimateDurationFromPts
        {
            get=> pFormatContext->skip_estimate_duration_from_pts;
            set=> pFormatContext->skip_estimate_duration_from_pts = value;
        }

        public int MaxProbePackets
        {
            get=> pFormatContext->max_probe_packets;
            set=> pFormatContext->max_probe_packets = value;
        }

    }
}
