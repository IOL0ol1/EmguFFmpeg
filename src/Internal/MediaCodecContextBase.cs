using FFmpeg.AutoGen;
namespace FFmpegSharp.Internal
{
    public abstract unsafe partial class MediaCodecContextBase
    {
        protected AVCodecContext* pCodecContext = null;

        public static implicit operator AVCodecContext*(MediaCodecContextBase value)
        {
            if (value == null) return null;
            return value.pCodecContext;
        }

        public MediaCodecContextBase(AVCodecContext* value)
        {
            pCodecContext = value;
        }

        public AVCodecContext Ref => *pCodecContext;

        public int LogLevelOffset
        {
            get => pCodecContext->log_level_offset;
            set => pCodecContext->log_level_offset = value;
        }

        public AVMediaType CodecType
        {
            get => pCodecContext->codec_type;
            set => pCodecContext->codec_type = value;
        }

        public AVCodecID CodecId
        {
            get => pCodecContext->codec_id;
            set => pCodecContext->codec_id = value;
        }

        public uint CodecTag
        {
            get => pCodecContext->codec_tag;
            set => pCodecContext->codec_tag = value;
        }

        public long BitRate
        {
            get => pCodecContext->bit_rate;
            set => pCodecContext->bit_rate = value;
        }

        public int BitRateTolerance
        {
            get => pCodecContext->bit_rate_tolerance;
            set => pCodecContext->bit_rate_tolerance = value;
        }

        public int GlobalQuality
        {
            get => pCodecContext->global_quality;
            set => pCodecContext->global_quality = value;
        }

        public int CompressionLevel
        {
            get => pCodecContext->compression_level;
            set => pCodecContext->compression_level = value;
        }

        public int Flags
        {
            get => pCodecContext->flags;
            set => pCodecContext->flags = value;
        }

        public int Flags2
        {
            get => pCodecContext->flags2;
            set => pCodecContext->flags2 = value;
        }

        public int ExtradataSize
        {
            get => pCodecContext->extradata_size;
            set => pCodecContext->extradata_size = value;
        }

        public AVRational TimeBase
        {
            get => pCodecContext->time_base;
            set => pCodecContext->time_base = value;
        }

        public int TicksPerFrame
        {
            get => pCodecContext->ticks_per_frame;
            set => pCodecContext->ticks_per_frame = value;
        }

        public int Delay
        {
            get => pCodecContext->delay;
            set => pCodecContext->delay = value;
        }

        public int Width
        {
            get => pCodecContext->width;
            set => pCodecContext->width = value;
        }

        public int Height
        {
            get => pCodecContext->height;
            set => pCodecContext->height = value;
        }

        public int CodedWidth
        {
            get => pCodecContext->coded_width;
            set => pCodecContext->coded_width = value;
        }

        public int CodedHeight
        {
            get => pCodecContext->coded_height;
            set => pCodecContext->coded_height = value;
        }

        public int GopSize
        {
            get => pCodecContext->gop_size;
            set => pCodecContext->gop_size = value;
        }

        public AVPixelFormat PixFmt
        {
            get => pCodecContext->pix_fmt;
            set => pCodecContext->pix_fmt = value;
        }

        public int MaxBFrames
        {
            get => pCodecContext->max_b_frames;
            set => pCodecContext->max_b_frames = value;
        }

        public float BQuantFactor
        {
            get => pCodecContext->b_quant_factor;
            set => pCodecContext->b_quant_factor = value;
        }

        public float BQuantOffset
        {
            get => pCodecContext->b_quant_offset;
            set => pCodecContext->b_quant_offset = value;
        }

        public int HasBFrames
        {
            get => pCodecContext->has_b_frames;
            set => pCodecContext->has_b_frames = value;
        }

        public float IQuantFactor
        {
            get => pCodecContext->i_quant_factor;
            set => pCodecContext->i_quant_factor = value;
        }

        public float IQuantOffset
        {
            get => pCodecContext->i_quant_offset;
            set => pCodecContext->i_quant_offset = value;
        }

        public float LumiMasking
        {
            get => pCodecContext->lumi_masking;
            set => pCodecContext->lumi_masking = value;
        }

        public float TemporalCplxMasking
        {
            get => pCodecContext->temporal_cplx_masking;
            set => pCodecContext->temporal_cplx_masking = value;
        }

        public float SpatialCplxMasking
        {
            get => pCodecContext->spatial_cplx_masking;
            set => pCodecContext->spatial_cplx_masking = value;
        }

        public float PMasking
        {
            get => pCodecContext->p_masking;
            set => pCodecContext->p_masking = value;
        }

        public float DarkMasking
        {
            get => pCodecContext->dark_masking;
            set => pCodecContext->dark_masking = value;
        }

        public int SliceCount
        {
            get => pCodecContext->slice_count;
            set => pCodecContext->slice_count = value;
        }

        public AVRational SampleAspectRatio
        {
            get => pCodecContext->sample_aspect_ratio;
            set => pCodecContext->sample_aspect_ratio = value;
        }

        public int MeCmp
        {
            get => pCodecContext->me_cmp;
            set => pCodecContext->me_cmp = value;
        }

        public int MeSubCmp
        {
            get => pCodecContext->me_sub_cmp;
            set => pCodecContext->me_sub_cmp = value;
        }

        public int MbCmp
        {
            get => pCodecContext->mb_cmp;
            set => pCodecContext->mb_cmp = value;
        }

        public int IldctCmp
        {
            get => pCodecContext->ildct_cmp;
            set => pCodecContext->ildct_cmp = value;
        }

        public int DiaSize
        {
            get => pCodecContext->dia_size;
            set => pCodecContext->dia_size = value;
        }

        public int LastPredictorCount
        {
            get => pCodecContext->last_predictor_count;
            set => pCodecContext->last_predictor_count = value;
        }

        public int MePreCmp
        {
            get => pCodecContext->me_pre_cmp;
            set => pCodecContext->me_pre_cmp = value;
        }

        public int PreDiaSize
        {
            get => pCodecContext->pre_dia_size;
            set => pCodecContext->pre_dia_size = value;
        }

        public int MeSubpelQuality
        {
            get => pCodecContext->me_subpel_quality;
            set => pCodecContext->me_subpel_quality = value;
        }

        public int MeRange
        {
            get => pCodecContext->me_range;
            set => pCodecContext->me_range = value;
        }

        public int SliceFlags
        {
            get => pCodecContext->slice_flags;
            set => pCodecContext->slice_flags = value;
        }

        public int MbDecision
        {
            get => pCodecContext->mb_decision;
            set => pCodecContext->mb_decision = value;
        }

        public int IntraDcPrecision
        {
            get => pCodecContext->intra_dc_precision;
            set => pCodecContext->intra_dc_precision = value;
        }

        public int SkipTop
        {
            get => pCodecContext->skip_top;
            set => pCodecContext->skip_top = value;
        }

        public int SkipBottom
        {
            get => pCodecContext->skip_bottom;
            set => pCodecContext->skip_bottom = value;
        }

        public int MbLmin
        {
            get => pCodecContext->mb_lmin;
            set => pCodecContext->mb_lmin = value;
        }

        public int MbLmax
        {
            get => pCodecContext->mb_lmax;
            set => pCodecContext->mb_lmax = value;
        }

        public int BidirRefine
        {
            get => pCodecContext->bidir_refine;
            set => pCodecContext->bidir_refine = value;
        }

        public int KeyintMin
        {
            get => pCodecContext->keyint_min;
            set => pCodecContext->keyint_min = value;
        }

        public int Refs
        {
            get => pCodecContext->refs;
            set => pCodecContext->refs = value;
        }

        public int Mv0Threshold
        {
            get => pCodecContext->mv0_threshold;
            set => pCodecContext->mv0_threshold = value;
        }

        public AVColorPrimaries ColorPrimaries
        {
            get => pCodecContext->color_primaries;
            set => pCodecContext->color_primaries = value;
        }

        public AVColorTransferCharacteristic ColorTrc
        {
            get => pCodecContext->color_trc;
            set => pCodecContext->color_trc = value;
        }

        public AVColorSpace Colorspace
        {
            get => pCodecContext->colorspace;
            set => pCodecContext->colorspace = value;
        }

        public AVColorRange ColorRange
        {
            get => pCodecContext->color_range;
            set => pCodecContext->color_range = value;
        }

        public AVChromaLocation ChromaSampleLocation
        {
            get => pCodecContext->chroma_sample_location;
            set => pCodecContext->chroma_sample_location = value;
        }

        public int Slices
        {
            get => pCodecContext->slices;
            set => pCodecContext->slices = value;
        }

        public AVFieldOrder FieldOrder
        {
            get => pCodecContext->field_order;
            set => pCodecContext->field_order = value;
        }

        public int SampleRate
        {
            get => pCodecContext->sample_rate;
            set => pCodecContext->sample_rate = value;
        }

        public AVSampleFormat SampleFmt
        {
            get => pCodecContext->sample_fmt;
            set => pCodecContext->sample_fmt = value;
        }

        public int FrameSize
        {
            get => pCodecContext->frame_size;
            set => pCodecContext->frame_size = value;
        }

        public int FrameNumber
        {
            get => pCodecContext->frame_number;
            set => pCodecContext->frame_number = value;
        }

        public int BlockAlign
        {
            get => pCodecContext->block_align;
            set => pCodecContext->block_align = value;
        }

        public int Cutoff
        {
            get => pCodecContext->cutoff;
            set => pCodecContext->cutoff = value;
        }

        public AVAudioServiceType AudioServiceType
        {
            get => pCodecContext->audio_service_type;
            set => pCodecContext->audio_service_type = value;
        }

        public AVSampleFormat RequestSampleFmt
        {
            get => pCodecContext->request_sample_fmt;
            set => pCodecContext->request_sample_fmt = value;
        }

        public float Qcompress
        {
            get => pCodecContext->qcompress;
            set => pCodecContext->qcompress = value;
        }

        public float Qblur
        {
            get => pCodecContext->qblur;
            set => pCodecContext->qblur = value;
        }

        public int Qmin
        {
            get => pCodecContext->qmin;
            set => pCodecContext->qmin = value;
        }

        public int Qmax
        {
            get => pCodecContext->qmax;
            set => pCodecContext->qmax = value;
        }

        public int MaxQdiff
        {
            get => pCodecContext->max_qdiff;
            set => pCodecContext->max_qdiff = value;
        }

        public int RcBufferSize
        {
            get => pCodecContext->rc_buffer_size;
            set => pCodecContext->rc_buffer_size = value;
        }

        public int RcOverrideCount
        {
            get => pCodecContext->rc_override_count;
            set => pCodecContext->rc_override_count = value;
        }

        public long RcMaxRate
        {
            get => pCodecContext->rc_max_rate;
            set => pCodecContext->rc_max_rate = value;
        }

        public long RcMinRate
        {
            get => pCodecContext->rc_min_rate;
            set => pCodecContext->rc_min_rate = value;
        }

        public float RcMaxAvailableVbvUse
        {
            get => pCodecContext->rc_max_available_vbv_use;
            set => pCodecContext->rc_max_available_vbv_use = value;
        }

        public float RcMinVbvOverflowUse
        {
            get => pCodecContext->rc_min_vbv_overflow_use;
            set => pCodecContext->rc_min_vbv_overflow_use = value;
        }

        public int RcInitialBufferOccupancy
        {
            get => pCodecContext->rc_initial_buffer_occupancy;
            set => pCodecContext->rc_initial_buffer_occupancy = value;
        }

        public int Trellis
        {
            get => pCodecContext->trellis;
            set => pCodecContext->trellis = value;
        }

        public int WorkaroundBugs
        {
            get => pCodecContext->workaround_bugs;
            set => pCodecContext->workaround_bugs = value;
        }

        public int StrictStdCompliance
        {
            get => pCodecContext->strict_std_compliance;
            set => pCodecContext->strict_std_compliance = value;
        }

        public int ErrorConcealment
        {
            get => pCodecContext->error_concealment;
            set => pCodecContext->error_concealment = value;
        }

        public int Debug
        {
            get => pCodecContext->debug;
            set => pCodecContext->debug = value;
        }

        public int ErrRecognition
        {
            get => pCodecContext->err_recognition;
            set => pCodecContext->err_recognition = value;
        }

        public long ReorderedOpaque
        {
            get => pCodecContext->reordered_opaque;
            set => pCodecContext->reordered_opaque = value;
        }

        public ulong_array8 Error
        {
            get => pCodecContext->error;
            set => pCodecContext->error = value;
        }

        public int DctAlgo
        {
            get => pCodecContext->dct_algo;
            set => pCodecContext->dct_algo = value;
        }

        public int IdctAlgo
        {
            get => pCodecContext->idct_algo;
            set => pCodecContext->idct_algo = value;
        }

        public int BitsPerCodedSample
        {
            get => pCodecContext->bits_per_coded_sample;
            set => pCodecContext->bits_per_coded_sample = value;
        }

        public int BitsPerRawSample
        {
            get => pCodecContext->bits_per_raw_sample;
            set => pCodecContext->bits_per_raw_sample = value;
        }

        public int Lowres
        {
            get => pCodecContext->lowres;
            set => pCodecContext->lowres = value;
        }

        public int ThreadCount
        {
            get => pCodecContext->thread_count;
            set => pCodecContext->thread_count = value;
        }

        public int ThreadType
        {
            get => pCodecContext->thread_type;
            set => pCodecContext->thread_type = value;
        }

        public int ActiveThreadType
        {
            get => pCodecContext->active_thread_type;
            set => pCodecContext->active_thread_type = value;
        }

        public int NsseWeight
        {
            get => pCodecContext->nsse_weight;
            set => pCodecContext->nsse_weight = value;
        }

        public int Profile
        {
            get => pCodecContext->profile;
            set => pCodecContext->profile = value;
        }

        public int Level
        {
            get => pCodecContext->level;
            set => pCodecContext->level = value;
        }

        public AVDiscard SkipLoopFilter
        {
            get => pCodecContext->skip_loop_filter;
            set => pCodecContext->skip_loop_filter = value;
        }

        public AVDiscard SkipIdct
        {
            get => pCodecContext->skip_idct;
            set => pCodecContext->skip_idct = value;
        }

        public AVDiscard SkipFrame
        {
            get => pCodecContext->skip_frame;
            set => pCodecContext->skip_frame = value;
        }

        public int SubtitleHeaderSize
        {
            get => pCodecContext->subtitle_header_size;
            set => pCodecContext->subtitle_header_size = value;
        }

        public int InitialPadding
        {
            get => pCodecContext->initial_padding;
            set => pCodecContext->initial_padding = value;
        }

        public AVRational Framerate
        {
            get => pCodecContext->framerate;
            set => pCodecContext->framerate = value;
        }

        public AVPixelFormat SwPixFmt
        {
            get => pCodecContext->sw_pix_fmt;
            set => pCodecContext->sw_pix_fmt = value;
        }

        public AVRational PktTimebase
        {
            get => pCodecContext->pkt_timebase;
            set => pCodecContext->pkt_timebase = value;
        }

        public long PtsCorrectionNumFaultyPts
        {
            get => pCodecContext->pts_correction_num_faulty_pts;
            set => pCodecContext->pts_correction_num_faulty_pts = value;
        }

        public long PtsCorrectionNumFaultyDts
        {
            get => pCodecContext->pts_correction_num_faulty_dts;
            set => pCodecContext->pts_correction_num_faulty_dts = value;
        }

        public long PtsCorrectionLastPts
        {
            get => pCodecContext->pts_correction_last_pts;
            set => pCodecContext->pts_correction_last_pts = value;
        }

        public long PtsCorrectionLastDts
        {
            get => pCodecContext->pts_correction_last_dts;
            set => pCodecContext->pts_correction_last_dts = value;
        }

        public int SubCharencMode
        {
            get => pCodecContext->sub_charenc_mode;
            set => pCodecContext->sub_charenc_mode = value;
        }

        public int SkipAlpha
        {
            get => pCodecContext->skip_alpha;
            set => pCodecContext->skip_alpha = value;
        }

        public int SeekPreroll
        {
            get => pCodecContext->seek_preroll;
            set => pCodecContext->seek_preroll = value;
        }

        public uint Properties
        {
            get => pCodecContext->properties;
            set => pCodecContext->properties = value;
        }

        public int NbCodedSideData
        {
            get => pCodecContext->nb_coded_side_data;
            set => pCodecContext->nb_coded_side_data = value;
        }

        public int TrailingPadding
        {
            get => pCodecContext->trailing_padding;
            set => pCodecContext->trailing_padding = value;
        }

        public long MaxPixels
        {
            get => pCodecContext->max_pixels;
            set => pCodecContext->max_pixels = value;
        }

        public int HwaccelFlags
        {
            get => pCodecContext->hwaccel_flags;
            set => pCodecContext->hwaccel_flags = value;
        }

        public int ApplyCropping
        {
            get => pCodecContext->apply_cropping;
            set => pCodecContext->apply_cropping = value;
        }

        public int ExtraHwFrames
        {
            get => pCodecContext->extra_hw_frames;
            set => pCodecContext->extra_hw_frames = value;
        }

        public int DiscardDamagedPercentage
        {
            get => pCodecContext->discard_damaged_percentage;
            set => pCodecContext->discard_damaged_percentage = value;
        }

        public long MaxSamples
        {
            get => pCodecContext->max_samples;
            set => pCodecContext->max_samples = value;
        }

        public int ExportSideData
        {
            get => pCodecContext->export_side_data;
            set => pCodecContext->export_side_data = value;
        }

        public AVChannelLayout ChLayout
        {
            get => pCodecContext->ch_layout;
            set => pCodecContext->ch_layout = value;
        }

    }
}
