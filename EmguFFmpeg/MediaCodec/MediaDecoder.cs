using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace EmguFFmpeg
{
    public unsafe class MediaDecoder : MediaCodec
    {
        public static MediaDecoder CreateDecoder(AVCodecID codecId, Action<MediaCodec> setBeforeOpen = null, MediaDictionary opts = null)
        {
            MediaDecoder encode = new MediaDecoder(codecId);
            encode.Initialize(setBeforeOpen, 0, opts);
            return encode;
        }

        public static MediaDecoder CreateDecoder(string codecName, Action<MediaCodec> setBeforeOpen = null, MediaDictionary opts = null)
        {
            MediaDecoder encode = new MediaDecoder(codecName);
            encode.Initialize(setBeforeOpen, 0, opts);
            return encode;
        }

        /// <summary>
        /// Find decoder by id
        /// <para>
        /// Must call <see cref="Initialize(Action{MediaCodec}, int, MediaDictionary)"/> before decode
        /// </para>
        /// </summary>
        /// <param name="codecId">codec id</param>
        public MediaDecoder(AVCodecID codecId)
        {
            if ((pCodec = ffmpeg.avcodec_find_decoder(codecId)) == null)
                throw new FFmpegException(ffmpeg.AVERROR_DECODER_NOT_FOUND);
        }

        /// <summary>
        /// Find decoder by name
        /// <para>
        /// Must call <see cref="Initialize(Action{MediaCodec}, int, MediaDictionary)"/> before decode
        /// </para>
        /// </summary>
        /// <param name="codecName">codec name</param>
        public MediaDecoder(string codecName)
        {
            if ((pCodec = ffmpeg.avcodec_find_decoder_by_name(codecName)) == null)
                throw new FFmpegException(ffmpeg.AVERROR_DECODER_NOT_FOUND);
        }

        /// <summary>
        /// <see cref="AVCodec"/> decoder adapter.
        /// </summary>
        /// <param name="pAVCodec"></param>
        public MediaDecoder(IntPtr pAVCodec)
        {
            pCodec = (AVCodec*)pAVCodec;
        }

        internal MediaDecoder(AVCodec* codec)
            : this((IntPtr)codec) { }

        /// <summary>
        /// alloc <see cref="AVCodecContext"/> and <see cref="ffmpeg.avcodec_open2(AVCodecContext*, AVCodec*, AVDictionary**)"/>
        /// </summary>
        /// <param name="setBeforeOpen">
        /// set <see cref="AVCodecContext"/> after <see cref="ffmpeg.avcodec_alloc_context3(AVCodec*)"/> and before <see cref="ffmpeg.avcodec_open2(AVCodecContext*, AVCodec*, AVDictionary**)"/>
        /// </param>
        /// <param name="flags">no used</param>
        /// <param name="opts">options for "avcodec_open2"</param>
        public override int Initialize(Action<MediaCodec> setBeforeOpen = null, int flags = 0, MediaDictionary opts = null)
        {
            pCodecContext = ffmpeg.avcodec_alloc_context3(pCodec);
            if (pCodecContext == null)
                throw new FFmpegException(FFmpegException.NullReference);
            setBeforeOpen?.Invoke(this);
            if (pCodec == null)
                throw new FFmpegException(FFmpegException.NullReference);
            return ffmpeg.avcodec_open2(pCodecContext, pCodec, opts).ThrowExceptionIfError();
        }

        /// <summary>
        /// decode packet to get frame.
        /// TODO: add SubtitleFrame support
        /// <para>
        /// <see cref="SendPacket(MediaPacket)"/> and <see cref="ReceiveFrame(MediaFrame)"/>
        /// </para>
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public virtual IEnumerable<MediaFrame> DecodePacket(MediaPacket packet)
        {
            if (SendPacket(packet) >= 0)
            {
                // if codoc type is video, create new video frame
                // else if codoc type is audio, create new audio frame
                // else throw a exception (e.g. codec type is subtitle)
                // TODO: add subtitle supported
                using (MediaFrame frame =
                    Type == AVMediaType.AVMEDIA_TYPE_VIDEO ?
                        new VideoFrame() :
                    Type == AVMediaType.AVMEDIA_TYPE_AUDIO ?
                        (MediaFrame)new AudioFrame() :
                        throw new FFmpegException(FFmpegException.NotSupportFrame))
                {
                    while (ReceiveFrame(frame) >= 0)
                    {
                        yield return frame;
                    }
                }
            }
        }

        #region safe for IEnumerable
        /// <summary>
        /// send packet to decoder
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public int SendPacket([In] MediaPacket packet)
        {
            if (pCodecContext == null)
                throw new FFmpegException(FFmpegException.NotInitCodecContext);
            return ffmpeg.avcodec_send_packet(pCodecContext, packet);
        }

        /// <summary>
        /// receive frame from decoder
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public int ReceiveFrame([Out] MediaFrame frame)
        {
            if (pCodecContext == null)
                throw new FFmpegException(FFmpegException.NotInitCodecContext);
            return ffmpeg.avcodec_receive_frame(pCodecContext, frame);
        }
        #endregion

        /// <summary>
        /// get all decodes
        /// </summary>
        public static IEnumerable<MediaDecoder> Decodes
        {
            get
            {
                IntPtr pCodec;
                IntPtr2Ptr opaque = IntPtr2Ptr.Null;
                while ((pCodec = CodecIterate(opaque)) != IntPtr.Zero)
                {
                    if (CodecIsDecoder(pCodec))
                        yield return new MediaDecoder(pCodec);
                }

            }
        }
    }
}
