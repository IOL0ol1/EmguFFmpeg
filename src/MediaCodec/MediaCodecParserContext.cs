using System;
using System.Collections.Generic;
using System.IO;

using FFmpeg.AutoGen;
using FFmpegSharp.Internal;
namespace FFmpegSharp
{
    public unsafe class MediaCodecParserContext : IDisposable
    {
        protected AVCodecParserContext* pCodecParserContext;


        public MediaCodecParserContext(AVCodecParserContext* pAVCodecParserContext, bool isDisposeByOwner = true)
        {
            pCodecParserContext = pAVCodecParserContext;
            disposedValue = !isDisposeByOwner;
        }

        public MediaCodecParserContext(int codecId)
            : this(ffmpeg.av_parser_init(codecId))
        { }

        public MediaCodecParserContext(AVCodecID codecId)
            : this((int)codecId)
        { }

        private static AVCodecParser? av_parser_iterate_safe(IntPtrPtr opaque)
        {
            fixed (void** pp = &opaque.Ptr)
            {
                var ret = ffmpeg.av_parser_iterate(pp);
                return ret == null ? (AVCodecParser?)null : *ret;
            }
        }

        public static IEnumerable<AVCodecParser> GetParsers()
        {
            AVCodecParser? output;
            IntPtrPtr opaque = new IntPtrPtr();
            while ((output = av_parser_iterate_safe(opaque)) != null)
            {
                yield return output.Value;
            }
        }

        /// <summary>
        /// TODO:
        /// </summary>
        /// <param name="codecContext"></param>
        /// <param name="stream"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public IEnumerable<MediaPacket> ParserPackets(MediaCodecContextBase codecContext, Stream stream, MediaPacket packet = null)
        {
            var bufSize = 20480 + 64; // buffer size + AV_INPUT_BUFFER_PADDING_SIZE
            var buf = new byte[bufSize];
            int outSize;
            var pkt = packet ?? new MediaPacket() { Dts = ffmpeg.AV_NOPTS_VALUE, Pts = ffmpeg.AV_NOPTS_VALUE, Pos = 0 };
            try
            {
                while ((outSize = stream.Read(buf, 0, bufSize)) != 0)
                {
                    for (int offset = 0; offset < outSize;)
                    {
                        var ret = Parser2(codecContext, pkt, buf, offset).ThrowIfError();
                        offset += ret;
                        if (packet.Size > 0)
                            yield return pkt;
                    }
                }
            }
            finally
            {
                if (packet == null) pkt?.Dispose();
            }
        }


        /// <summary>
        /// TODO:
        /// </summary>
        /// <param name="codecContext"></param>
        /// <param name="poutbuf"></param>
        /// <param name="poutbufSize"></param>
        /// <param name="buf"></param>
        /// <param name="bufSize"></param>
        /// <param name="pts"></param>
        /// <param name="dts"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public int Parser2(MediaCodecContextBase codecContext, IntPtrPtr<byte> poutbuf, IntPtr poutbufSize, IntPtr buf, int bufSize, long pts, long dts, long pos)
        {
            fixed (byte** pp = &poutbuf.Ptr)
            {
                return ffmpeg.av_parser_parse2(pCodecParserContext, codecContext, pp, (int*)poutbufSize, (byte*)buf, bufSize, pts, dts, pos);
            }
        }


        public int Parser2(MediaCodecContextBase codecContext, IntPtrPtr<byte> poutbuf, IntPtr poutbufSize, byte[] buf, long pts, long dts, long pos)
        {
            fixed (byte** pp = &poutbuf.Ptr)
            fixed (byte* pbuf = buf)
            {
                return ffmpeg.av_parser_parse2(pCodecParserContext, codecContext, pp, (int*)poutbufSize, pbuf, buf.Length, pts, dts, pos);
            }
        }

        public int Parser2(MediaCodecContextBase codecContext, MediaPacket packet, byte[] buf, int bufOffset = 0)
        {
            fixed (byte* pbuf = buf)
            {
                byte* pbufStart = pbuf + bufOffset;
                return ffmpeg.av_parser_parse2(pCodecParserContext, codecContext, &((AVPacket*)packet)->data, &((AVPacket*)packet)->size, pbufStart, buf.Length - bufOffset, packet.Pts, packet.Dts, packet.Pos);
            }
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // nothing
                }
                ffmpeg.av_parser_close(pCodecParserContext);
                disposedValue = true;
            }
        }

        ~MediaCodecParserContext()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
