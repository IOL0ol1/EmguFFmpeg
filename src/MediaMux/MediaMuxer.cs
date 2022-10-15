using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FFmpeg.AutoGen;
using FFmpegSharp.Internal;

namespace FFmpegSharp
{
    public unsafe class MediaMuxer : MediaFormatContext
    {
        protected bool hasWriteHeader; // Fixed: use ffmpeg's flag is better.
        protected bool hasWriteTrailer; // Fixed: use ffmpeg's flag is better.

        private Stream _stream;

        public string Url => ((IntPtr)pFormatContext->url).PtrToStringUTF8();

        public OutFormat Format { get; protected set; }

        /// <summary>
        /// write to stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="oformat"></param>
        /// <param name="options">useless, the future may change</param>
        public static MediaMuxer Create(Stream stream, OutFormat oformat, MediaDictionary options = null)
        {
            var pFormatContext = ffmpeg.avformat_alloc_context();
            pFormatContext->oformat = oformat;
            if ((pFormatContext->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
                pFormatContext->pb = MediaIOContext.Link(stream);
            var output = new MediaMuxer(pFormatContext) { _stream = stream };
            return output;
        }

        /// <summary>
        /// write to file.
        /// <para><see cref="ffmpeg.avformat_alloc_output_context2(AVFormatContext**, AVOutputFormat*, string, string)"/></para>
        /// <para><see cref="ffmpeg.avio_open(AVIOContext**, string, int)"/></para>
        /// </summary>
        /// <param name="file"></param>
        /// <param name="oformat"></param>
        /// <param name="options"></param>
        public static MediaMuxer Create(string file, OutFormat oformat = null, MediaDictionary options = null)
        {
            AVFormatContext* pFormatContext = null;
            ffmpeg.avformat_alloc_output_context2(&pFormatContext, oformat, null, file).ThrowIfError();

            if ((pFormatContext->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
                pFormatContext->pb = MediaIOContext.Create(file, ffmpeg.AVIO_FLAG_WRITE | ffmpeg.AVIO_FLAG_DIRECT, options);
            return new MediaMuxer(pFormatContext);
        }

        public MediaMuxer(AVFormatContext* formatContext, bool isOwner = true) : base(formatContext, isOwner)
        {
            if (formatContext == null) throw new NullReferenceException();
            disposedValue = !isOwner;
            Format = new OutFormat(pFormatContext->oformat);
        }

        /// <summary>
        /// Print detailed information about the output format, such as duration,
        ///     bitrate, streams, container, programs, metadata, side data, codec and time base.
        /// </summary>
        public void DumpFormat()
        {
            for (int i = 0; i < pFormatContext->nb_streams; i++)
            {
                ffmpeg.av_dump_format(pFormatContext, i, ((IntPtr)pFormatContext->url).PtrToStringUTF8(), 1);
            }
        }

        /// <summary>
        /// Get timing information for the data currently output.
        /// <para>The exact meaning of "currently output" depends on the format. It is mostly relevant for devices that have an internal buffer and/or work in real time.</para>
        /// <para>Note: some formats or devices may not allow to measure dts and wall atomically.</para>
        /// </summary>
        /// <param name="stream">stream in the media file</param>
        /// <param name="dts">DTS of the last packet output for the stream, in stream time_base units</param>
        /// <param name="wall">absolute time when that packet whas output, in microsecond</param>
        /// <returns></returns>
        public bool TryGetOutputTimeStamp(int stream, out long dts, out long wall)
        {
            fixed (long* pdts = &dts)
            fixed (long* pwall = &wall)
            {
                return ffmpeg.av_get_output_timestamp(this, stream, pdts, pwall) == 0;
            }
        }

        /// <summary>
        /// Add a new stream to a media file.
        /// <see cref="ffmpeg.avformat_new_stream(AVFormatContext*, AVCodec*)"/>.
        /// </summary>
        /// <param name="encoder">
        /// if <paramref name="encoder"/> is not null, then call 
        /// <see cref="ffmpeg.avcodec_parameters_from_context(AVCodecParameters*, AVCodecContext*)"/>
        /// </param>
        /// <returns>newly created stream or null on error.</returns>
        public MediaStream AddStream(MediaEncoder encoder = null)
        {
            AVStream* pStream = ffmpeg.avformat_new_stream(pFormatContext, null);
            if (pStream == null) return null;
            var stream = new MediaStream(pStream);
            if (encoder != null)
            {
                ffmpeg.avcodec_parameters_from_context(pStream->codecpar, encoder.Context).ThrowIfError();
                pStream->time_base = encoder.Context.TimeBase;
            }
            return stream;
        }

        /// <summary>
        /// Add a new stream to a media file.
        /// <see cref="ffmpeg.avformat_new_stream(AVFormatContext*, AVCodec*)"/>.
        /// </summary>
        /// <param name="codecpar">
        /// <see cref="ffmpeg.avcodec_parameters_copy(AVCodecParameters*, AVCodecParameters*)"/>
        /// </param>
        /// <param name="timebase">stream's timebase</param>
        /// <returns>newly created stream or null on error.</returns>
        public MediaStream AddStream(AVCodecParameters codecpar, AVRational timebase)
        {
            AVStream* pStream = ffmpeg.avformat_new_stream(pFormatContext, null);
            if (pStream == null) return null;
            var stream = new MediaStream(pStream);
            ffmpeg.avcodec_parameters_copy(pStream->codecpar, &codecpar).ThrowIfError();
            pStream->time_base = timebase;
            return stream;
        }

        public MediaStream AddStream(IntPtr pAVCodecParameters, AVRational timebase)
        {
            AVStream* pStream = ffmpeg.avformat_new_stream(pFormatContext, null);
            if (pStream == null) return null;
            var stream = new MediaStream(pStream);
            ffmpeg.avcodec_parameters_copy(pStream->codecpar, (AVCodecParameters*)pAVCodecParameters).ThrowIfError();
            pStream->time_base = timebase;
            return stream;
        }

        /// <summary>
        /// <see cref="ffmpeg.avformat_write_header(AVFormatContext*, AVDictionary**)"/>
        /// </summary>
        /// <param name="options">
        /// An AVDictionary filled with AVFormatContext and muxer-private options. On return
        /// this parameter will be destroyed and replaced with a dict containing options
        /// that were not found. May be NULL.</param>
        /// <returns>
        /// AVSTREAM_INIT_IN_WRITE_HEADER on success if the codec had not already been fully
        /// initialized in avformat_init, AVSTREAM_INIT_IN_INIT_OUTPUT on success if the
        /// codec had already been fully initialized in avformat_init, negative AVERROR on
        /// failure.
        /// </returns>
        /// <exception cref="FFmpegException"></exception>
        public int WriteHeader(MediaDictionary options = null)
        {
            hasWriteHeader = true;
            return ffmpeg.avformat_write_header(pFormatContext, options).ThrowIfError();
        }

        /// <summary>
        /// <para><see cref="ffmpeg.av_interleaved_write_frame(AVFormatContext*, AVPacket*)"/></para>
        /// <para><see cref="ffmpeg.av_packet_unref"/></para>
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="codecTimeBase"><see cref="AVCodecContext.time_base"/></param>
        /// <returns></returns>
        public int WritePacket(MediaPacket packet, AVRational? codecTimeBase = null)
        {
            if (codecTimeBase != null)
                ffmpeg.av_packet_rescale_ts(packet, codecTimeBase.Value, pFormatContext->streams[packet.StreamIndex]->time_base);
            int ret = ffmpeg.av_interleaved_write_frame(pFormatContext, packet);
            packet.Unref();
            return ret;
        }

        /// <summary>
        /// Flush codecs cache.
        /// <para><see cref="MediaEncoder.EncodeFrame(MediaFrame, MediaPacket)"/></para>
        /// <para><see cref="WritePacket(MediaPacket, AVRational?)"/></para> 
        /// </summary>
        /// <param name="mediaCodecs">Flush encode list</param>
        public void FlushCodecs(IEnumerable<MediaEncoder> mediaCodecs)
        {
            if (mediaCodecs != null)
            {
                foreach (var mediaCodec in mediaCodecs
                    .Where(_ => (((AVCodecContext*)_.Context)->codec->capabilities & ffmpeg.AV_CODEC_CAP_DELAY) != 0))
                {
                    foreach (var packet in mediaCodec.EncodeFrame(null))
                    {
                        WritePacket(packet, mediaCodec.Context.TimeBase);
                    }
                }
            }
        }

        /// <summary>
        /// Write the stream trailer to an output media file and free the file private data.
        /// May only be called after a successful call to avformat_write_header. 
        /// <para><see cref="ffmpeg.av_write_trailer(AVFormatContext*)"/></para>
        /// </summary>
        public int WriteTrailer()
        {
            hasWriteTrailer = true;
            return ffmpeg.av_write_trailer(pFormatContext).ThrowIfError();
        }

        #region IDisposable

        /// <summary>
        /// <para><see cref="ffmpeg.avio_close(AVIOContext*)"/></para>
        /// <para><see cref="ffmpeg.avformat_free_context(AVFormatContext*)"/></para>
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // nothing
                }

                _stream?.Dispose();
                if (pFormatContext != null)
                {
                    // Fixed: External calls to ffmpeg.avformat_write_header
                    // and ffmpeg.av_write_trailer function may cause errors.
                    if (hasWriteHeader && !hasWriteTrailer)
                        ffmpeg.av_write_trailer(pFormatContext);
                    pFormatContext = null;
                }
                disposedValue = true;
            }
            base.Dispose(disposing);
        }

        ~MediaMuxer()
        {
            Dispose(disposing: false);
        }
        #endregion IDisposable
    }
}
