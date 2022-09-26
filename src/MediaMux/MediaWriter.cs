using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    public unsafe class MediaWriter : MediaFormatContext
    {
        private bool disposedValue;

        protected MediaIOStream _stream;

        public OutFormat Format { get; protected set; }

        /// <summary>
        /// write to stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="oformat"></param>
        /// <param name="options">useless, the future may change</param>
        public MediaWriter(Stream stream, OutFormat oformat, MediaDictionary options = null)
        {
            _stream = new MediaIOStream(stream);
            pFormatContext = ffmpeg.avformat_alloc_context();
            pFormatContext->oformat = oformat;
            Format = oformat;
            if ((pFormatContext->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
                pFormatContext->pb = _stream;
        }

        /// <summary>
        /// write to file.
        /// <para><see cref="ffmpeg.avformat_alloc_output_context2(AVFormatContext**, AVOutputFormat*, string, string)"/></para>
        /// <para><see cref="ffmpeg.avio_open(AVIOContext**, string, int)"/></para>
        /// </summary>
        /// <param name="file"></param>
        /// <param name="oformat"></param>
        /// <param name="options"></param>
        public MediaWriter(string file, OutFormat oformat = null, MediaDictionary options = null)
        {
            fixed (AVFormatContext** ppFormatContext = &pFormatContext)
            {
                ffmpeg.avformat_alloc_output_context2(ppFormatContext, oformat, null, file).ThrowIfError();
            }
            Format = oformat ?? new OutFormat(pFormatContext->oformat);
            if ((pFormatContext->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
                pFormatContext->pb = _stream = MediaIOStream.Open(file, ffmpeg.AVIO_FLAG_WRITE | ffmpeg.AVIO_FLAG_DIRECT, options);
        }

        public MediaWriter(AVFormatContext* formatContext, bool isOwner = true)
        {
            if (formatContext == null) throw new NullReferenceException();
            pFormatContext = formatContext;
            disposedValue = !isOwner;
        }

        public MediaWriter(IntPtr formatContext, bool isOwner = true)
            : this((AVFormatContext*)formatContext, isOwner)
        { }

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
        /// <param name="codecContext">
        /// if <paramref name="codecContext"/> is not null, then call 
        /// <see cref="ffmpeg.avcodec_parameters_from_context(AVCodecParameters*, AVCodecContext*)"/>
        /// </param>
        /// <returns>newly created stream or null on error.</returns>
        public MediaStream AddStream(MediaCodecContext codecContext = null)
        {
            AVStream* pStream = ffmpeg.avformat_new_stream(pFormatContext, null);
            pStream->id = (int)(pFormatContext->nb_streams - 1);
            if (pStream == null) return null;
            if (codecContext != null)
            {
                ffmpeg.avcodec_parameters_from_context(pStream->codecpar, codecContext).ThrowIfError();
                pStream->time_base = codecContext.AVCodecContext.time_base;
            }
            var stream = new MediaStream(pStream);
            streams.Add(stream);
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
            ffmpeg.avcodec_parameters_copy(pStream->codecpar, &codecpar).ThrowIfError();
            pStream->time_base = timebase;
            var stream = new MediaStream(pStream);
            streams.Add(stream);
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
                ffmpeg.av_packet_rescale_ts(packet, codecTimeBase.Value, this[packet.StreamIndex].TimeBase);
            int ret = ffmpeg.av_interleaved_write_frame(pFormatContext, packet);
            packet.Unref();
            return ret;
        }

        /// <summary>
        /// Write the stream trailer to an output media file and free the file private data.
        /// May only be called after a successful call to avformat_write_header.
        /// <para><see cref="MediaCodecContext.EncodeFrame(MediaFrame)"/></para>
        /// <para><see cref="WritePacket(MediaPacket)"/></para>
        /// <para><see cref="ffmpeg.av_write_trailer(AVFormatContext*)"/></para>
        /// </summary>
        /// <param name="mediaCodecs">Flush encode list</param>
        public int WriteTrailer(IEnumerable<MediaCodecContext> mediaCodecs = null)
        {
            if (mediaCodecs != null)
            {
                foreach (var mediaCodec in mediaCodecs
                    .Where(_ => (((AVCodecContext*)_)->codec->capabilities & ffmpeg.AV_CODEC_CAP_DELAY) != 0))
                {
                    foreach (var packet in mediaCodec.EncodeFrame(null))
                    {
                        WritePacket(packet);
                    }
                }
            }
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

                if (pFormatContext != null)
                {
                    if (_stream != null)
                        _stream.Dispose();
                    ffmpeg.avformat_free_context(pFormatContext);
                    pFormatContext = null;
                }
                disposedValue = true;
            }

        }


        #endregion IDisposable
    }
}
