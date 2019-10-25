using FFmpeg.AutoGen;

using System;
using System.Linq;

namespace EmguFFmpeg
{
    public unsafe class MediaWriter : MediaMux
    {
        public new OutFormat Format => base.Format as OutFormat;

        public MediaWriter(string file, OutFormat oformat = null, MediaDictionary options = null)
        {
            fixed (AVFormatContext** ppFormatContext = &pFormatContext)
            {
                ffmpeg.avformat_alloc_output_context2(ppFormatContext, oformat, null, file).ThrowExceptionIfError();
            }
            base.Format = oformat ?? new OutFormat(pFormatContext->oformat);
            if ((pFormatContext->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
                ffmpeg.avio_open2(&pFormatContext->pb, file, ffmpeg.AVIO_FLAG_WRITE, null, options).ThrowExceptionIfError();
            Options = options;
        }

        public override void DumpInfo()
        {
            for (int i = 0; i < pFormatContext->nb_streams; i++)
            {
                ffmpeg.av_dump_format(pFormatContext, i, ((IntPtr)pFormatContext->url).PtrToStringUTF8(), 1);
            }
        }

        public MediaStream AddStream(MediaEncode encode)
        {
            AVStream* stream = ffmpeg.avformat_new_stream(pFormatContext, null);
            stream->id = (int)(pFormatContext->nb_streams - 1);
            // keep the order
            ffmpeg.avcodec_parameters_from_context(stream->codecpar, encode);
            stream->time_base = encode.AVCodecContext.time_base;
            streams.Add(new MediaStream(stream) { Codec = encode });
            return streams.Last();
        }

        public void Initialize(MediaDictionary options = null)
        {
            ffmpeg.avformat_write_header(pFormatContext, options).ThrowExceptionIfError();
        }

        public int WritePacket(MediaPacket packet)
        {
            int ret = ffmpeg.av_interleaved_write_frame(pFormatContext, packet);
            packet.Wipe();
            return ret;
        }

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
        }

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (pFormatContext != null)
            {
                try
                {
                    ffmpeg.av_write_trailer(pFormatContext);
                    // Close the output file.
                    if ((pFormatContext->flags & ffmpeg.AVFMT_NOFILE) == 0)
                        ffmpeg.avio_closep(&pFormatContext->pb);
                }
                finally
                {
                    ffmpeg.avformat_free_context(pFormatContext);
                    pFormatContext = null;
                }
            }
        }

        #endregion
    }
}