using System;
using System.IO;
using FFmpeg.AutoGen;

namespace FFmpegSharp.Example
{
    public class EncodeVideo : ExampleBase
    {
        public EncodeVideo() : this($"EncodeVideo-output.h264", "libx264")
        { }

        public EncodeVideo(params string[] args) : base(args)
        {
            Index = 11;
        }

        public unsafe override void Execute()
        {
            var outputFile = args[0];
            var codeName = args[1];

            /* resolution must be a multiple of two */
            var width = 352;
            var height = 288;
            var fps = 25; // used to set time_base and framerate
            var pixelFormat = AVPixelFormat.AV_PIX_FMT_YUV420P;
            var bitrate = 400000;
            using (FileStream os = File.Create(outputFile))
            using (MediaFrame frame = MediaFrame.CreateVideoFrame(width, height, pixelFormat))
            using (MediaPacket pkt = new MediaPacket())
            using (MediaEncoder encoder = MediaEncoder.CreateVideoEncoder(codeName, width, height, fps, pixelFormat, bitrate, otherSettings: _ =>
            {
                /* emit one intra frame every ten frames
                 * check frame pict_type before passing frame
                 * to encoder, if frame->pict_type is AV_PICTURE_TYPE_I
                 * then gop_size is ignored and the output of encoder
                 * will always be I frame irrespective to gop_size
                 */
                _.GopSize = 10;
                _.MaxBFrames = 1;
                if (_.CodecId == AVCodecID.AV_CODEC_ID_H264)
                    ffmpeg.av_opt_set(((AVCodecContext*)_)->priv_data, "preset", "slow", 0);
            }))
            {
                for (int i = 0; i < 25; i++)
                {
                    /* Make sure the frame data is writable.
                      On the first round, the frame is fresh from av_frame_get_buffer()
                      and therefore we know it is writable.
                      But on the next rounds, encode() will have called
                      avcodec_send_frame(), and the codec may have kept a reference to
                      the frame in its internal structures, that makes the frame
                      unwritable.
                      av_frame_make_writable() checks that and allocates a new buffer
                      for the frame only if necessary.
                      NOTE:FFmpegSharp do it in encoder.EncodeFrame finished
                    */
                    FillYuv420P(frame, i);
                    frame.Pts = i;
                    /* encode the image */
                    foreach (var item in encoder.EncodeFrame(frame, pkt))
                    {
                        os.Write(new ReadOnlySpan<byte>(item.Ref.data, item.Ref.size));
                    }
                }
                /* flush the encoder */
                foreach (var item in encoder.EncodeFrame(null, pkt))
                {
                    os.Write(new ReadOnlySpan<byte>(item.Ref.data, item.Ref.size));
                }
                /* Add sequence end code to have a real MPEG file.
                  It makes only sense because this tiny examples writes packets
                  directly. This is called "elementary stream" and only works for some
                  codecs. To create a valid file, you usually need to write packets
                  into a proper file format or protocol; see muxing.c.
                */
                if (encoder.Ref.codec_id == AVCodecID.AV_CODEC_ID_MPEG1VIDEO
                    || encoder.Ref.codec_id == AVCodecID.AV_CODEC_ID_MPEG2VIDEO)
                {
                    byte[] endcode = { 0, 0, 1, 0xb7 };
                    os.Write(endcode, 0, endcode.Length);
                }
            }
        }

        /// <summary>
        /// Fill frame
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="i"></param>
        private static unsafe void FillYuv420P(MediaFrame frame, int i)
        {
            var data = frame.Data;
            var linesize = frame.Linesize;
            /* Prepare a dummy image.
              In real code, this is where you would have your own logic for
              filling the frame. FFmpeg does not care what you put in the
              frame.
            */
            /* Y */
            for (int y = 0; y < frame.Height; y++)
            {
                for (int x = 0; x < frame.Width; x++)
                {
                    data[0][y * linesize[0] + x] = (byte)(x + y + i * 3);
                }
            }

            /* Cb and Cr */
            for (int y = 0; y < frame.Height / 2; y++)
            {
                for (int x = 0; x < frame.Width / 2; x++)
                {
                    data[1][y * linesize[1] + x] = (byte)(128 + y + i * 2);
                    data[2][y * linesize[2] + x] = (byte)(64 + x + i * 5);
                }
            }
        }
    }
}
