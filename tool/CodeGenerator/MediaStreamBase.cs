using FFmpeg.AutoGen;
namespace FFmpegSharp.Internal
{
    public abstract unsafe partial class MediaStreamBase
    {
        protected AVStream* pStream = null;

        public static implicit operator AVStream*(MediaStreamBase value)
        {
            if(value == null) return null;
            return value.pStream;
        }

        public MediaStreamBase(AVStream* value)
        {
            pStream = value;
        }

        public AVStream Ref => *pStream;

        public int Index
        {
            get=> pStream->index;
            set=> pStream->index = value;
        }

        public int Id
        {
            get=> pStream->id;
            set=> pStream->id = value;
        }

        public AVRational TimeBase
        {
            get=> pStream->time_base;
            set=> pStream->time_base = value;
        }

        public long StartTime
        {
            get=> pStream->start_time;
            set=> pStream->start_time = value;
        }

        public long Duration
        {
            get=> pStream->duration;
            set=> pStream->duration = value;
        }

        public long NbFrames
        {
            get=> pStream->nb_frames;
            set=> pStream->nb_frames = value;
        }

        public int Disposition
        {
            get=> pStream->disposition;
            set=> pStream->disposition = value;
        }

        public AVDiscard Discard
        {
            get=> pStream->discard;
            set=> pStream->discard = value;
        }

        public AVRational SampleAspectRatio
        {
            get=> pStream->sample_aspect_ratio;
            set=> pStream->sample_aspect_ratio = value;
        }

        public AVRational AvgFrameRate
        {
            get=> pStream->avg_frame_rate;
            set=> pStream->avg_frame_rate = value;
        }

        public AVPacket AttachedPic
        {
            get=> pStream->attached_pic;
            set=> pStream->attached_pic = value;
        }

        public int NbSideData
        {
            get=> pStream->nb_side_data;
            set=> pStream->nb_side_data = value;
        }

        public int EventFlags
        {
            get=> pStream->event_flags;
            set=> pStream->event_flags = value;
        }

        public AVRational RFrameRate
        {
            get=> pStream->r_frame_rate;
            set=> pStream->r_frame_rate = value;
        }

        public int PtsWrapBits
        {
            get=> pStream->pts_wrap_bits;
            set=> pStream->pts_wrap_bits = value;
        }

    }
}
