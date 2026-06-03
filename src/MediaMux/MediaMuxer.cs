using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    public unsafe class MediaMuxer : MediaFormatContext
    {
        protected bool hasWriteHeader;
        protected bool hasWriteTrailer;
        // Track encoders attached via AddStream(encoder) so Dispose can auto-flush them before writing trailer.
        private readonly List<MediaEncoder> _attachedEncoders = new List<MediaEncoder>();
        // Track any low-level failure so we don't try to write a trailer over a known-bad state.
        private bool _wroteCorruptedHeader;

        protected MediaIOContext _ioContext;

        public string Url => ((IntPtr)pFormatContext->url).PtrToStringUTF8();

        public MediaOutputFormat Format => new MediaOutputFormat(pFormatContext->oformat);

        /// <summary>
        /// Write to a managed <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">Output stream.</param>
        /// <param name="oformat">Output format descriptor.</param>
        /// <param name="leaveOpen">When <see langword="true"/> (default), the underlying stream is NOT disposed when this muxer is disposed.</param>
        public static MediaMuxer Create(Stream stream, MediaOutputFormat oformat, bool leaveOpen = true)
        {
            if (oformat == null) throw new ArgumentNullException(nameof(oformat));
            var ioContext = (stream as MediaIOContext) ?? new MediaIOContext(stream, 32768, leaveOpen);
            AVFormatContext* pFormatContext = ffmpeg.avformat_alloc_context();
            pFormatContext->oformat = oformat;
            if ((pFormatContext->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
                pFormatContext->pb = ioContext;
            return new MediaMuxer(pFormatContext) { _ioContext = ioContext };
        }

        /// <summary>
        /// Write to a managed <see cref="Stream"/> with format auto-detected from <paramref name="formatName"/> or <paramref name="fileNameHint"/>.
        /// </summary>
        public static MediaMuxer Create(Stream stream, string formatName, string fileNameHint = null, bool leaveOpen = true)
        {
            var oformat = MediaOutputFormat.GuessFormat(formatName, fileNameHint, null);
            if (oformat == null)
                throw new FFmpegException("Cannot guess output format from formatName='" + formatName + "', fileNameHint='" + fileNameHint + "'");
            return Create(stream, oformat, leaveOpen);
        }

        /// <summary>
        /// Write to a file.
        /// </summary>
        public static MediaMuxer Create(string fileName, MediaOutputFormat oformat = null, string formatName = null, MediaDictionary options = null)
        {
            AVFormatContext* pFormatContext = null;
            ffmpeg.avformat_alloc_output_context2(&pFormatContext, oformat, formatName, fileName).ThrowIfError();
            var o = new MediaMuxer(pFormatContext);
            if ((pFormatContext->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0 && fileName != null)
            {
                o._ioContext = MediaIOContext.Open(fileName, ffmpeg.AVIO_FLAG_WRITE | ffmpeg.AVIO_FLAG_DIRECT, options);
                pFormatContext->pb = o._ioContext;
            }
            return o;
        }

        public MediaMuxer(AVFormatContext* pAVCodecContext, bool isDisposeByOwner = true)
            : base(pAVCodecContext, isDisposeByOwner)
        { }

        public MediaMuxer()
            : base()
        { }

        /// <summary>
        /// Print detailed information about the output format.
        /// </summary>
        public void DumpFormat()
        {
            ffmpeg.av_dump_format(pFormatContext, 0, ((IntPtr)pFormatContext->url).PtrToStringUTF8(), 1);
        }

        public bool TryGetOutputTimeStamp(int stream, out long dts, out long wall)
        {
            fixed (long* pdts = &dts)
            fixed (long* pwall = &wall)
            {
                return ffmpeg.av_get_output_timestamp(this, stream, pdts, pwall) == 0;
            }
        }

        /// <summary>
        /// Add a stream backed by <paramref name="encoder"/>. The encoder is registered for automatic
        /// flushing in <see cref="Dispose(bool)"/>.
        /// </summary>
        public MediaStream AddStream(MediaEncoder encoder = null)
        {
            AVStream* pStream = ffmpeg.avformat_new_stream(pFormatContext, null);
            if (pStream == null) return null;
            var stream = new MediaStream(pStream);
            if (encoder != null)
            {
                ffmpeg.avcodec_parameters_from_context(pStream->codecpar, encoder).ThrowIfError();
                pStream->time_base = encoder.Ref.time_base;
                _attachedEncoders.Add(encoder);
            }
            return stream;
        }

        public MediaStream AddStream(AVCodecParameters codecpar)
        {
            AVStream* pStream = ffmpeg.avformat_new_stream(pFormatContext, null);
            if (pStream == null) return null;
            var stream = new MediaStream(pStream);
            ffmpeg.avcodec_parameters_copy(pStream->codecpar, &codecpar).ThrowIfError();
            return stream;
        }

        public int WriteHeader(MediaDictionary options = null)
        {
            hasWriteHeader = true;
            int ret;
            if (options == null)
                ret = ffmpeg.avformat_write_header(pFormatContext, null);
            else
            {
                fixed (AVDictionary** pOptions = &options.pDictionary)
                    ret = ffmpeg.avformat_write_header(pFormatContext, pOptions);
            }
            if (ret < 0)
            {
                _wroteCorruptedHeader = true;
                ret.ThrowIfError();
            }
            return ret;
        }

        /// <summary>
        /// Rescale the packet timestamps from <paramref name="codecTimeBase"/> to the stream's time base, then write it.
        /// If <paramref name="codecTimeBase"/> is null we assume the packet's timestamps are already in the stream's time base.
        /// </summary>
        public int WritePacket(MediaPacket packet, AVRational? codecTimeBase = null)
        {
            if (codecTimeBase != null)
                ffmpeg.av_packet_rescale_ts(packet, codecTimeBase.Value, pFormatContext->streams[packet.Ref.stream_index]->time_base);
            int ret = ffmpeg.av_interleaved_write_frame(pFormatContext, packet);
            packet.Unref();
            return ret;
        }

        /// <summary>
        /// Rescale using <paramref name="encoder"/>.Ref.time_base — the common case.
        /// </summary>
        public int WritePacket(MediaPacket packet, MediaEncoder encoder)
            => WritePacket(packet, encoder?.Ref.time_base);

        /// <summary>
        /// Flush all encoders that declare AV_CODEC_CAP_DELAY (and others — non-delayed encoders are a no-op).
        /// </summary>
        public void FlushCodecs(IEnumerable<MediaEncoder> mediaCodecs)
        {
            if (mediaCodecs == null) return;
            foreach (var mediaCodec in mediaCodecs)
            {
                if (mediaCodec == null) continue;
                foreach (var packet in mediaCodec.EncodeFrame(null))
                {
                    WritePacket(packet, mediaCodec.Ref.time_base);
                }
            }
        }

        public int WriteTrailer()
        {
            hasWriteTrailer = true;
            return ffmpeg.av_write_trailer(pFormatContext).ThrowIfError();
        }

        #region IDisposable
        private bool disposedValue;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (pFormatContext != null)
                {
                    // Best-effort: flush any encoders registered via AddStream(encoder), then write trailer.
                    // Wrap in try/catch — Dispose MUST NOT throw (esp. from a finalizer).
                    if (hasWriteHeader && !hasWriteTrailer && !_wroteCorruptedHeader && pFormatContext->pb != null)
                    {
                        try
                        {
                            FlushCodecs(_attachedEncoders);
                            ffmpeg.av_write_trailer(pFormatContext);
                        }
                        catch
                        {
                            // Swallow — disposing during exception unwind is the typical path here, surfacing
                            // would mask the original error. Users who care should call WriteTrailer() explicitly.
                        }
                    }
                    if (_ioContext != null && (pFormatContext->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
                    {
                        AVIOContext* pb = _ioContext;
                        _ioContext.Dispose();
                        if (pb == pFormatContext->pb)
                            pFormatContext->pb = null;
                    }
                    base.Dispose(disposing);
                    pFormatContext = null;
                }
                disposedValue = true;
            }
        }

        #endregion IDisposable
    }
}
