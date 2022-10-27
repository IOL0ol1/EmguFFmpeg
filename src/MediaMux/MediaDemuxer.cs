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
        private MediaIOContext _ioContext;

        /// <summary>
        /// Get <see cref="AVInputFormat"/>
        /// </summary>
        public InFormat Format => new InFormat(pFormatContext->iformat);

        public string Url => ((IntPtr)pFormatContext->url).PtrToStringUTF8();

        /// <summary>
        /// Load stream 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="iformat"></param>
        /// <param name="options"></param>
        public static MediaDemuxer Open(Stream stream, InFormat iformat = null, MediaDictionary options = null)
        {
            var ioContext = (stream as MediaIOContext) ?? new MediaIOContext(stream, 32768);
            var output = Open(null, iformat, options, _ =>
            {
                AVFormatContext* f = _;
                f->pb = ioContext;
            });
            output._ioContext = ioContext;
            return output;
        }

        /// <summary>
        /// Load path
        /// </summary>
        /// <param name="url"></param>
        /// <param name="iformat"></param>
        /// <param name="options"></param>
        /// <param name="beforeOpen"></param>
        public static MediaDemuxer Open(string url, InFormat iformat = null, MediaDictionary options = null, Action<MediaFormatContextBase> beforeOpen = null)
        { 
            var output = new MediaDemuxer();
            beforeOpen?.Invoke(output);
            AVFormatContext* p = output;
            ffmpeg.avformat_open_input(&p, url, iformat, options).ThrowIfError();
            output.FindStreamInfo(options);
            return output;
        }

        public MediaDemuxer(AVFormatContext* pAVCodecContext, bool isDisposeByOwner = true)
            : base(pAVCodecContext, isDisposeByOwner)
        { }

        public MediaDemuxer()
            : base()
        { }

        public int FindStreamInfo(MediaDictionary options)
        {
            return ffmpeg.avformat_find_stream_info(pFormatContext, options).ThrowIfError();
        }

        /// <summary>
        /// Find the "best" stream in the file. The best stream is determined according to
        /// various heuristics as the most likely to be what the user expects. If the decoder
        /// parameter is non-NULL, av_find_best_stream will find the default decoder for
        /// the stream's codec; streams for which no decoder can be found are ignored.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="codec"></param>
        /// <param name="wantedStreamNb"></param>
        /// <param name="relatedStream"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public int FindBestStream(AVMediaType type, ref MediaCodec codec, int wantedStreamNb = -1, int relatedStream = -1, int flags = 0)
        {
            AVCodec* pCodec = codec;
            var ret = ffmpeg.av_find_best_stream(pFormatContext, type, wantedStreamNb, relatedStream, &pCodec, flags).ThrowIfError();
            if (codec == null)
                codec = new MediaCodec(pCodec);
            return ret;
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
                timestamp = ffmpeg.av_rescale_q(timestamp, ffmpeg.av_get_time_base_q(), pFormatContext->streams[streamIndex]->time_base);
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

        /// <summary>
        /// stream count in mux.
        /// </summary>
        public int Count => (int)pFormatContext->nb_streams;

        /// <summary>
        /// get stream
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public MediaStream this[int index] => new MediaStream(pFormatContext->streams[index]);

        /// <summary>
        /// enum stream
        /// </summary>
        /// <returns></returns>
        public IEnumerator<MediaStream> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
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
        private bool disposedValue;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (pFormatContext != null)
                {
                    if (_ioContext != null)
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
