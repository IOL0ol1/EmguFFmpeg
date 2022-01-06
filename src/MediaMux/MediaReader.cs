using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    public unsafe partial class MediaReader : MediaMux
    {
        /// <summary>
        /// Get <see cref="AVInputFormat"/>
        /// </summary>
        public InFormat Format { get; protected set; }

        /// <summary>
        /// Load stream, default buffer size is 4096
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="iformat"></param>
        /// <param name="options"></param>
        public MediaReader(Stream stream, InFormat iformat = null, MediaDictionary options = null)
            : this(stream, 4096, iformat, options) { }

        /// <summary>
        /// Load stream with buffersize
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffersize"></param>
        /// <param name="iformat"></param>
        /// <param name="options"></param>
        public MediaReader(Stream stream, int buffersize, InFormat iformat = null, MediaDictionary options = null)
        {
            baseStream = stream;
            bufferLength = buffersize;
            buffer = new byte[bufferLength];
            avio_Alloc_Context_Read_Packet = ReadFunc;
            avio_Alloc_Context_Seek = SeekFunc;
            pFormatContext = ffmpeg.avformat_alloc_context();
            pFormatContext->pb = ffmpeg.avio_alloc_context((byte*)ffmpeg.av_malloc((ulong)bufferLength), bufferLength, 0, null,
                avio_Alloc_Context_Read_Packet, null, avio_Alloc_Context_Seek);
            fixed (AVFormatContext** ppFormatContext = &pFormatContext)
            {
                ffmpeg.avformat_open_input(ppFormatContext, null, iformat, options).ThrowIfError();
            }
            ffmpeg.avformat_find_stream_info(pFormatContext, null).ThrowIfError();
            Format = iformat ?? new InFormat(pFormatContext->iformat);

            for (int i = 0; i < pFormatContext->nb_streams; i++)
            {
                AVStream* pStream = pFormatContext->streams[i];
                MediaDecoder codec = MediaDecoder.CreateDecoder(pStream->codecpar->codec_id, _ =>
                {
                    ffmpeg.avcodec_parameters_to_context(_, pStream->codecpar);
                });
                streams.Add(new MediaStream(pStream) { Codec = codec });
            }
        }

        /// <summary>
        /// Load path
        /// </summary>
        /// <param name="url"></param>
        /// <param name="iformat"></param>
        /// <param name="options"></param>
        public MediaReader(string url, InFormat iformat = null, MediaDictionary options = null)
        {
            fixed (AVFormatContext** ppFormatContext = &pFormatContext)
            {
                ffmpeg.avformat_open_input(ppFormatContext, url, iformat, options).ThrowIfError();
            }
            ffmpeg.avformat_find_stream_info(pFormatContext, null).ThrowIfError();
            base.Format = iformat ?? new InFormat(pFormatContext->iformat);

            for (int i = 0; i < pFormatContext->nb_streams; i++)
            {
                AVStream* pStream = pFormatContext->streams[i];
                MediaDecoder codec = MediaDecoder.CreateDecoder(pStream->codecpar->codec_id, _ =>
                {
                    ffmpeg.avcodec_parameters_to_context(_, pStream->codecpar);
                });
                streams.Add(new MediaStream(pStream) { Codec = codec });
            }
        }

        /// <summary>
        /// Print detailed information about the input format, such as duration,
        ///     bitrate, streams, container, programs, metadata, side data, codec and time base.
        /// </summary>
        public void DumpFormat()
        {
            for (int i = 0; i < pFormatContext->nb_streams; i++)
            {
                ffmpeg.av_dump_format(pFormatContext, i, ((IntPtr)pFormatContext->url).PtrToStringUTF8(), 0);
            }
        }

        /// <summary>
        /// Seek timestamp base <see cref="ffmpeg.AV_TIME_BASE"/>. it's precision than <see cref="Seek(TimeSpan, int)"/>
        /// <para></para>
        /// </summary>
        /// <param name="timestamp">Seconds * <see cref="ffmpeg.AV_TIME_BASE"/> </param>
        /// <param name="streamIndex"></param>
        public int Seek(long timestamp, int streamIndex = -1)
        {
            if (streamIndex >= 0)
                timestamp = ffmpeg.av_rescale_q(timestamp, ffmpeg.av_get_time_base_q(), streams[streamIndex].TimeBase);
            var ret = ffmpeg.avformat_seek_file(pFormatContext, streamIndex, long.MinValue, timestamp, long.MaxValue, 0).ThrowIfError();
            if (streamIndex >= 0)
            {
                ffmpeg.avcodec_flush_buffers(streams[streamIndex].Codec);
            }
            else
            {
                foreach (var stream in streams)
                {
                    ffmpeg.avcodec_flush_buffers(stream.Codec);
                }
            }
            return ret;
        }

        /// <summary>
        /// <see cref="Seek(long, int)"/>
        /// </summary>
        /// <param name="time"></param>
        /// <param name="streamIndex"></param>
        public int Seek(TimeSpan time, int streamIndex = -1)
        {
            return Seek((long)(time.TotalSeconds * ffmpeg.AV_TIME_BASE), streamIndex);
        }

        #region IEnumerable<MediaPacket>

        public IEnumerable<MediaPacket> ReadPacket()
        {
            using (MediaPacket packet = new MediaPacket())
            {
                int ret;
                do
                {
                    ret = ReadPacket(packet);
                    if (ret < 0 && ret != ffmpeg.AVERROR_EOF)
                        throw new FFmpegException(ret);
                    yield return packet;
                    packet.Unref();
                } while (ret >= 0);
            }
        }

        public int ReadPacket([Out] MediaPacket packet)
        {
            return ffmpeg.av_read_frame(pFormatContext, packet);
        }

        #endregion IEnumerable<MediaPacket>

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (pFormatContext != null)
            {
                fixed (AVFormatContext** ppFormatContext = &pFormatContext)
                {
                    try
                    {
                        if (baseStream != null)
                        {
                            baseStream.Dispose();
                            ffmpeg.av_freep(&pFormatContext->pb->buffer);
                            ffmpeg.avio_context_free(&pFormatContext->pb);
                        }
                    }
                    finally
                    {
                        ffmpeg.avformat_close_input(ppFormatContext);
                        avio_Alloc_Context_Read_Packet = null;
                        avio_Alloc_Context_Write_Packet = null;
                        avio_Alloc_Context_Seek = null;
                        pFormatContext = null;
                    }
                }
            }
        }

        #endregion IDisposable
    }
}
