using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    public unsafe class MediaDecoder : IDisposable
    {
        private bool disposedValue;

        public static implicit operator MediaCodecContext(MediaDecoder value)
        {
            if (value == null) return null;
            return value.Context;
        }

        public static implicit operator AVCodecContext*(MediaDecoder value)
        {
            if (value == null) return null;
            return value.Context;
        }

        public MediaCodecContext Context { get; private set; }


        public MediaDecoder(MediaCodecContext context, bool isDisposeByOwner = true)
        {
            Context = context;
            disposedValue = !isDisposeByOwner;
        }

        /// <summary>
        /// <see cref="ffmpeg.avcodec_send_packet(AVCodecContext*, AVPacket*)"/>
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public int SendPacket(MediaPacket packet) => ffmpeg.avcodec_send_packet(Context, packet);

        /// <summary>
        /// <see cref="ffmpeg.avcodec_receive_frame(AVCodecContext*, AVFrame*)"/>
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public int ReceiveFrame(MediaFrame frame) => ffmpeg.avcodec_receive_frame(Context, frame);

        /// <summary>
        /// decode packet to get frame.
        /// TODO: add SubtitleFrame support
        /// <para>
        /// <see cref="SendPacket(MediaPacket)"/> and <see cref="ReceiveFrame(MediaFrame)"/>
        /// </para>
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="inFrame"></param>
        /// <returns></returns>
        public IEnumerable<MediaFrame> DecodePacket(MediaPacket packet, MediaFrame inFrame = null)
        {
            int ret = SendPacket(packet);
            if (ret < 0 && ret != ffmpeg.AVERROR(ffmpeg.EAGAIN) && ret != ffmpeg.AVERROR_EOF)
                ret.ThrowIfError();
            MediaFrame frame = inFrame ?? new MediaFrame();
            try
            {
                while (true)
                {
                    ret = ReceiveFrame(frame);
                    if (ret < 0)
                    {
                        // those two return values are special and mean there is no output
                        // frame available, but there were no errors during decoding
                        if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
                            yield break;
                        else
                            break;
                    }
                    yield return frame;
                }
            }
            finally { if (inFrame == null) frame.Dispose(); }
        }


        #region Create

        /// <summary>
        /// Create <see cref="AVCodecContext"/> by <see cref="AVCodecParameters"/>.
        /// <para>
        /// <seealso cref="MediaCodecContext.Open(Action{MediaCodecContextSettings}, MediaCodec, MediaDictionary)"/>
        /// </para>
        /// <para>
        /// <seealso cref="ffmpeg.avcodec_parameters_to_context(AVCodecContext*, AVCodecParameters*)"/>
        /// </para>
        /// </summary>
        /// <param name="codecParameters"></param>
        /// <param name="opts"></param>
        /// <returns></returns>
        public static MediaDecoder CreateDecoder(AVCodecParameters codecParameters, MediaDictionary opts = null)
        {
            var codec = MediaCodec.FindDecoder(codecParameters.codec_id);
            AVCodecParameters* pCodecParameters = &codecParameters;
            // If codec_id is AV_CODEC_ID_NONE return null
            return codec == null
                ? null
                : new MediaDecoder(MediaCodecContext.Create(_ => ffmpeg.avcodec_parameters_to_context(_, pCodecParameters).ThrowIfError(), codec, opts));
        }
 
        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // nothing
                }
                Context?.Dispose();
                disposedValue = true;
            }
        }

        ~MediaDecoder()
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
