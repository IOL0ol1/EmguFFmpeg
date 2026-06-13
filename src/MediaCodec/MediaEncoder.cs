using System;
using System.Collections;
using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    public unsafe partial class MediaEncoder : MediaCodecContext
    {
        /// <summary>Fluent builder for video encoders. See <see cref="VideoEncoderBuilder"/>.</summary>
        public static VideoEncoderBuilder Video() => new VideoEncoderBuilder();
        /// <summary>Fluent builder for audio encoders. See <see cref="AudioEncoderBuilder"/>.</summary>
        public static AudioEncoderBuilder Audio() => new AudioEncoderBuilder();

        public MediaEncoder(AVCodecContext* pAVCodecContext, bool leaveOpen)
            : base(pAVCodecContext, leaveOpen)
        { }

        public MediaEncoder(MediaCodec codec = null)
            : base(codec)
        { }

        /// <summary>Open the encoder. See <see cref="MediaCodecContext.Open"/>.</summary>
        public new MediaEncoder Open(MediaDictionary opts = null) => (MediaEncoder)base.Open(opts);

        #region Create

        /// <summary>
        /// Create and open a <see cref="MediaEncoder"/> from <see cref="AVCodecParameters"/> (transcode/remux scenarios).
        /// <para>
        /// For configuration-driven creation use the <see cref="Video"/>/<see cref="Audio"/> builders;
        /// for custom before-open configuration use the ctor + <see cref="Open"/>.
        /// </para>
        /// </summary>
        /// <param name="codecParameters">Source codec parameters.</param>
        /// <param name="opts">Codec options forwarded to <see cref="Open"/>.</param>
        /// <returns>An opened encoder, or null when codec_id is AV_CODEC_ID_NONE / no encoder is found.</returns>
        public static MediaEncoder CreateEncoder(AVCodecParameters codecParameters, MediaDictionary opts = null)
        {
            var codec = MediaCodec.FindEncoder(codecParameters.codec_id);
            // If codec_id is AV_CODEC_ID_NONE return null
            if (codec == null) return null;
            var output = new MediaEncoder(codec);
            output.SetCodecParameters(ref codecParameters);
            return output.Open(opts);
        }
        #endregion

        /// <summary>
        /// <see cref="ffmpeg.avcodec_send_frame(AVCodecContext*, AVFrame*)"/>
        /// </summary>
        /// <param name="frame"></param>
        /// <returns>
        /// 0 on success, otherwise negative error code: AVERROR(EAGAIN): input is not accepted
        /// in the current state - user must read output with avcodec_receive_packet() (once
        /// all output is read, the packet should be resent, and the call will not fail with
        /// EAGAIN). AVERROR_EOF: the encoder has been flushed, and no new frames can be
        /// sent to it AVERROR(EINVAL): codec not opened, it is a decoder, or requires flush
        /// AVERROR(ENOMEM): failed to add packet to internal queue, or similar other errors:
        /// legitimate encoding errors</returns>
        public int SendFrame(MediaFrame frame) => ffmpeg.avcodec_send_frame(pCodecContext, frame);

        /// <summary>
        /// <see cref="ffmpeg.avcodec_receive_packet(AVCodecContext*, AVPacket*)"/>
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public int ReceivePacket(MediaPacket packet) => ffmpeg.avcodec_receive_packet(pCodecContext, packet);

        /// <summary>
        /// Encode <paramref name="frame"/> into one or more packets.
        /// <para>
        /// Pass <see langword="null"/> for <paramref name="frame"/> to enter draining mode (signal EOF to the encoder).
        /// </para>
        /// <para>
        /// The yielded <see cref="MediaPacket"/> is reused across iterations — call <see cref="MediaPacket.Clone"/>
        /// before storing it past the next MoveNext.
        /// </para>
        /// </summary>
        /// <param name="frame">Input frame, or <see langword="null"/> to flush.</param>
        /// <param name="inPacket">Optional reusable packet (avoids per-call allocation).</param>
        /// <returns>
        /// A single-pass receive sequence over the encoder's buffered output. <c>foreach</c> compiles against the
        /// struct enumerator (zero allocation); using it as <see cref="IEnumerable{T}"/> (LINQ etc.) boxes.
        /// The frame is sent eagerly at call time — not enumerating only delays packet retrieval, it cannot lose data.
        /// </returns>
        public Packets EncodeFrame(MediaFrame frame, MediaPacket inPacket = null)
        {
            int ret = SendFrame(frame);
            // EAGAIN here means the encoder's internal queue is full; the caller is expected to drain at least one
            // packet before retrying. We treat EOF as "no more frames" but still drain whatever is buffered.
            if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                throw new FFmpegException(ret,
                    "avcodec_send_frame returned EAGAIN. Drain the encoder with ReceivePacket before sending more frames, or use EncodeFrame(frame) only after enumerating the previous call to completion.");
            if (ret != ffmpeg.AVERROR_EOF)
                ret.ThrowIfError();
            return new Packets(this, inPacket);
        }

        /// <summary>Single-pass receive sequence returned by <see cref="EncodeFrame"/>.</summary>
        public readonly struct Packets : IEnumerable<MediaPacket>
        {
            private readonly MediaEncoder _encoder;
            private readonly MediaPacket _inPacket;

            internal Packets(MediaEncoder encoder, MediaPacket inPacket)
            {
                _encoder = encoder;
                _inPacket = inPacket;
            }

            public Enumerator GetEnumerator() => new Enumerator(_encoder, _inPacket);

            public struct Enumerator : IEnumerator<MediaPacket>
            {
                private readonly MediaEncoder _encoder;
                private readonly bool _ownsPacket;
                private readonly int _eagain;
                private MediaPacket _packet;
                private MediaPacket _current;

                internal Enumerator(MediaEncoder encoder, MediaPacket inPacket)
                {
                    _encoder = encoder;
                    _ownsPacket = inPacket == null;
                    _packet = inPacket ?? new MediaPacket();
                    _eagain = ffmpeg.AVERROR(ffmpeg.EAGAIN);
                    _current = null;
                }

                public MediaPacket Current => _current;

                public bool MoveNext()
                {
                    ReleaseCurrent();
                    int ret = _encoder.ReceivePacket(_packet);
                    if (ret == _eagain || ret == ffmpeg.AVERROR_EOF) return false;
                    ret.ThrowIfError();
                    _current = _packet;
                    return true;
                }

                public void Dispose()
                {
                    ReleaseCurrent(); // runs on early break too
                    if (_ownsPacket && _packet != null)
                    {
                        _packet.Dispose();
                        _packet = null;
                    }
                }

                private void ReleaseCurrent()
                {
                    if (_current == null) return;
                    _packet.Unref();
                    _current = null;
                }

                object IEnumerator.Current => Current;
                void IEnumerator.Reset() => throw new NotSupportedException();
            }

            // Interface fallback (boxes) — keeps LINQ and IEnumerable<T> consumers working.
            IEnumerator<MediaPacket> IEnumerable<MediaPacket>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
