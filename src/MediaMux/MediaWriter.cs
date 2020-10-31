﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    public unsafe class MediaWriter : MediaMux
    {
        public new OutFormat Format => base.Format as OutFormat;

        /// <summary>
        /// write to stream,default buffersize 4096
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="oformat"></param>
        /// <param name="options"></param>
        public MediaWriter(Stream stream, OutFormat oformat, MediaDictionary options = null)
            : this(stream, 4096, oformat, options) { }

        /// <summary>
        /// write to stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffersize"></param>
        /// <param name="oformat"></param>
        /// <param name="options">useless, the future may change</param>
        public MediaWriter(Stream stream, int buffersize, OutFormat oformat, MediaDictionary options = null)
        {
            baseStream = stream;
            bufferLength = buffersize;
            buffer = new byte[bufferLength];
            avio_Alloc_Context_Read_Packet = ReadFunc;
            avio_Alloc_Context_Write_Packet = WriteFunc;
            avio_Alloc_Context_Seek = SeekFunc;
            pFormatContext = ffmpeg.avformat_alloc_context();
            pFormatContext->oformat = oformat;
            base.Format = oformat;
            if ((pFormatContext->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
                pFormatContext->pb = ffmpeg.avio_alloc_context((byte*)ffmpeg.av_malloc((ulong)bufferLength), bufferLength, 1, null,
                    avio_Alloc_Context_Read_Packet, avio_Alloc_Context_Write_Packet, avio_Alloc_Context_Seek);
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
            base.Format = oformat ?? new OutFormat(pFormatContext->oformat);
            if ((pFormatContext->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
                ffmpeg.avio_open2(&pFormatContext->pb, file, ffmpeg.AVIO_FLAG_WRITE, null, options).ThrowIfError();
        }

        /// <summary>
        /// Print detailed information about the output format, such as duration,
        ///     bitrate, streams, container, programs, metadata, side data, codec and time base.
        /// </summary>
        public override void DumpFormat()
        {
            for (int i = 0; i < pFormatContext->nb_streams; i++)
            {
                ffmpeg.av_dump_format(pFormatContext, i, ((IntPtr)pFormatContext->url).PtrToStringUTF8(), 1);
            }
        }


        public MediaStream AddStream(MediaCodecContext codecContext, int streamid = -1, MediaCodec codec = null)
        {
            AVStream* stream = ffmpeg.avformat_new_stream(pFormatContext, codec);
            stream->id = streamid < 0 ? (int)(pFormatContext->nb_streams - 1) : streamid;
            ffmpeg.avcodec_parameters_from_context(stream->codecpar, codecContext).ThrowIfError();
            stream->time_base = codecContext.AVCodecContext.time_base;
            streams.Add(new MediaStream(stream));
            return streams.Last();
        }

        /// <summary>
        /// Add stream by copy <see cref="ffmpeg.avcodec_parameters_copy(AVCodecParameters*, AVCodecParameters*)"/>,
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public MediaStream AddStream(MediaStream stream, int flags = 0)
        {
            AVStream* pstream = ffmpeg.avformat_new_stream(pFormatContext, null);
            pstream->id = (int)(pFormatContext->nb_streams - 1);
            ffmpeg.avcodec_parameters_copy(pstream->codecpar, stream.Stream.codecpar);
            pstream->codecpar->codec_tag = 0;
            MediaCodec mediaCodec = null;
            if (stream.Codec != null)
            {
                mediaCodec = MediaEncoder.CreateEncode(stream.Codec.AVCodecContext.codec_id, flags, _ =>
                {
                    AVCodecContext* pContext = _;
                    AVCodecParameters* pParameters = ffmpeg.avcodec_parameters_alloc();
                    ffmpeg.avcodec_parameters_from_context(pParameters, stream.Codec).ThrowIfError();
                    ffmpeg.avcodec_parameters_to_context(pContext, pParameters);
                    ffmpeg.avcodec_parameters_free(&pParameters);
                    pContext->time_base = stream.Stream.r_frame_rate.ToInvert();
                });
            }
            streams.Add(new MediaStream(pstream) { TimeBase = stream.Stream.r_frame_rate.ToInvert(), Codec = mediaCodec });
            return streams.Last();
        }

        /// <summary>
        /// <see cref="ffmpeg.avformat_write_header(AVFormatContext*, AVDictionary**)"/>
        /// </summary>
        /// <param name="options"></param>
        public int Initialize(MediaDictionary options = null)
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
        /// send null frame to flush encoder cache and write trailer
        /// <para><see cref="MediaStream.WriteFrame(MediaFrame)"/></para>
        /// <para><see cref="WritePacket(MediaPacket)"/></para>
        /// <para><see cref="ffmpeg.av_write_trailer(AVFormatContext*)"/></para>
        /// </summary>
        public int FlushMuxer()
        {
            foreach (var stream in streams)
            {
                try
                {
                    foreach (var packet in stream.WriteFrame(null))
                    {
                        try { WritePacket(packet); }
                        catch (FFmpegException) { break; }
                    }
                }
                catch (FFmpegException) { }
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
            if (pFormatContext != null)
            {
                try
                {
                    // Close the output file.
                    if ((pFormatContext->flags & ffmpeg.AVFMT_NOFILE) == 0)
                    {
                        if (baseStream == null)
                        {
                            ffmpeg.avio_closep(&pFormatContext->pb);
                        }
                        else
                        {
                            baseStream.Dispose();
                            ffmpeg.av_freep(&pFormatContext->pb->buffer);
                            ffmpeg.avio_context_free(&pFormatContext->pb);
                        }
                    }
                }
                finally
                {
                    ffmpeg.avformat_free_context(pFormatContext);
                    avio_Alloc_Context_Read_Packet = null;
                    avio_Alloc_Context_Write_Packet = null;
                    avio_Alloc_Context_Seek = null;
                    pFormatContext = null;
                }
            }
        }

        #endregion IDisposable
    }
}
