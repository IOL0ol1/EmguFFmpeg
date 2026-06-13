using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    /// <summary>
    /// Wraps <see cref="AVCodecParserContext"/>.
    /// Use this when you have a raw elementary stream (e.g. an Annex-B .h264 file) and need to split it
    /// into <see cref="AVPacket"/>-sized chunks before feeding a decoder.
    /// </summary>
    public unsafe class MediaCodecParserContext : IDisposable
    {
        /// <summary>Default rolling buffer size when streaming from a managed <see cref="Stream"/>.</summary>
        public const int DefaultStreamBufferSize = 32 * 1024;

        protected AVCodecParserContext* pCodecParserContext;


        public MediaCodecParserContext(AVCodecParserContext* pAVCodecParserContext, bool leaveOpen)
        {
            pCodecParserContext = pAVCodecParserContext;
            disposedValue = leaveOpen;
        }

        public MediaCodecParserContext(int codecId)
            : this(ffmpeg.av_parser_init(codecId), false)
        { }

        public MediaCodecParserContext(AVCodecID codecId)
            : this((int)codecId)
        { }

        public static IEnumerable<AVCodecParser> GetParsers()
            => NativeIterate.Cursor(o => (IntPtr)ffmpeg.av_parser_iterate(o), p => *(AVCodecParser*)p);

        /// <summary>
        /// Parse complete packets from a managed <see cref="Stream"/>.
        /// <para>
        /// Each yielded <see cref="MediaPacket"/> owns its data via <see cref="ffmpeg.av_packet_from_data"/>, so the
        /// consumer is free to enqueue or process it asynchronously. The packet is unrefed automatically on the next
        /// MoveNext to keep the steady-state footprint constant — call <see cref="MediaPacket.Clone"/> if you need to outlive that.
        /// </para>
        /// </summary>
        /// <param name="codecContext">Decoder context for the same codec id this parser was initialized with.</param>
        /// <param name="stream">Source byte stream containing the elementary stream.</param>
        /// <param name="bufferSize">Size of the rolling buffer used to feed FFmpeg (must include AV_INPUT_BUFFER_PADDING_SIZE).</param>
        public IEnumerable<MediaPacket> ParsePackets(MediaCodecContext codecContext, Stream stream, int bufferSize = DefaultStreamBufferSize)
        {
            if (codecContext == null) throw new ArgumentNullException(nameof(codecContext));
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (bufferSize <= ffmpeg.AV_INPUT_BUFFER_PADDING_SIZE)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            // Rent from the shared pool so a long-running parser doesn't churn the GC heap.
            var rented = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                int outSize;
                while ((outSize = stream.Read(rented, 0, bufferSize)) > 0)
                {
                    int offset = 0;
                    while (offset < outSize)
                    {
                        // pkt is owned by this method only — yield Clones so consumers' lifetime is independent.
                        using (var pkt = new MediaPacket())
                        {
                            int consumed = ParseInto(codecContext, pkt, new ReadOnlySpan<byte>(rented, offset, outSize - offset));
                            offset += consumed;
                            if (pkt.Ref.size > 0)
                                yield return pkt; // pkt.Dispose() runs on next MoveNext; consumer must Clone to outlive that.
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        /// <summary>
        /// Parse a chunk of bytes into <paramref name="packet"/>. Returns the number of bytes consumed.
        /// The <paramref name="packet"/>'s data pointer is replaced with a refcounted copy of the parsed slice, so
        /// it remains valid after <paramref name="input"/> goes out of scope.
        /// </summary>
        public int ParseInto(MediaCodecContext codecContext, MediaPacket packet, ReadOnlySpan<byte> input)
        {
            if (codecContext == null) throw new ArgumentNullException(nameof(codecContext));
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            if (input.Length == 0) return 0;

            byte* poutbuf = null;
            int poutbufSize = 0;
            int consumed;
            fixed (byte* pIn = input)
            {
                consumed = ffmpeg.av_parser_parse2(
                    pCodecParserContext, codecContext,
                    &poutbuf, &poutbufSize,
                    pIn, input.Length,
                    packet.Ref.pts, packet.Ref.dts, packet.Ref.pos);
            }
            consumed.ThrowIfError();

            if (poutbufSize > 0)
            {
                // av_parser_parse2 returns a pointer that aliases either the input or an internal parser-owned buffer.
                // Either way, we MUST copy into a packet-owned, refcounted buffer before returning to managed code,
                // otherwise the data pointer can become dangling.
                ffmpeg.av_new_packet(packet, poutbufSize).ThrowIfError();
                Buffer.MemoryCopy(poutbuf, packet.Ref.data, packet.Ref.size, poutbufSize);
            }
            else
            {
                packet.Ref.data = null;
                packet.Ref.size = 0;
            }
            return consumed;
        }

        /// <summary>
        /// Low-level passthrough to <see cref="ffmpeg.av_parser_parse2"/>. Caller is responsible for ensuring
        /// <paramref name="buf"/> lives until the resulting packet data is consumed or copied.
        /// </summary>
        public int Parser2(MediaCodecContext codecContext, IntPtr poutbuf, IntPtr poutbufSize, IntPtr buf, int bufSize, long pts, long dts, long pos)
        {
            return ffmpeg.av_parser_parse2(pCodecParserContext, codecContext, (byte**)poutbuf, (int*)poutbufSize, (byte*)buf, bufSize, pts, dts, pos);
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (pCodecParserContext != null)
                {
                    ffmpeg.av_parser_close(pCodecParserContext);
                    pCodecParserContext = null;
                }
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
