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
            ffmpeg.av_frame_get_buffer(dst, 0);
            ffmpeg.av_frame_copy_props(dst, pFrame);
            ffmpeg.av_frame_copy(dst, pFrame).ThrowExceptionIfError();
            return dstFrame;
        }

        object ICloneable.Clone()
        {
            return Clone<MediaFrame>();
        }

        /// <summary>
        /// Get managed copy of <see cref="AVFrame.data"/> by <see cref="AVFrame.linesize"/>
        /// <para>
        /// NOTE: length maybe greater than valid data, because memory alignment.
        /// </para>
        /// </summary>
        /// <returns></returns>
        public byte[][] GetData()
        {
            List<byte[]> result = new List<byte[]>();
            IntPtr intPtr;
            for (uint i = 0; (intPtr = (IntPtr)pFrame->extended_data[i]) != IntPtr.Zero; i++)
            {
                if (0 < pFrame->channels && pFrame->channels < i)
                    break;
                byte[] line = new byte[pFrame->linesize[pFrame->channels > 0 ? 0 : i]];
                Marshal.Copy(intPtr, line, 0, line.Length);
                result.Add(line);
            }
            return result.ToArray();
        }

        public void Clear()
        {
            ffmpeg.av_frame_unref(pFrame);
        }

        public IntPtr[] Data
        {
            get
            {
                List<IntPtr> result = new List<IntPtr>();
                IntPtr intPtr;
                for (uint i = 0; (intPtr = (IntPtr)pFrame->extended_data[i]) != IntPtr.Zero; i++)
                {
                    if (0 < pFrame->channels && pFrame->channels < i)
                        break;
                    result.Add(intPtr);
                }
                return result.ToArray();
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
        public static VideoFrame CreateFrame(MediaCodec codec)
        {
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

#if NETFRAMEWORK

        public System.Drawing.Bitmap ToBitmap()
        {
            var width = pFrame->width;
            var height = pFrame->height;
            var stride = pFrame->linesize[0];
            var data = pFrame->data;
            var format = (AVPixelFormat)pFrame->format;
            switch (format)
            {
                case AVPixelFormat.AV_PIX_FMT_BGRA:
                    return new System.Drawing.Bitmap(width, height, stride, System.Drawing.Imaging.PixelFormat.Format32bppArgb, (IntPtr)data[0]);

                case AVPixelFormat.AV_PIX_FMT_BGR24:
                    return new System.Drawing.Bitmap(width, height, stride, System.Drawing.Imaging.PixelFormat.Format24bppRgb, (IntPtr)data[0]);

                case AVPixelFormat.AV_PIX_FMT_GRAY8:
                    return new System.Drawing.Bitmap(width, height, stride, System.Drawing.Imaging.PixelFormat.Format8bppIndexed, (IntPtr)data[0]);

                case AVPixelFormat.AV_PIX_FMT_BGR0:
                    return new System.Drawing.Bitmap(width, height, stride, System.Drawing.Imaging.PixelFormat.Format32bppRgb, (IntPtr)data[0]);

                default:
                    throw new FFmpegException(ffmpeg.AVERROR(ffmpeg.EINVAL));
            }
        }

#endif
    }

    public unsafe class AudioFrame : MediaFrame
    {

        public static AudioFrame CreateFrame(MediaCodec codec)
        {
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

        public byte[][] ToSamples()
        {
            if (pFrame->data[0] == null)
                return null;
            int samplesize = ffmpeg.av_get_bytes_per_sample((AVSampleFormat)pFrame->format);
            int planarsize = samplesize * pFrame->nb_samples;
            byte[][] result;
            if (ffmpeg.av_sample_fmt_is_planar((AVSampleFormat)pFrame->format) > 0)
            {
                result = new byte[pFrame->channels][];
                for (uint ch = 0; ch < pFrame->channels; ch++)
                {
                    result[ch] = new byte[planarsize];
                    Marshal.Copy((IntPtr)pFrame->data[ch], result[ch], 0, planarsize);
                }
            }
            else
            {
                result = new byte[1][];
                int totalsize = planarsize * pFrame->channels;
                result[0] = new byte[totalsize];
                Marshal.Copy((IntPtr)pFrame->data[0], result[0], 0, totalsize);
            }
            return result;
        }
    }
}