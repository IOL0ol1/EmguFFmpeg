using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using FFmpeg.AutoGen;
using FFmpegSharp.Internal;

namespace FFmpegSharp
{
    public unsafe class MediaDemuxer : MediaFormatContext, IReadOnlyList<MediaStream>
    {
        private Stream _stream;

        /// <summary>
        /// Get <see cref="AVInputFormat"/>
        /// </summary>
        public InFormat Format { get; protected set; }


        public string Url => ((IntPtr)pFormatContext->url).PtrToStringUTF8();

        /// <summary>
        /// Load stream 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="iformat"></param>
        /// <param name="options"></param>
        public static MediaDemuxer Open(Stream stream, InFormat iformat = null, MediaDictionary options = null)
        {
            var pFormatContext = ffmpeg.avformat_alloc_context();
            pFormatContext->pb = MediaIOContext.Link(stream);
            ffmpeg.avformat_open_input(&pFormatContext, null, iformat, options).ThrowIfError();
            ffmpeg.avformat_find_stream_info(pFormatContext, options).ThrowIfError();
            var output = new MediaDemuxer(pFormatContext)
            {
                _stream = stream,
            };
            return output;
        }

        /// <summary>
        /// Load path
        /// </summary>
        /// <param name="url"></param>
        /// <param name="iformat"></param>
        /// <param name="options"></param>
        /// <param name="context"></param>
        public static MediaDemuxer Open(string url, InFormat iformat = null, MediaDictionary options = null, MediaFormatContext context = null)
        {
            AVFormatContext* pFormatContext = context;
            ffmpeg.avformat_open_input(&pFormatContext, url, iformat, options).ThrowIfError();
            ffmpeg.avformat_find_stream_info(pFormatContext, options).ThrowIfError();
            var output = new MediaDemuxer(pFormatContext);
            return output;
        }

        public MediaDemuxer(AVFormatContext* formatContext, bool isOwner = true)
            : base(formatContext)
        {
            if (formatContext == null) throw new NullReferenceException();
            disposedValue = !isOwner;
            Format = new InFormat(pFormatContext->iformat);
            for (int i = 0; i < pFormatContext->nb_streams; i++)
            {
                streams.Add(new MediaStream(pFormatContext->streams[i]));
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

        #region IReadOnlyList<MediaStream>

        protected List<MediaStream> streams = new List<MediaStream>();

        /// <summary>
        /// stream count in mux.
        /// </summary>
        public int Count => (int)pFormatContext->nb_streams;

        /// <summary>
        /// get stream
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public MediaStream this[int index] => streams[index];

        /// <summary>
        /// enum stream
        /// </summary>
        /// <returns></returns>
        public IEnumerator<MediaStream> GetEnumerator()
        {
            return streams.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion IReadOnlyList<MediaStream>

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

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                _stream?.Dispose();
                if (pFormatContext != null)
                {
                    fixed (AVFormatContext** ppFormatContext = &pFormatContext)
                    {
                        ffmpeg.avformat_close_input(ppFormatContext);
                    }
                    pFormatContext = null;
                }
                disposedValue = true;
            }
            base.Dispose(disposing);
        }

        ~MediaDemuxer()
        {
            Dispose(disposing: false);
        }
        #endregion IDisposable
    }
}
