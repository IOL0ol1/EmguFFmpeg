using System;
using System.Collections.Generic;
using System.Linq;

using FFmpeg.AutoGen;


namespace FFmpeg.Sharp
{
    public unsafe partial class MediaFrame : IDisposable, ICloneable
    {
        /// <summary>
        /// Wrap an existing <see cref="AVFrame"/> pointer.
        /// </summary>
        /// <param name="frame">Native frame pointer (must be non-null).</param>
        /// <param name="leaveOpen">
        /// When <see langword="true"/>, this wrapper does NOT call <see cref="ffmpeg.av_frame_free(AVFrame**)"/> on dispose;
        /// the caller retains ownership of <paramref name="frame"/>.
        /// When <see langword="false"/>, the wrapper takes ownership and will free the frame.
        /// </param>
        public MediaFrame(AVFrame* frame, bool leaveOpen)
           : this(frame)
        {
            disposedValue = leaveOpen;
        }


        /// <summary>
        /// Allocate a new empty frame via <see cref="ffmpeg.av_frame_alloc()"/>.
        /// The wrapper owns the native frame and will free it on dispose.
        /// </summary>
        public MediaFrame() : this(ffmpeg.av_frame_alloc(), false)
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
            if (channels <= 0) throw new ArgumentOutOfRangeException(nameof(channels));
            if (nbSamples <= 0) throw new ArgumentOutOfRangeException(nameof(nbSamples));
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
            if (nbSamples <= 0) throw new ArgumentOutOfRangeException(nameof(nbSamples));
            var f = new MediaFrame();
            f.pFrame->format = (int)format;
            // Preserve the exact channel layout (order, mask, nb_channels) rather than deriving a
            // default layout from only nb_channels, which would silently corrupt custom layouts.
            // Copy to a local so we can take its address (value-type parameter cannot be pinned directly).
            var layoutCopy = channelLayout;
            ffmpeg.av_channel_layout_copy(&f.pFrame->ch_layout, &layoutCopy).ThrowIfError();
            f.pFrame->nb_samples = nbSamples;
            f.pFrame->sample_rate = sampleRate;
            f.AllocateBuffer(align);
            return f;
        }

        /// <summary>
        /// Allocate frame buffers. Requires the relevant shape fields to be set first:
        /// video frames need <c>width</c>, <c>height</c>, <c>format</c>; audio frames need
        /// <c>nb_samples</c>, <c>ch_layout</c>, <c>format</c>.
        /// </summary>
        public void AllocateBuffer(int align = 0)
        {
            if (pFrame->width > 0 || pFrame->height > 0)
            {
                if (pFrame->width <= 0 || pFrame->height <= 0 || pFrame->format < 0)
                    throw new InvalidOperationException("Video frame requires positive width, height, and format set before AllocateBuffer.");
            }
            else if (pFrame->nb_samples > 0 || pFrame->ch_layout.nb_channels > 0)
            {
                if (pFrame->nb_samples <= 0 || pFrame->ch_layout.nb_channels <= 0 || pFrame->format < 0)
                    throw new InvalidOperationException("Audio frame requires positive nb_samples, ch_layout, and format set before AllocateBuffer.");
            }
            else
            {
                throw new InvalidOperationException("Frame has no shape set. For video: set width/height/format. For audio: set nb_samples/ch_layout/format.");
            }
            ffmpeg.av_frame_get_buffer(pFrame, align).ThrowIfError();
        }

        /// <summary>Opaque user pointer carried by the frame (untyped). Use with caution.</summary>
        public IntPtr Opaque
        {
            get => (IntPtr)pFrame->opaque;
            set => pFrame->opaque = (void*)value;
        }

        /// <summary>True if this frame contains any AVFrameSideData entries.</summary>
        public bool HasSideData => pFrame->nb_side_data > 0;

        /// <summary>Look up the first side-data entry of the given type, or null when absent.</summary>
        public AVFrameSideData* GetSideData(AVFrameSideDataType type)
            => ffmpeg.av_frame_get_side_data(pFrame, type);

        public bool IsAudioFrame => pFrame->nb_samples > 0 && pFrame->ch_layout.nb_channels > 0;
        public bool IsVideoFrame => pFrame->width > 0 && pFrame->height > 0;

        /// <summary>
        /// True if the frame's pixel format is a hardware-acceleration surface (e.g. AV_PIX_FMT_D3D11/CUDA/VAAPI/...).
        /// The pixel data of such a frame lives on the GPU and is not directly readable from the CPU — use
        /// <see cref="TransferToSoftware"/> or build a software-side filter graph to bring it back to system memory.
        /// </summary>
        public bool IsHardwareFrame
        {
            get
            {
                if (pFrame->format < 0) return false;
                var desc = ffmpeg.av_pix_fmt_desc_get((AVPixelFormat)pFrame->format);
                return desc != null && (desc->flags & ffmpeg.AV_PIX_FMT_FLAG_HWACCEL) != 0;
            }
        }

        /// <summary>Borrowed pointer to the frame's hw_frames_ctx, or null.</summary>
        public AVBufferRef* HwFramesCtxRef => pFrame->hw_frames_ctx;

        /// <summary>
        /// Download a hardware frame into system memory. Returns a new owned <see cref="MediaFrame"/>.
        /// </summary>
        /// <param name="targetFormat">
        /// Desired CPU pixel format. Pass <see cref="AVPixelFormat.AV_PIX_FMT_NONE"/> to let FFmpeg pick the first supported one
        /// (typically NV12 for D3D11/QSV/CUDA, NV12 or YUV420P for VAAPI).
        /// </param>
        /// <param name="flags">Flags forwarded to <see cref="ffmpeg.av_hwframe_transfer_data"/>.</param>
        public MediaFrame TransferToSoftware(AVPixelFormat targetFormat = AVPixelFormat.AV_PIX_FMT_NONE, int flags = 0)
        {
            if (!IsHardwareFrame)
                throw new InvalidOperationException("Frame is not a hardware frame; nothing to transfer.");
            var dst = new MediaFrame();
            if (targetFormat != AVPixelFormat.AV_PIX_FMT_NONE)
                dst.Ref.format = (int)targetFormat;
            try
            {
                ffmpeg.av_hwframe_transfer_data(dst, this, flags).ThrowIfError();
                ffmpeg.av_frame_copy_props(dst, this).ThrowIfError();
                return dst;
            }
            catch
            {
                dst.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Allocate this frame's storage on a hardware frames context (the canonical way to feed HW encoders).
        /// </summary>
        public void AllocateOnHWFrames(AVBufferRef* hwFramesCtx)
        {
            if (hwFramesCtx == null) throw new ArgumentNullException(nameof(hwFramesCtx));
            ffmpeg.av_hwframe_get_buffer(hwFramesCtx, pFrame, 0).ThrowIfError();
        }

        #region Get Managed Copy Of Data

        /// <summary>
        /// Get managed data of <see cref="AVFrame.data"/>
        /// <para>
        /// reference <see cref="ffmpeg.av_frame_copy(AVFrame*, AVFrame*)"/>
        /// </para>
        /// </summary>
        /// <param name="padding"><see langword="false"/> will remove ffmpeg padding bytes</param>
        /// <returns></returns>
        public byte[][] GetData(bool padding = true)
        {
            if (pFrame->width > 0 && pFrame->height > 0)
                return GetVideoData(padding).ToArray();
            else if (pFrame->nb_samples > 0 && pFrame->ch_layout.nb_channels > 0)
                return GetAudioData(padding).ToArray();
            throw new FFmpegException(ffmpeg.AVERROR_INVALIDDATA);
        }

        /// <summary>
        /// Get managed bytes of <see cref="AVFrame.data"/>.
        /// <para>
        /// When <paramref name="padding"/> is <see langword="false"/>, the output is equivalent to
        /// <c>av_image_copy_to_buffer(..., align: 1)</c> — padding bytes are stripped and planes are
        /// tightly packed. Use this overload as a drop-in replacement for
        /// <c>av_image_copy_to_buffer</c> + <c>av_image_get_buffer_size</c> in example code.
        /// </para>
        /// </summary>
        /// <param name="padding"><see langword="false"/> removes ffmpeg padding bytes (equivalent to <c>av_image_copy_to_buffer</c> with align=1).</param>
        /// <returns></returns>
        /// <exception cref="FFmpegException"></exception>
        public byte[] GetBytes(bool padding = true)
        {
            var size = GetBytesSize(padding);
            var result = new byte[size];
            GetBytes(result, padding);
            return result;
        }

        /// <summary>
        /// Compute the size in bytes that the Span <c>GetBytes</c> overload would write,
        /// without allocating anything.
        /// </summary>
        /// <param name="padding"><see langword="false"/> assumes ffmpeg padding bytes are stripped</param>
        /// <returns>byte count</returns>
        /// <exception cref="FFmpegException"></exception>
        public int GetBytesSize(bool padding = true)
        {
            if (pFrame->width > 0 && pFrame->height > 0)
                return ComputeVideoSize(padding);
            else if (pFrame->nb_samples > 0 && pFrame->ch_layout.nb_channels > 0)
                return ComputeAudioSize();
            throw new FFmpegException(ffmpeg.AVERROR_INVALIDDATA);
        }

        /// <summary>
        /// Zero-allocation copy of <see cref="AVFrame.data"/> into the supplied buffer.
        /// Use <see cref="GetBytesSize(bool)"/> to size the buffer.
        /// <para>
        /// Reference <see cref="ffmpeg.av_frame_copy(AVFrame*, AVFrame*)"/>.
        /// </para>
        /// </summary>
        /// <param name="dst">destination buffer; must be at least <see cref="GetBytesSize(bool)"/> bytes</param>
        /// <param name="padding"><see langword="false"/> will strip ffmpeg padding bytes</param>
        /// <returns>bytes written</returns>
        /// <exception cref="FFmpegException"></exception>
        /// <exception cref="ArgumentException">dst is too small</exception>
        public int GetBytes(Span<byte> dst, bool padding = true)
        {
            if (pFrame->width > 0 && pFrame->height > 0)
                return CopyVideoBytes(dst, padding);
            else if (pFrame->nb_samples > 0 && pFrame->ch_layout.nb_channels > 0)
                return CopyAudioBytes(dst);
            throw new FFmpegException(ffmpeg.AVERROR_INVALIDDATA);
        }

        private int ComputeVideoSize(bool padding)
        {
            AVPixFmtDescriptor* desc = ffmpeg.av_pix_fmt_desc_get((AVPixelFormat)pFrame->format);
            if (desc == null || (desc->flags & ffmpeg.AV_PIX_FMT_FLAG_HWACCEL) != 0)
                throw new FFmpegException(ffmpeg.AVERROR_INVALIDDATA);

            int total = 0;
            if ((desc->flags & ffmpeg.AV_PIX_FMT_FLAG_PAL) != 0)
            {
                var srcLine = pFrame->linesize[0] * pFrame->height;
                var byteWidth = pFrame->width * pFrame->height;
                total += padding ? srcLine : byteWidth;
                if (pFrame->data[1] != null) // AV_PIX_FMT_PAL8 palette
                    total += 4 * 256;
            }
            else
            {
                int planes_nb = 0;
                for (int i = 0; i < desc->nb_components; i++)
                    planes_nb = Math.Max(planes_nb, desc->comp[(uint)i].plane + 1);
                for (int i = 0; i < planes_nb; i++)
                {
                    int h = pFrame->height;
                    int bwidth = ffmpeg.av_image_get_linesize((AVPixelFormat)pFrame->format, pFrame->width, i);
                    if (i == 1 || i == 2)
                        h = (int)Math.Ceiling((double)pFrame->height / (1 << desc->log2_chroma_h));
                    int srcLine = pFrame->linesize[(uint)i];
                    total += h * (padding ? srcLine : bwidth);
                }
            }
            return total;
        }

        private int CopyVideoBytes(Span<byte> dst, bool padding)
        {
            AVPixFmtDescriptor* desc = ffmpeg.av_pix_fmt_desc_get((AVPixelFormat)pFrame->format);
            if (desc == null || (desc->flags & ffmpeg.AV_PIX_FMT_FLAG_HWACCEL) != 0)
                throw new FFmpegException(ffmpeg.AVERROR_INVALIDDATA);

            int offset = 0;
            fixed (byte* dstPtr = dst)
            {
                if ((desc->flags & ffmpeg.AV_PIX_FMT_FLAG_PAL) != 0)
                {
                    offset += CopyPlaneInto(dstPtr + offset, dst.Length - offset, (IntPtr)pFrame->data[0],
                                            pFrame->linesize[0] * pFrame->height, pFrame->width * pFrame->height, 1, padding);
                    if (pFrame->data[1] != null)
                        offset += CopyPlaneInto(dstPtr + offset, dst.Length - offset, (IntPtr)pFrame->data[1],
                                                4 * 256, 4 * 256, 1, padding);
                }
                else
                {
                    int planes_nb = 0;
                    for (int i = 0; i < desc->nb_components; i++)
                        planes_nb = Math.Max(planes_nb, desc->comp[(uint)i].plane + 1);
                    for (int i = 0; i < planes_nb; i++)
                    {
                        int h = pFrame->height;
                        int bwidth = ffmpeg.av_image_get_linesize((AVPixelFormat)pFrame->format, pFrame->width, i);
                        if (i == 1 || i == 2)
                            h = (int)Math.Ceiling((double)pFrame->height / (1 << desc->log2_chroma_h));
                        offset += CopyPlaneInto(dstPtr + offset, dst.Length - offset, (IntPtr)pFrame->data[(uint)i],
                                                pFrame->linesize[(uint)i], bwidth, h, padding);
                    }
                }
            }
            return offset;
        }

        private static int CopyPlaneInto(byte* dst, int dstCapacity, IntPtr src, int srcLineSize, int byteWidth, int height, bool padding)
        {
            int dstLine = padding ? srcLineSize : byteWidth;
            int planeSize = height * dstLine;
            if (planeSize > dstCapacity)
                throw new ArgumentException("destination buffer too small");
            FFmpegUtil.CopyPlane(src, srcLineSize, (IntPtr)dst, dstLine, byteWidth, height);
            return planeSize;
        }

        private int ComputeAudioSize()
        {
            bool planar = ((AVSampleFormat)pFrame->format).IsPlanar();
            int planes = planar ? pFrame->ch_layout.nb_channels : 1;
            int block_align = ((AVSampleFormat)pFrame->format).GetBytesPerSample() * (planar ? 1 : pFrame->ch_layout.nb_channels);
            int data_size = pFrame->nb_samples * block_align;
            int total = 0;
            for (uint i = 0; pFrame->extended_data[i] != null && i < planes; i++)
                total += data_size;
            return total;
        }

        private int CopyAudioBytes(Span<byte> dst)
        {
            bool planar = ((AVSampleFormat)pFrame->format).IsPlanar();
            int planes = planar ? pFrame->ch_layout.nb_channels : 1;
            int block_align = ((AVSampleFormat)pFrame->format).GetBytesPerSample() * (planar ? 1 : pFrame->ch_layout.nb_channels);
            int data_size = pFrame->nb_samples * block_align;
            int offset = 0;
            fixed (byte* dstPtr = dst)
            {
                for (uint i = 0; pFrame->extended_data[i] != null && i < planes; i++)
                {
                    if (offset + data_size > dst.Length)
                        throw new ArgumentException("destination buffer too small");
                    FFmpegUtil.CopyPlane((IntPtr)pFrame->extended_data[i], data_size, (IntPtr)(dstPtr + offset), data_size, data_size, 1);
                    offset += data_size;
                }
            }
            return offset;
        }

        private List<byte[]> GetVideoData(bool padding)
        {
            List<byte[]> result = new List<byte[]>();
            AVPixFmtDescriptor* desc = ffmpeg.av_pix_fmt_desc_get((AVPixelFormat)pFrame->format);
            if (desc == null || (desc->flags & ffmpeg.AV_PIX_FMT_FLAG_HWACCEL) != 0)
                throw new FFmpegException(ffmpeg.AVERROR_INVALIDDATA);

            if ((desc->flags & ffmpeg.AV_PIX_FMT_FLAG_PAL) != 0) // packet
            {
                result.Add(GetPlane((IntPtr)pFrame->data[0], pFrame->linesize[0] * pFrame->height, pFrame->width * pFrame->height, 1, padding));
                if (pFrame->data[1] != null) // AV_PIX_FMT_PAL8, 8 bits with AV_PIX_FMT_RGB32 palette
                {
                    result.Add(GetPlane((IntPtr)pFrame->data[1], 4 * 256, 4 * 256, 1, padding));
                }
            }
            else //planer
            {
                int i, planes_nb = 0;
                for (i = 0; i < desc->nb_components; i++)
                    planes_nb = Math.Max(planes_nb, desc->comp[(uint)i].plane + 1);
                for (i = 0; i < planes_nb; i++)
                {
                    int h = pFrame->height;
                    int bwidth = ffmpeg.av_image_get_linesize((AVPixelFormat)pFrame->format, pFrame->width, i);
                    if (i == 1 || i == 2)
                        h = (int)Math.Ceiling((double)pFrame->height / (1 << desc->log2_chroma_h));
                    result.Add(GetPlane((IntPtr)pFrame->data[(uint)i], pFrame->linesize[(uint)i], bwidth, h, padding));
                }
            }
            return result;
        }

        /// <summary>
        /// reference <see cref="ffmpeg.av_samples_copy(byte**, byte**, int, int, int, int, AVSampleFormat)"/>
        /// </summary>
        /// <returns></returns>
        private List<byte[]> GetAudioData(bool padding)
        {
            List<byte[]> result = new List<byte[]>();
            bool planar = ((AVSampleFormat)pFrame->format).IsPlanar();
            int planes = planar ? pFrame->ch_layout.nb_channels : 1;
            int block_align = ((AVSampleFormat)pFrame->format).GetBytesPerSample() * (planar ? 1 : pFrame->ch_layout.nb_channels);
            int data_size = pFrame->nb_samples * block_align;
            IntPtr intPtr;
            for (uint i = 0; (intPtr = (IntPtr)pFrame->extended_data[i]) != IntPtr.Zero && i < planes; i++)
            {
                result.Add(GetPlane(intPtr, data_size, data_size, 1, padding));
            }
            return result;
        }

        private byte[] GetPlane(IntPtr srcData, int srcByteLinesize, int byteWidth, int height, bool padding = true)
        {
            var dstByteLineSize = padding ? srcByteLinesize : byteWidth;
            var result = new byte[height * dstByteLineSize];
            fixed (void* ptr = result)
            {
                FFmpegUtil.CopyPlane(srcData, srcByteLinesize, (IntPtr)ptr, dstByteLineSize, byteWidth, height);
            }
            return result;
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
        /// Deep copy a new frame. The returned frame is owned by the caller and must be disposed.
        /// </summary>
        public MediaFrame Clone()
        {
            return new MediaFrame(ffmpeg.av_frame_clone(this), leaveOpen: false);
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

        public bool IsWriteable() => ffmpeg.av_frame_is_writable(pFrame).ThrowIfError() != 0;

        public int MakeWritable() => ffmpeg.av_frame_make_writable(pFrame).ThrowIfError();


        #region IDisposable Support

        // Default is `false` (owned). The (AVFrame*, bool leaveOpen) ctor flips it to `true` for borrowed pointers.
        // Historical bug (fixed): this used to be `true`, which caused single-pointer ctors (Clone path) to leak.
        private bool disposedValue;

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
