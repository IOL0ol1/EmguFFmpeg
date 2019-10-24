using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;

namespace EmguFFmpeg
{
    public unsafe partial class MediaReader : MediaMux
    {
        public new InFormat Format => base.Format as InFormat;

        public MediaReader(string file, InFormat iformat = null)
        {
            fixed (AVFormatContext** ppFormatContext = &pFormatContext)
            {
                ffmpeg.avformat_open_input(ppFormatContext, file, iformat, null).ThrowExceptionIfError();
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

        public override void DumpInfo()
        {
            for (int i = 0; i < pFormatContext->nb_streams; i++)
            {
                ffmpeg.av_dump_format(pFormatContext, i, ((IntPtr)pFormatContext->url).PtrToStringUTF8(), 0);
            }
        }

        public void Seek(TimeSpan time, int streamIndex = -1)
        {
            long timestamp = (long)(time.TotalSeconds * ffmpeg.AV_TIME_BASE);
            if (streamIndex >= 0)
                timestamp = ffmpeg.av_rescale_q(timestamp, ffmpeg.av_get_time_base_q(), streams[streamIndex].TimeBase);
            ffmpeg.avformat_seek_file(pFormatContext, streamIndex, long.MinValue, timestamp, timestamp, 0).ThrowExceptionIfError();
        }

        #region IEnumerable<MediaPacket>

        public IEnumerable<MediaPacket> Packets
        {
            get
            {
                using (MediaPacket packet = new MediaPacket())
                {
                    int ret;
                    do
                    {
                        ret = ReadPacket(packet);
                        yield return packet;
                        packet.Wipe();
                    } while (ret >= 0);
                }
            }
        }

        private int ReadPacket(MediaPacket packet)
        {
            return ffmpeg.av_read_frame(pFormatContext, packet);
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (pFormatContext != null)
            {
                fixed (AVFormatContext** ppFormatContext = &pFormatContext)
                {
                    ffmpeg.avformat_close_input(ppFormatContext);
                    pFormatContext = null;
                }
            }
        }

        #endregion
    }
}