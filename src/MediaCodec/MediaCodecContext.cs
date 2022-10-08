using System;
using System.Collections.Generic;

using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    public unsafe partial class MediaCodecContext : MediaCodecContextSettings, IDisposable
    {
        private bool disposedValue;

        public MediaCodecContext(AVCodecContext* pAVCodecContext, bool isDisposeByOwner = true)
        {
            pCodecContext = pAVCodecContext;
            disposedValue = !isDisposeByOwner;
        }

        public MediaCodecContext(IntPtr pAVCodecContext, bool isDisposeByOwner = true)
            : this((AVCodecContext*)pAVCodecContext, isDisposeByOwner)
        { }


        public MediaCodecContext(MediaCodec codec = null)
                    : this(ffmpeg.avcodec_alloc_context3(codec), true)
        { }

        /// <summary>
        /// <see cref="ffmpeg.avcodec_open2(AVCodecContext*, AVCodec*, AVDictionary**)"/>
        /// </summary>
        /// <param name="beforeOpenSetting"></param>
        /// <param name="codec"></param>
        /// <param name="opts"></param>
        public MediaCodecContext Open(Action<MediaCodecContextSettings> beforeOpenSetting, MediaCodec codec = null, MediaDictionary opts = null)
        {
            beforeOpenSetting?.Invoke(this);
            ffmpeg.avcodec_open2(pCodecContext, codec, opts).ThrowIfError();
            return this;
        }

        /// <summary>
        /// encode frame to packet
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="inPacket"></param>
        /// <returns></returns>
        public IEnumerable<MediaPacket> EncodeFrame(MediaFrame frame, MediaPacket inPacket = null)
        {
            int ret = SendFrame(frame);
            if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
                yield break;
            ret.ThrowIfError();
            MediaPacket packet = inPacket ?? new MediaPacket();
            try
            {
                while (true)
                {
                    ret = ReceivePacket(packet);
                    if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
                        yield break;
                    try
                    {
                        ret.ThrowIfError();
                        yield return packet;
                    }
                    finally { packet.Unref(); }
                }
            }
            finally
            {
                if (inPacket == null) packet.Dispose();
            }
        }

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


        #region Safe wapper for IEnumerable

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
        /// <see cref="ffmpeg.avcodec_send_packet(AVCodecContext*, AVPacket*)"/>
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public int SendPacket(MediaPacket packet) => ffmpeg.avcodec_send_packet(pCodecContext, packet);

        /// <summary>
        /// <see cref="ffmpeg.avcodec_receive_frame(AVCodecContext*, AVFrame*)"/>
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public int ReceiveFrame(MediaFrame frame) => ffmpeg.avcodec_receive_frame(pCodecContext, frame);

        #endregion safe wapper for IEnumerable

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                fixed (AVCodecContext** ppCodecContext = &pCodecContext)
                {
                    ffmpeg.avcodec_free_context(ppCodecContext);
                }
                disposedValue = true;
            }
        }

        ~MediaCodecContext()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    public unsafe abstract class MediaCodecContextSettings
    {
        protected AVCodecContext* pCodecContext = null;

        public static implicit operator AVCodecContext*(MediaCodecContextSettings value)
        {
            if (value == null) return null;
            return value.pCodecContext;
        }
        public AVCodecContext AVCodecContext => *pCodecContext;

        public AVMediaType CodecType
        {
            get => pCodecContext->codec_type;
            set => pCodecContext->codec_type = value;
        }

        public int Height
        {
            get => pCodecContext->height;
            set => pCodecContext->height = value;
        }

        public int Width
        {
            get => pCodecContext->width;
            set => pCodecContext->width = value;
        }

        public AVRational TimeBase
        {
            get => pCodecContext->time_base;
            set => pCodecContext->time_base = value;
        }

        public AVRational FrameRate
        {
            get => pCodecContext->framerate;
            set => pCodecContext->framerate = value;
        }

        public int FrameSize
        {
            get => pCodecContext->frame_size;
            set => pCodecContext->frame_size = value;
        }

        public AVPixelFormat PixFmt
        {
            get => pCodecContext->pix_fmt;
            set => pCodecContext->pix_fmt = value;
        }

        public long BitRate
        {
            get => pCodecContext->bit_rate;
            set => pCodecContext->bit_rate = value;
        }


        public int Refs
        {
            get => pCodecContext->refs;
            set => pCodecContext->refs = value;
        }

        public int SampleRate
        {
            get => pCodecContext->sample_rate;
            set => pCodecContext->sample_rate = value;
        }

        public AVChannelLayout ChLayout
        {
            get => pCodecContext->ch_layout;
            set => pCodecContext->ch_layout = value;
        }

        public AVSampleFormat SampleFmt
        {
            get => pCodecContext->sample_fmt;
            set => pCodecContext->sample_fmt = value;
        }

        public int Flag
        {
            get => pCodecContext->flags;
            set => pCodecContext->flags = value;
        }

        public int Profile
        {
            get => pCodecContext->profile;
            set => pCodecContext->profile = value;
        }

        public int Level
        {
            get => pCodecContext->level;
            set => pCodecContext->level = value;
        }
    }


}
