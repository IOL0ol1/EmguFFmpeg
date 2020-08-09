using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace EmguFFmpeg
{
    public partial class MediaReader : MediaMux
    {
        public new InFormat Format => base.Format as InFormat;

        public MediaReader(Stream stream, InFormat iformat = null, MediaDictionary options = null)
            : this(stream, 4096, iformat, options) { }

        public MediaReader(Stream stream, int buffersize, InFormat iformat = null, MediaDictionary options = null)
        {
            unsafe
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
                    ffmpeg.avformat_open_input(ppFormatContext, null, iformat, options).ThrowExceptionIfError();
                }
                ffmpeg.avformat_find_stream_info(pFormatContext, null).ThrowExceptionIfError();
                base.Format = iformat ?? new InFormat(pFormatContext->iformat);

                for (int i = 0; i < pFormatContext->nb_streams; i++)
                {
                    AVStream* pStream = pFormatContext->streams[i];
                    MediaDecode codec = MediaDecode.CreateDecode(pStream->codecpar->codec_id, _ =>
                    {
                        ffmpeg.avcodec_parameters_to_context(_, pStream->codecpar);
                    });
                    streams.Add(new MediaStream(pStream) { Codec = codec });
                }
            }
        }

        public MediaReader(string file, InFormat iformat = null, MediaDictionary options = null)
        {
            unsafe
            {
                fixed (AVFormatContext** ppFormatContext = &pFormatContext)
                {
                    ffmpeg.avformat_open_input(ppFormatContext, file, iformat, options).ThrowExceptionIfError();
                }
                ffmpeg.avformat_find_stream_info(pFormatContext, null).ThrowExceptionIfError();
                base.Format = iformat ?? new InFormat(pFormatContext->iformat);

                for (int i = 0; i < pFormatContext->nb_streams; i++)
                {
                    AVStream* pStream = pFormatContext->streams[i];
                    MediaDecode codec = MediaDecode.CreateDecode(pStream->codecpar->codec_id, _ =>
                    {
                        ffmpeg.avcodec_parameters_to_context(_, pStream->codecpar);
                    });
                    streams.Add(new MediaStream(pStream) { Codec = codec });
                }
            }
        }

        public override void DumpInfo()
        {
            unsafe
            {
                for (int i = 0; i < pFormatContext->nb_streams; i++)
                {
                    ffmpeg.av_dump_format(pFormatContext, i, ((IntPtr)pFormatContext->url).PtrToStringUTF8(), 0);
                }
            }
        }

        /// <summary>
        /// Seek timestamp base <see cref="ffmpeg.AV_TIME_BASE"/>. it's precision than <see cref="Seek(TimeSpan, int)"/>
        /// <para></para>
        /// </summary>
        /// <param name="timestamp">Seconds * <see cref="ffmpeg.AV_TIME_BASE"/> </param>
        /// <param name="streamIndex"></param>
        public void Seek(long timestamp, int streamIndex = -1)
        {
            unsafe
            {
                if (streamIndex >= 0)
                    timestamp = ffmpeg.av_rescale_q(timestamp, ffmpeg.av_get_time_base_q(), streams[streamIndex].TimeBase);
                ffmpeg.avformat_seek_file(pFormatContext, streamIndex, long.MinValue, timestamp, long.MaxValue, 0).ThrowExceptionIfError();
                foreach (var stream in streams)
                {
                    if (stream.Index == streamIndex || streamIndex < 0)
                        ffmpeg.avcodec_flush_buffers(stream.Codec);
                }
            }
        }

        /// <summary>
        /// <see cref="Seek(long, int)"/>
        /// </summary>
        /// <param name="time"></param>
        /// <param name="streamIndex"></param>
        public void Seek(TimeSpan time, int streamIndex = -1)
        {
            Seek((long)(time.TotalSeconds * ffmpeg.AV_TIME_BASE), streamIndex);
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
                    packet.Clear();
                } while (ret >= 0);
            }
        }

        public int ReadPacket([Out] MediaPacket packet)
        {
            unsafe
            {
                return ffmpeg.av_read_frame(pFormatContext, packet);
            }
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            unsafe
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
        }

        #endregion
    }
}