using System;
using System.Collections.Generic;
using System.IO;
using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    public unsafe partial class MediaReader : MediaFormatContext
    {
        protected MediaIOStream _stream;

        /// <summary>
        /// Get <see cref="AVInputFormat"/>
        /// </summary>
        public InFormat Format { get; protected set; }

        /// <summary>
        /// Load stream 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="iformat"></param>
        /// <param name="options"></param>
        public MediaReader(Stream stream, InFormat iformat = null, MediaDictionary options = null)
        {
            _stream = new MediaIOStream(stream);
            pFormatContext = ffmpeg.avformat_alloc_context();
            pFormatContext->pb = _stream;
            fixed (AVFormatContext** ppFormatContext = &pFormatContext)
            {
                ffmpeg.avformat_open_input(ppFormatContext, null, iformat, options).ThrowIfError();
            }
            ffmpeg.avformat_find_stream_info(pFormatContext, options).ThrowIfError();
            Format = iformat ?? new InFormat(pFormatContext->iformat);

            for (int i = 0; i < pFormatContext->nb_streams; i++)
            {
                AVStream* pStream = pFormatContext->streams[i];
                streams.Add(new MediaStream(pStream));
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
            ffmpeg.avformat_find_stream_info(pFormatContext, options).ThrowIfError();
            Format = iformat ?? new InFormat(pFormatContext->iformat);

            for (int i = 0; i < pFormatContext->nb_streams; i++)
            {
                AVStream* pStream = pFormatContext->streams[i];
                streams.Add(new MediaStream(pStream));
            }
        }

        public MediaReader(AVFormatContext* formatContext, MediaDictionary options = null, bool isOwner = true)
        {
            if (formatContext == null) throw new NullReferenceException();
            pFormatContext = formatContext;
            disposedValue = !isOwner;
            ffmpeg.avformat_find_stream_info(pFormatContext, options).ThrowIfError();
            Format = new InFormat(pFormatContext->iformat);

            for (int i = 0; i < pFormatContext->nb_streams; i++)
            {
                AVStream* pStream = pFormatContext->streams[i];
                streams.Add(new MediaStream(pStream));
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
            var ret = ffmpeg.avformat_seek_file(pFormatContext, streamIndex, long.MinValue, timestamp, timestamp, 0).ThrowIfError();
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

        /// <summary>
        /// Read packets from media
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FFmpegException"></exception>
        public IEnumerable<MediaPacket> ReadPackets(MediaPacket inPacket = null)
        {
            MediaPacket packet = inPacket ?? new MediaPacket();
            try
            {
                int ret;
                do
                {
                    ret = ReadPacketSafe(packet);
                    try
                    {
                        if (ret < 0 && ret != ffmpeg.AVERROR_EOF)
                            ret.ThrowIfError();
                        yield return packet;
                    }
                    finally { packet.Unref(); }
                } while (ret >= 0);
            }
            finally { if (inPacket == null) packet.Dispose(); }
        }

        protected int ReadPacketSafe(MediaPacket packet)
        {
            return ffmpeg.av_read_frame(pFormatContext, packet);
        }

        #endregion IEnumerable<MediaPacket>

        #region IDisposable
        private bool disposedValue;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (_stream != null)
                    _stream.Dispose();
                if (pFormatContext != null)
                {
                    fixed (AVFormatContext** ppFormatContext = &pFormatContext)
                    {
                        ffmpeg.av_freep(&pFormatContext->pb->buffer);
                        ffmpeg.avio_context_free(&pFormatContext->pb);
                        ffmpeg.avformat_close_input(ppFormatContext);
                    }
                    pFormatContext = null;
                }
                disposedValue = true;
            }
        }

        #endregion IDisposable
    }
}
