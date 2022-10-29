﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using FFmpeg.AutoGen;
using FFmpegSharp.Internal;

namespace FFmpegSharp
{
    public unsafe class MediaFrame : MediaFrameBase, IDisposable, ICloneable
    {
        public MediaFrame(AVFrame* frame, bool isDisposeByOwner = true)
            : base(frame)
        {
            disposedValue = !isDisposeByOwner;
        }

        /// <summary>
        /// <see cref="ffmpeg.av_frame_alloc()"/>
        /// </summary>
        public MediaFrame() : this(ffmpeg.av_frame_alloc(), true)
        { }

        public static MediaFrame CreateVideoFrame(int width, int height, AVPixelFormat pixelFormat, int align = 0)
        {
            var f = new MediaFrame();
            f.pFrame->format = (int)pixelFormat;
            f.pFrame->width = width;
            f.pFrame->height = height;
            f.AllocateBuffer(align);
            return f;
        }

        public static MediaFrame CreateAudioFrame(int channels, int nbSamples, AVSampleFormat format, int sampleRate = 0, int align = 0)
        {
            var f = new MediaFrame();
            f.pFrame->format = (int)format;
            ffmpeg.av_channel_layout_default(&f.pFrame->ch_layout, channels);
            f.pFrame->nb_samples = nbSamples;
            f.pFrame->sample_rate = sampleRate;
            f.AllocateBuffer(align);
            return f;
        }

        public static MediaFrame CreateAudioFrame(AVChannelLayout channelLayout, int nbSamples, AVSampleFormat format, int sampleRate = 0, int align = 0)
        {
            return CreateAudioFrame(channelLayout.nb_channels, nbSamples, format, sampleRate, align);
        }

        public void AllocateBuffer(int align = 0)
        {
            ffmpeg.av_frame_get_buffer(pFrame, align).ThrowIfError();
        }

        public bool IsAudioFrame => pFrame->nb_samples > 0 && pFrame->ch_layout.nb_channels > 0;

        public bool IsVideoFrame => pFrame->width > 0 && pFrame->height > 0;

        #region Get Managed Copy Of Data

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
            else if (pFrame->nb_samples > 0 && pFrame->ch_layout.nb_channels > 0)
                return GetAudioData();
            throw new FFmpegException(ffmpeg.AVERROR_INVALIDDATA);
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
                throw new FFmpegException(ffmpeg.FFERRTAG('I', 'N', 'D', 'A'));

            if ((desc->flags & ffmpeg.AV_PIX_FMT_FLAG_PAL) != 0)
            {
                result.Add(GetVideoPlane((IntPtr)pFrame->data[0], pFrame->linesize[0], pFrame->width, pFrame->height));
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
                    bwidth.ThrowIfError();
                    if (i == 1 || i == 2)
                        h = (int)Math.Ceiling((double)pFrame->height / (1 << desc->log2_chroma_h));
                    result.Add(GetVideoPlane((IntPtr)pFrame->data[(uint)i], pFrame->linesize[(uint)i], bwidth, h));
                }
            }
            return result.ToArray();
        }

        private byte[] GetVideoPlane(IntPtr srcData, int linesize, int bytewidth, int height)
        {
            if (linesize < bytewidth)
                throw new FFmpegException(ffmpeg.AVERROR_INVALIDDATA);
            byte[] result = new byte[height * linesize];
            for (int i = 0; i < height; i++)
                Marshal.Copy(srcData + i * linesize, result, i * linesize, bytewidth);
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
            int planes = planar != 0 ? pFrame->ch_layout.nb_channels : 1;
            int block_align = ffmpeg.av_get_bytes_per_sample((AVSampleFormat)pFrame->format) * (planar != 0 ? 1 : pFrame->ch_layout.nb_channels);
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

        #endregion Get Managed Copy Of Data

        public IntPtr[] DataSafe
        {
            get
            {
                List<IntPtr> result = new List<IntPtr>();
                IntPtr intPtr;
                for (uint i = 0; (intPtr = (IntPtr)pFrame->extended_data[i]) != IntPtr.Zero; i++)
                    result.Add(intPtr);
                return result.ToArray();
            }
        }
        object ICloneable.Clone()
        {
            return Clone();
        }

        /// <summary>
        /// Deep copy a new frame.
        /// </summary>
        /// <returns></returns>
        public MediaFrame Clone()
        {
            return new MediaFrame(ffmpeg.av_frame_clone(this));
        }

        /// <summary>
        /// <see cref="ffmpeg.av_frame_unref(AVFrame*)"/>
        /// </summary>
        public void Unref()
        {
            ffmpeg.av_frame_unref(pFrame);
        }

        public void CopyProps(MediaFrame dstframe)
        {
            ffmpeg.av_frame_copy_props(dstframe, this);
        }

        public int MakeWritable() => ffmpeg.av_frame_make_writable(pFrame).ThrowIfError();


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

        #endregion IDisposable Support
    }
}
