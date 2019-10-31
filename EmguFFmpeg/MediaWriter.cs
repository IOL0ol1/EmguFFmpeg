using FFmpeg.AutoGen;

using System;
using System.IO;
using System.Linq;

namespace EmguFFmpeg
{
    public unsafe class MediaWriter : MediaMux
    {
        public new OutFormat Format => base.Format as OutFormat;

        public MediaWriter(Stream stream, OutFormat oformat, MediaDictionary options = null)
        {
            baseStream = stream;
            avio_Alloc_Context_Read_Packet = ReadFunc;
            avio_Alloc_Context_Write_Packet = WriteFunc;
            avio_Alloc_Context_Seek = SeekFunc;
            pFormatContext = ffmpeg.avformat_alloc_context();
            pIOContext = ffmpeg.avio_alloc_context((byte*)ffmpeg.av_malloc(bufferLength), bufferLength, 1, null,
                avio_Alloc_Context_Read_Packet, avio_Alloc_Context_Write_Packet, avio_Alloc_Context_Seek);
            pFormatContext->oformat = oformat;
            base.Format = oformat;
            if ((pFormatContext->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
                pFormatContext->pb = pIOContext;
        }

        /// <summary>
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
                ffmpeg.avformat_alloc_output_context2(ppFormatContext, oformat, null, file).ThrowExceptionIfError();
            }
            base.Format = oformat ?? new OutFormat(pFormatContext->oformat);
            if ((pFormatContext->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
                ffmpeg.avio_open2(&pFormatContext->pb, file, ffmpeg.AVIO_FLAG_WRITE, null, options).ThrowExceptionIfError();
        }

        /// <summary>
        /// <see cref=" ffmpeg.av_dump_format(AVFormatContext*, int, string, int)"/>
        /// </summary>
        public override void DumpInfo()
        {
            for (int i = 0; i < pFormatContext->nb_streams; i++)
            {
                ffmpeg.av_dump_format(pFormatContext, i, ((IntPtr)pFormatContext->url).PtrToStringUTF8(), 1);
            }
        }

        /// <summary>
        /// Add stream by encode
        /// <para><see cref="ffmpeg.avformat_new_stream(AVFormatContext*, AVCodec*)"/></para>
        /// <para>set <see cref="AVStream.id"/></para>
        /// <para><see cref="ffmpeg.avcodec_parameters_from_context(AVCodecParameters*, AVCodecContext*)"/></para>
        /// <para>set <see cref="AVStream.time_base"/> from <see cref="AVCodecContext.time_base"/></para>
        /// </summary>
        /// <param name="encode">Used to codec stream.
        /// <para>
        /// set null to add a data stream but no encoder,
        /// then use <see cref="WritePacket(MediaPacket)"/> write data directly.
        /// </para>
        /// </param>
        /// <returns></returns>
        public MediaStream AddStream(MediaEncode encode)
        {
            AVStream* stream = ffmpeg.avformat_new_stream(pFormatContext, null);
            stream->id = (int)(pFormatContext->nb_streams - 1);
            // keep the order
            if (encode != null)
            {
                ffmpeg.avcodec_parameters_from_context(stream->codecpar, encode);
                stream->time_base = encode.AVCodecContext.time_base;
            }
            streams.Add(new MediaStream(stream) { Codec = encode });
            return streams.Last();
        }

        /// <summary>
        /// <see cref="ffmpeg.avformat_write_header(AVFormatContext*, AVDictionary**)"/>
        /// </summary>
        /// <param name="options"></param>
        public void Initialize(MediaDictionary options = null)
        {
            ffmpeg.avformat_write_header(pFormatContext, options).ThrowExceptionIfError();
        }

        /// <summary>
        /// <para><see cref="ffmpeg.av_interleaved_write_frame(AVFormatContext*, AVPacket*)"/></para>
        /// <para><see cref="ffmpeg.av_packet_unref"/></para>
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public int WritePacket(MediaPacket packet)
        {
            int ret = ffmpeg.av_interleaved_write_frame(pFormatContext, packet);
            packet.Clear();
            return ret;
        }

        /// <summary>
        /// send null frame and receive packet to flush encoder cache.
        /// <para><see cref="MediaStream.WriteFrame(MediaFrame)"/></para>
        /// <para><see cref="WritePacket(MediaPacket)"/></para>
        /// <para><see cref="ffmpeg.av_write_trailer(AVFormatContext*)"/></para>
        /// </summary>
        public void FlushMuxer()
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
            ffmpeg.av_write_trailer(pFormatContext);
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
                            ffmpeg.avio_context_free(&pFormatContext->pb);
                            baseStream.Dispose();
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
                    pIOContext = null;
                }
            }
        }

        #endregion
    }
}