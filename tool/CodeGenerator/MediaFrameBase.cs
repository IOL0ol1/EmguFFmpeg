using FFmpeg.AutoGen;
namespace FFmpegSharp.Internal
{
    public abstract unsafe partial class MediaFrameBase
    {
        protected AVFrame* pFrame = null;

        public static implicit operator AVFrame*(MediaFrameBase value)
        {
            if(value == null) return null;
            return value.pFrame;
        }

        public MediaFrameBase(AVFrame* value)
        {
            pFrame = value;
        }

        public AVFrame Ref => *pFrame;

        public byte_ptrArray8 Data
        {
            get=> pFrame->data;
            set=> pFrame->data = value;
        }

        public int_array8 Linesize
        {
            get=> pFrame->linesize;
            set=> pFrame->linesize = value;
        }

        public int Width
        {
            get=> pFrame->width;
            set=> pFrame->width = value;
        }

        public int Height
        {
            get=> pFrame->height;
            set=> pFrame->height = value;
        }

        public int NbSamples
        {
            get=> pFrame->nb_samples;
            set=> pFrame->nb_samples = value;
        }

        public int Format
        {
            get=> pFrame->format;
            set=> pFrame->format = value;
        }

        public int KeyFrame
        {
            get=> pFrame->key_frame;
            set=> pFrame->key_frame = value;
        }

        public AVPictureType PictType
        {
            get=> pFrame->pict_type;
            set=> pFrame->pict_type = value;
        }

        public AVRational SampleAspectRatio
        {
            get=> pFrame->sample_aspect_ratio;
            set=> pFrame->sample_aspect_ratio = value;
        }

        public long Pts
        {
            get=> pFrame->pts;
            set=> pFrame->pts = value;
        }

        public long PktDts
        {
            get=> pFrame->pkt_dts;
            set=> pFrame->pkt_dts = value;
        }

        public AVRational TimeBase
        {
            get=> pFrame->time_base;
            set=> pFrame->time_base = value;
        }

        public int CodedPictureNumber
        {
            get=> pFrame->coded_picture_number;
            set=> pFrame->coded_picture_number = value;
        }

        public int DisplayPictureNumber
        {
            get=> pFrame->display_picture_number;
            set=> pFrame->display_picture_number = value;
        }

        public int Quality
        {
            get=> pFrame->quality;
            set=> pFrame->quality = value;
        }

        public int RepeatPict
        {
            get=> pFrame->repeat_pict;
            set=> pFrame->repeat_pict = value;
        }

        public int InterlacedFrame
        {
            get=> pFrame->interlaced_frame;
            set=> pFrame->interlaced_frame = value;
        }

        public int TopFieldFirst
        {
            get=> pFrame->top_field_first;
            set=> pFrame->top_field_first = value;
        }

        public int PaletteHasChanged
        {
            get=> pFrame->palette_has_changed;
            set=> pFrame->palette_has_changed = value;
        }

        public long ReorderedOpaque
        {
            get=> pFrame->reordered_opaque;
            set=> pFrame->reordered_opaque = value;
        }

        public int SampleRate
        {
            get=> pFrame->sample_rate;
            set=> pFrame->sample_rate = value;
        }

        public AVBufferRef_ptrArray8 Buf
        {
            get=> pFrame->buf;
            set=> pFrame->buf = value;
        }

        public int NbExtendedBuf
        {
            get=> pFrame->nb_extended_buf;
            set=> pFrame->nb_extended_buf = value;
        }

        public int NbSideData
        {
            get=> pFrame->nb_side_data;
            set=> pFrame->nb_side_data = value;
        }

        public int Flags
        {
            get=> pFrame->flags;
            set=> pFrame->flags = value;
        }

        public AVColorRange ColorRange
        {
            get=> pFrame->color_range;
            set=> pFrame->color_range = value;
        }

        public AVColorPrimaries ColorPrimaries
        {
            get=> pFrame->color_primaries;
            set=> pFrame->color_primaries = value;
        }

        public AVColorTransferCharacteristic ColorTrc
        {
            get=> pFrame->color_trc;
            set=> pFrame->color_trc = value;
        }

        public AVColorSpace Colorspace
        {
            get=> pFrame->colorspace;
            set=> pFrame->colorspace = value;
        }

        public AVChromaLocation ChromaLocation
        {
            get=> pFrame->chroma_location;
            set=> pFrame->chroma_location = value;
        }

        public long BestEffortTimestamp
        {
            get=> pFrame->best_effort_timestamp;
            set=> pFrame->best_effort_timestamp = value;
        }

        public long PktPos
        {
            get=> pFrame->pkt_pos;
            set=> pFrame->pkt_pos = value;
        }

        public long PktDuration
        {
            get=> pFrame->pkt_duration;
            set=> pFrame->pkt_duration = value;
        }

        public int DecodeErrorFlags
        {
            get=> pFrame->decode_error_flags;
            set=> pFrame->decode_error_flags = value;
        }

        public int PktSize
        {
            get=> pFrame->pkt_size;
            set=> pFrame->pkt_size = value;
        }

        public ulong CropTop
        {
            get=> pFrame->crop_top;
            set=> pFrame->crop_top = value;
        }

        public ulong CropBottom
        {
            get=> pFrame->crop_bottom;
            set=> pFrame->crop_bottom = value;
        }

        public ulong CropLeft
        {
            get=> pFrame->crop_left;
            set=> pFrame->crop_left = value;
        }

        public ulong CropRight
        {
            get=> pFrame->crop_right;
            set=> pFrame->crop_right = value;
        }

        public AVChannelLayout ChLayout
        {
            get=> pFrame->ch_layout;
            set=> pFrame->ch_layout = value;
        }

    }
}
