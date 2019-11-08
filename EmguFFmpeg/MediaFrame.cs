using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace EmguFFmpeg
{
    public unsafe class MediaFrame : IDisposable, ICloneable
    {
        protected AVFrame* pFrame;

        public MediaFrame()
        {
            pFrame = ffmpeg.av_frame_alloc();
        }

        public AVFrame AVFrame => *pFrame;

        /// <summary>
        /// Deep clone frame
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Clone<T>() where T : MediaFrame, new()
        {
            T dstFrame = new T();
            AVFrame* dst = dstFrame;
            dst->format = pFrame->format;
            dst->width = pFrame->width;
            dst->height = pFrame->height;
            dst->channel_layout = pFrame->channel_layout;
            dst->channels = pFrame->channels;
            dst->nb_samples = pFrame->nb_samples;
            dst->sample_rate = pFrame->sample_rate;
            if (ffmpeg.av_frame_is_writable(pFrame) != 0)
            {
                ffmpeg.av_frame_get_buffer(dst, 0).ThrowExceptionIfError();
                ffmpeg.av_frame_copy(dst, pFrame).ThrowExceptionIfError();
            }
            ffmpeg.av_frame_copy_props(dst, pFrame).ThrowExceptionIfError();
            return dstFrame;
        }

        object ICloneable.Clone()
        {
            return Clone<MediaFrame>();
        }

        public void Clear()
        {
            ffmpeg.av_frame_unref(pFrame);
        }

        #region GetData

        /// <summary>
        /// Get managed copy of <see cref="AVFrame.data"/>
        /// <para>
        /// reference <see cref="ffmpeg.av_frame_copy(AVFrame*, AVFrame*)"/>
        /// </para>
        /// </summary>
        /// <returns></returns>
        public byte[][] GetData()
        {
            if (pFrame->width > 0 && pFrame->height > 0)
                return GetVideoData();
            else if (pFrame->nb_samples > 0 && pFrame->channels > 0)
                return GetAudioData();
            throw new FFmpegException(FFmpegException.ErrorMessages.InvalidFrame);
        }

        /// <summary>
        /// reference <see cref="ffmpeg.av_image_copy(ref byte_ptrArray4, ref int_array4, ref byte_ptrArray4, int_array4, AVPixelFormat, int, int)"/>
        /// </summary>
        /// <returns></returns>
        private byte[][] GetVideoData()
        {
            List<byte[]> result = new List<byte[]>();
            AVPixFmtDescriptor* desc = ffmpeg.av_pix_fmt_desc_get((AVPixelFormat)pFrame->format);
            if (desc == null || (desc->flags & ffmpeg.AV_PIX_FMT_FLAG_HWACCEL) != 0)
                throw new FFmpegException(FFmpegException.ErrorMessages.NotSupportFrame);

            if ((desc->flags & ffmpeg.AV_PIX_FMT_FLAG_PAL) != 0 || (desc->flags & ffmpeg.AV_PIX_FMT_FLAG_PSEUDOPAL) != 0)
            {
                result.Add(GetPlane((IntPtr)pFrame->data[0], pFrame->linesize[0], pFrame->width, pFrame->height));
                if ((desc->flags & ffmpeg.AV_PIX_FMT_FLAG_PAL) != 0 && pFrame->data[1] != null)
                {
                    byte[] line1 = new byte[4 * 256];
                    Marshal.Copy((IntPtr)pFrame->data[1], line1, 0, line1.Length);
                    result.Add(line1);
                }
            }
            else
            {
                int i, planes_nb = 0;
                for (i = 0; i < desc->nb_components; i++)
                    planes_nb = Math.Max(planes_nb, desc->comp[(uint)i].plane + 1);
                for (i = 0; i < planes_nb; i++)
                {
                    int h = pFrame->height;
                    int bwidth = ffmpeg.av_image_get_linesize((AVPixelFormat)pFrame->format, pFrame->width, i);
                    bwidth.ThrowExceptionIfError();
                    if (i == 1 || i == 2)
                        h = (int)Math.Ceiling((double)pFrame->height / (1 << desc->log2_chroma_h));
                    result.Add(GetPlane((IntPtr)pFrame->data[(uint)i], pFrame->linesize[(uint)i], bwidth, h));
                }
            }
            return result.ToArray();
        }

        private byte[] GetPlane(IntPtr srcData, int linesize, int bytewidth, int height)
        {
            if (linesize < bytewidth)
                throw new FFmpegException(FFmpegException.ErrorMessages.LineSizeError);
            byte[] result = new byte[height * linesize];
            for (int i = 0; i < height; i++)
                Marshal.Copy(IntPtr.Add(srcData, i * linesize), result, i * linesize, bytewidth);
            return result;
        }

        /// <summary>
        /// reference <see cref="ffmpeg.av_samples_copy(byte**, byte**, int, int, int, int, AVSampleFormat)"/>
        /// </summary>
        /// <returns></returns>
        private byte[][] GetAudioData()
        {
            List<byte[]> result = new List<byte[]>();
            int planar = ffmpeg.av_sample_fmt_is_planar((AVSampleFormat)pFrame->format);
            int planes = planar != 0 ? pFrame->channels : 1;
            int block_align = ffmpeg.av_get_bytes_per_sample((AVSampleFormat)pFrame->format) * (planar != 0 ? 1 : pFrame->channels);
            int data_size = pFrame->nb_samples * block_align;
            IntPtr intPtr;
            for (uint i = 0; (intPtr = (IntPtr)pFrame->extended_data[i]) != IntPtr.Zero && i < planes; i++)
            {
                byte[] line = new byte[data_size];
                Marshal.Copy(intPtr, line, 0, data_size);
                result.Add(line);
            }
            return result.ToArray();
        }

        #endregion

        public IReadOnlyList<IntPtr> Data
        {
            get
            {
                List<IntPtr> result = new List<IntPtr>();
                IntPtr intPtr;
                for (uint i = 0; (intPtr = (IntPtr)pFrame->extended_data[i]) != IntPtr.Zero; i++)
                    result.Add(intPtr);
                return result;
            }
        }

        public int[] Linesize
        {
            get => pFrame->linesize;
        }

        public byte** ExtendedData
        {
            get => pFrame->extended_data;
        }

        public int Width
        {
            get => pFrame->width;
            set => pFrame->width = value;
        }

        public int Height
        {
            get => pFrame->height;
            set => pFrame->height = value;
        }

        public int NbSamples
        {
            get => pFrame->nb_samples;
            set => pFrame->nb_samples = value;
        }

        public int Format
        {
            get => pFrame->format;
            set => pFrame->format = value;
        }

        public int KeyFrame
        {
            get => pFrame->key_frame;
            set => pFrame->key_frame = value;
        }

        public AVPictureType PictType
        {
            get => pFrame->pict_type;
            set => pFrame->pict_type = value;
        }

        public AVRational SampleAspectRatio
        {
            get => pFrame->sample_aspect_ratio;
            set => pFrame->sample_aspect_ratio = value;
        }

        public long Pts
        {
            get => pFrame->pts;
            set => pFrame->pts = value;
        }

        public long PktPts
        {
            get => pFrame->pkt_pts;
        }

        public long PktDts
        {
            get => pFrame->pkt_dts;
        }

        public int SampleRate
        {
            get => pFrame->sample_rate;
            set => pFrame->sample_rate = value;
        }

        public ulong ChannelLayout
        {
            get => pFrame->channel_layout;
            set => pFrame->channel_layout = value;
        }

        public int Flags
        {
            get => pFrame->flags;
            set => pFrame->flags = value;
        }

        public long BestEffortTimestamp
        {
            get => pFrame->best_effort_timestamp;
        }

        public long PktPos
        {
            get => pFrame->pkt_pos;
        }

        public long PktDuration
        {
            get => pFrame->pkt_duration;
        }

        public int DecodeErrorFlags
        {
            get => pFrame->decode_error_flags;
        }

        public int Channels
        {
            get => pFrame->channels;
        }

        public int PktSize
        {
            get => pFrame->pkt_size;
        }

        public static implicit operator AVFrame*(MediaFrame value)
        {
            if (value == null) return null;
            return value.pFrame;
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                fixed (AVFrame** ppFrame = &pFrame)
                {
                    ffmpeg.av_frame_free(ppFrame);
                }

                disposedValue = true;
            }
        }

        ~MediaFrame()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    public unsafe class VideoFrame : MediaFrame
    {
        public static VideoFrame CreateFrameByCodec(MediaCodec codec)
        {
            if (codec.Type != AVMediaType.AVMEDIA_TYPE_VIDEO)
                throw new FFmpegException(FFmpegException.ErrorMessages.CodecTypeError);
            return new VideoFrame(codec.AVCodecContext.pix_fmt, codec.AVCodecContext.width, codec.AVCodecContext.height);
        }

        public VideoFrame() : base()
        { }

        public VideoFrame(AVPixelFormat format, int width, int height, int align = 0) : base()
        {
            AllocBuffer(format, width, height, align);
        }

        private void AllocBuffer(AVPixelFormat format, int width, int height, int align = 0)
        {
            if (ffmpeg.av_frame_is_writable(pFrame) != 0)
                return;
            pFrame->format = (int)format;
            pFrame->width = width;
            pFrame->height = height;
            ffmpeg.av_frame_get_buffer(pFrame, align);
        }

        public void Init(AVPixelFormat format, int width, int height, int align = 0)
        {
            Clear();
            AllocBuffer(format, width, height, align);
        }
    }

    public unsafe class AudioFrame : MediaFrame
    {
        public static AudioFrame CreateFrameByCodec(MediaCodec codec)
        {
            if (codec.Type != AVMediaType.AVMEDIA_TYPE_AUDIO)
                throw new FFmpegException(FFmpegException.ErrorMessages.CodecTypeError);
            return new AudioFrame(codec.AVCodecContext.sample_fmt, (AVChannelLayout)codec.AVCodecContext.channel_layout, codec.AVCodecContext.frame_size, codec.AVCodecContext.sample_rate);
        }

        public AudioFrame() : base()
        { }

        /// <summary>
        /// </summary>
        /// <param name="format"><see cref="AVCodecContext.sample_fmt"/></param>
        /// <param name="channelLayout"><see cref="AVCodecContext.channel_layout"/></param>
        /// <param name="nbSamples"><see cref="AVCodecContext.frame_size"/></param>
        /// <param name="sampleRate"><see cref="AVCodecContext.sample_rate"/></param>
        /// <param name="align">
        /// Required buffer size alignment. If equal to 0, alignment will be chosen automatically for
        /// the current CPU. It is highly recommended to pass 0 here unless you know what you are doing.
        /// </param>
        public AudioFrame(AVSampleFormat format, AVChannelLayout channelLayout, int nbSamples, int sampleRate = 0, int align = 0) : base()
        {
            AllocBuffer(format, (ulong)channelLayout, nbSamples, sampleRate, align);
        }

        public AudioFrame(AVSampleFormat format, int channels, int nbSamples, int sampleRate = 0, int align = 0) : base()
        {
            AllocBuffer(format, (ulong)ffmpeg.av_get_default_channel_layout(channels), nbSamples, sampleRate, align);
        }

        private void AllocBuffer(AVSampleFormat format, ulong channelLayout, int nbSamples, int sampleRate = 0, int align = 0)
        {
            if (ffmpeg.av_frame_is_writable(pFrame) != 0)
                return;
            pFrame->format = (int)format;
            pFrame->channel_layout = channelLayout;
            pFrame->nb_samples = nbSamples;
            pFrame->sample_rate = sampleRate;
            ffmpeg.av_frame_get_buffer(pFrame, align);
        }

        public void Init(AVSampleFormat format, int channels, int nbSamples, int sampleRate = 0, int align = 0)
        {
            Clear();
            AllocBuffer(format, (ulong)ffmpeg.av_get_default_channel_layout(channels), nbSamples, sampleRate, align);
        }

        public void Init(AVSampleFormat format, AVChannelLayout channelLayout, int nbSamples, int sampleRate = 0, int align = 0)
        {
            Clear();
            AllocBuffer(format, (ulong)channelLayout, nbSamples, sampleRate, align);
        }

        /// <summary>
        /// refance <see cref="ffmpeg.av_samples_set_silence(byte**, int, int, int, AVSampleFormat)"/>
        /// </summary>
        /// <param name="offset">sample offset</param>
        /// <param name="fill">default is 0x00</param>
        public void SetSilence(int offset, params byte[] fill)
        {
            fill = (fill == null || fill.Length < 1) ? new byte[] { 0x00 } : fill;
            AVSampleFormat sample_fmt = (AVSampleFormat)pFrame->format;
            int planar = ffmpeg.av_sample_fmt_is_planar(sample_fmt);
            int planes = planar != 0 ? pFrame->channels : 1;
            int block_align = ffmpeg.av_get_bytes_per_sample(sample_fmt) * (planar != 0 ? 1 : pFrame->channels);
            int data_size = pFrame->nb_samples * block_align;

            if ((sample_fmt == AVSampleFormat.AV_SAMPLE_FMT_U8 || sample_fmt == AVSampleFormat.AV_SAMPLE_FMT_U8P))
            {
                for (int i = 0; i < fill.Length; i++)
                    fill[i] &= 0x80;
            }

            offset *= block_align; // convert to byte offset

            int fill_size = data_size - offset; // number of bytes to fill per plane
            List<byte> fill_data = new List<byte>(); // data to fill per plane
            while (fill_data.Count < fill_size)
                fill_data.AddRange(fill);

            for (int i = 0; i < planes; i++)
            {
                Marshal.Copy(fill_data.ToArray(), 0, IntPtr.Add((IntPtr)pFrame->extended_data[(uint)i], offset), fill_size);
            }
        }
    }
}