using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace EmguFFmpeg
{
    public unsafe class AudioFrame : MediaFrame
    {
        public static AudioFrame CreateFrameByCodec(MediaCodec codec)
        {
            if (codec.Type != AVMediaType.AVMEDIA_TYPE_AUDIO)
                throw new FFmpegException(FFmpegException.CodecTypeError);
            return new AudioFrame(codec.AVCodecContext.sample_fmt, codec.AVCodecContext.channel_layout, codec.AVCodecContext.frame_size, codec.AVCodecContext.sample_rate);
        }

        public AudioFrame() : base()
        { }

        /// <summary>
        /// </summary>
        /// <param name="format"><see cref="AVCodecContext.sample_fmt"/></param>
        /// <param name="channelLayout">see <see cref="AVChannelLayout"/></param>
        /// <param name="nbSamples">recommended use <see cref="AVCodecContext.frame_size"/></param>
        /// <param name="sampleRate"></param>
        /// <param name="align">
        /// Required buffer size alignment. If equal to 0, alignment will be chosen automatically for
        /// the current CPU. It is highly recommended to pass 0 here unless you know what you are doing.
        /// </param>
        public AudioFrame(AVSampleFormat format, ulong channelLayout, int nbSamples, int sampleRate = 0, int align = 0) : base()
        {
            AllocBuffer(format, channelLayout, nbSamples, sampleRate, align);
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

        public void Init(AVSampleFormat format, ulong channelLayout, int nbSamples, int sampleRate = 0, int align = 0)
        {
            Clear();
            AllocBuffer(format, channelLayout, nbSamples, sampleRate, align);
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

        public AudioFrame ToPlanar()
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
            AudioFrame outFrame = new AudioFrame(outFormat, pFrame->channel_layout, pFrame->nb_samples, pFrame->sample_rate);
            using (SampleConverter converter = new SampleConverter(outFrame))
            {
                return converter.ConvertFrame(this, out int _, out int __);
            }
        }

        public AudioFrame ToPacket()
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
            AudioFrame outFrame = new AudioFrame(outFormat, pFrame->channel_layout, pFrame->nb_samples, pFrame->sample_rate);
            using (SampleConverter converter = new SampleConverter(outFrame))
            {
                return converter.ConvertFrame(this, out int _, out int __);
            }
        }
    }

}