using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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


        public MediaStream AddStream(MediaCodecContext codecContext)
        {
            AVStream* pStream = ffmpeg.avformat_new_stream(pFormatContext, null);
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
        /// Add stream by copy <see cref="ffmpeg.avcodec_parameters_copy(AVCodecParameters*, AVCodecParameters*)"/>,
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public MediaStream AddStream(MediaStream stream)
        {
            AVStream* pStream = ffmpeg.avformat_new_stream(pFormatContext, null);
            ffmpeg.avcodec_parameters_copy(pStream->codecpar, stream.Stream.codecpar);
            pStream->codecpar->codec_tag = 0;
            pStream->time_base = stream.Stream.r_frame_rate.ToInvert(); //

            var s = new MediaStream(pStream);
            streams.Add(s);
            return s;
        }

        //public MediaStream AddStream(AVCodecParameters parameters, AVRational timeBase)
        //{
        //    AVStream* pStream = ffmpeg.avformat_new_stream(pFormatContext, null);
        //    ffmpeg.avcodec_parameters_copy(pStream->codecpar, &parameters);
        //    pStream->codecpar->codec_tag = 0;
        //    pStream->time_base = timeBase.ToInvert();

        //    var s = new MediaStream(pStream);
        //    streams.Add(s);
        //    return s;
        //}

        /// <summary>
        /// <see cref="ffmpeg.avformat_write_header(AVFormatContext*, AVDictionary**)"/>
        /// </summary>
        /// <param name="options"></param>
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
        /// <returns></returns>
        public int WritePacket([In] MediaPacket packet)
        {
            int ret = ffmpeg.av_interleaved_write_frame(pFormatContext, packet);
            packet.Unref();
            return ret;
        }

        /// <summary>
        /// flush encoder cache and write trailer
        /// <para><see cref="WritePacket(MediaPacket)"/></para>
        /// <para><see cref="ffmpeg.av_write_trailer(AVFormatContext*)"/></para>
        /// </summary>
        /// <exception cref="FFmpegException"></exception>
        public int WriteTrailer()
        {
            foreach (var stream in streams)
            {
                // TODO
                //try
                //{
                //    foreach (var packet in stream.WriteFrame(null))
                //    {
                //        try { WritePacket(packet); }
                //        catch (FFmpegException) { break; }
                //    }
                //}
                //catch (FFmpegException) { }
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
                    // TODO: 释放托管状态(托管对象)
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
