using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace EmguFFmpeg
{
    public class AudioFrame : MediaFrame
    {
        public static AudioFrame CreateFrameByCodec(MediaCodec codec)
        {
            unsafe
            {
                if (codec.Type != AVMediaType.AVMEDIA_TYPE_AUDIO)
                    throw new FFmpegException(FFmpegException.CodecTypeError);
                AudioFrame audioFrame = new AudioFrame(codec.AVCodecContext.channels, codec.AVCodecContext.frame_size, codec.AVCodecContext.sample_fmt, codec.AVCodecContext.sample_rate);
                if (codec.AVCodecContext.channel_layout > 0)
                    audioFrame.pFrame->channel_layout = codec.AVCodecContext.channel_layout;
                return audioFrame;
            }
        }

        public AudioFrame() : base()
        { }

        public AudioFrame(int channels, int nbSamples, AVSampleFormat format, int sampleRate = 0, int align = 0) : base()
        {
            unsafe
            {
                AllocBuffer(channels, nbSamples, format, sampleRate, align);
                pFrame->channel_layout = (ulong)ffmpeg.av_get_default_channel_layout(channels);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="channelLayout">see <see cref="AVChannelLayout"/></param>
        /// <param name="nbSamples">recommended use <see cref="AVCodecContext.frame_size"/></param>
        /// <param name="format"><see cref="AVCodecContext.sample_fmt"/></param>
        /// <param name="sampleRate"></param>
        /// <param name="align">
        /// Required buffer size alignment. If equal to 0, alignment will be chosen automatically for
        /// the current CPU. It is highly recommended to pass 0 here unless you know what you are doing.
        /// </param>
        public AudioFrame(AVChannelLayout channelLayout, int nbSamples, AVSampleFormat format, int sampleRate = 0, int align = 0)
            : this(ffmpeg.av_get_channel_layout_nb_channels((ulong)channelLayout), nbSamples, format, sampleRate, align)
        {
            unsafe
            {
                pFrame->channel_layout = (ulong)channelLayout;
            }
        }

        private void AllocBuffer(int channels, int nbSamples, AVSampleFormat format, int sampleRate = 0, int align = 0)
        {
            unsafe
            {
                if (ffmpeg.av_frame_is_writable(pFrame) != 0)
                    return;
                pFrame->format = (int)format;
                pFrame->channels = channels;
                pFrame->nb_samples = nbSamples;
                pFrame->sample_rate = sampleRate;
                ffmpeg.av_frame_get_buffer(pFrame, align);
            }
        }

        public void Init(int channels, int nbSamples, AVSampleFormat format, int sampleRate = 0, int align = 0)
        {
            Clear();
            AllocBuffer(channels, nbSamples, format, sampleRate, align);
        }

        public void Init(AVChannelLayout channelLayout, int nbSamples, AVSampleFormat format, int sampleRate = 0, int align = 0)
        {
            Clear();
            AllocBuffer(ffmpeg.av_get_channel_layout_nb_channels((ulong)channelLayout), nbSamples, format, sampleRate, align);
            unsafe
            {
                pFrame->channel_layout = (ulong)channelLayout;
            }
        }

        /// <summary>
        /// refance <see cref="ffmpeg.av_samples_set_silence(byte**, int, int, int, AVSampleFormat)"/>
        /// </summary>
        /// <param name="offset">sample offset</param>
        /// <param name="fill">
        /// default is new byte[] { 0x00 }
        /// <para>
        /// if fill is {0x01, 0x02}, loop fill data by {0x01, 0x02, 0x01, 0x02 ...}, all channels are the same.
        /// </para>
        /// </param>
        public void SetSilence(int offset = 0, params byte[] fill)
        {
            unsafe
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
                    Marshal.Copy(fill_data.ToArray(), 0, (IntPtr)pFrame->extended_data[(uint)i] + offset, fill_size);
                }
            }
        }

        /// <summary>
        /// convert current frame to planar frame.
        /// if current frame is planer return current frame
        /// else return new packet frame.
        /// </summary>
        /// <returns></returns>
        public AudioFrame ToPlanar()
        {
            unsafe
            {
                if (ffmpeg.av_sample_fmt_is_planar((AVSampleFormat)pFrame->format) > 0)
                    return this;
                AVSampleFormat outFormat = (AVSampleFormat)pFrame->format;
                if (outFormat == AVSampleFormat.AV_SAMPLE_FMT_NB || outFormat == AVSampleFormat.AV_SAMPLE_FMT_NONE)
                    throw new FFmpegException(FFmpegException.NotSupportFormat);
                switch ((AVSampleFormat)pFrame->format)
                {
                    case AVSampleFormat.AV_SAMPLE_FMT_U8:
                        outFormat = AVSampleFormat.AV_SAMPLE_FMT_U8P;
                        break;

                    case AVSampleFormat.AV_SAMPLE_FMT_S16:
                        outFormat = AVSampleFormat.AV_SAMPLE_FMT_S16P;
                        break;

                    case AVSampleFormat.AV_SAMPLE_FMT_S32:
                        outFormat = AVSampleFormat.AV_SAMPLE_FMT_S32P;
                        break;

                    case AVSampleFormat.AV_SAMPLE_FMT_FLT:
                        outFormat = AVSampleFormat.AV_SAMPLE_FMT_FLTP;
                        break;

                    case AVSampleFormat.AV_SAMPLE_FMT_DBL:
                        outFormat = AVSampleFormat.AV_SAMPLE_FMT_DBLP;
                        break;

                    case AVSampleFormat.AV_SAMPLE_FMT_S64:
                        outFormat = AVSampleFormat.AV_SAMPLE_FMT_S64P;
                        break;
                }
                AudioFrame outFrame = new AudioFrame(pFrame->channels, pFrame->nb_samples, outFormat, pFrame->sample_rate);
                outFrame.pFrame->channel_layout = pFrame->channel_layout;
                using (SampleConverter converter = new SampleConverter(outFrame))
                {
                    return converter.ConvertFrame(this, out int _, out int __);
                }
            }
        }

        /// <summary>
        /// convert current frame to packet frame.
        /// if current frame is planer return new packet frame
        /// else return current frame.
        /// </summary>
        /// <returns></returns>
        public AudioFrame ToPacket()
        {
            unsafe
            {
                if (ffmpeg.av_sample_fmt_is_planar((AVSampleFormat)pFrame->format) <= 0)
                    return this;
                AVSampleFormat outFormat = (AVSampleFormat)pFrame->format;
                if (outFormat == AVSampleFormat.AV_SAMPLE_FMT_NB || outFormat == AVSampleFormat.AV_SAMPLE_FMT_NONE)
                    throw new FFmpegException(FFmpegException.NotSupportFormat);
                switch ((AVSampleFormat)pFrame->format)
                {
                    case AVSampleFormat.AV_SAMPLE_FMT_U8P:
                        outFormat = AVSampleFormat.AV_SAMPLE_FMT_U8;
                        break;

                    case AVSampleFormat.AV_SAMPLE_FMT_S16P:
                        outFormat = AVSampleFormat.AV_SAMPLE_FMT_S16;
                        break;

                    case AVSampleFormat.AV_SAMPLE_FMT_S32P:
                        outFormat = AVSampleFormat.AV_SAMPLE_FMT_S32;
                        break;

                    case AVSampleFormat.AV_SAMPLE_FMT_FLTP:
                        outFormat = AVSampleFormat.AV_SAMPLE_FMT_FLT;
                        break;

                    case AVSampleFormat.AV_SAMPLE_FMT_DBLP:
                        outFormat = AVSampleFormat.AV_SAMPLE_FMT_DBL;
                        break;

                    case AVSampleFormat.AV_SAMPLE_FMT_S64P:
                        outFormat = AVSampleFormat.AV_SAMPLE_FMT_S64;
                        break;
                }
                AudioFrame outFrame = new AudioFrame(pFrame->channels, pFrame->nb_samples, outFormat, pFrame->sample_rate);
                outFrame.pFrame->channel_layout = pFrame->channel_layout;
                using (SampleConverter converter = new SampleConverter(outFrame))
                {
                    return converter.ConvertFrame(this, out int _, out int __);
                }
            }
        }

        /// <summary>
        /// A full copy.
        /// </summary>
        /// <returns></returns>
        public override MediaFrame Copy()
        {
            unsafe
            {
                AudioFrame dstFrame = new AudioFrame();
                AVFrame* dst = dstFrame;
                dst->format = pFrame->format;
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
        }
    }
}