using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FFmpeg.AutoGen;

namespace EmguFFmpeg.AppTest
{
    public class EncodeVideo : ExampleBase
    {
        public EncodeVideo(string outputFile = "path-to-yout.h264", string codeName = "libx264")
        {
            parames.Add("outputFile", outputFile);
            parames.Add("codeName", codeName);
        }



        public unsafe override void Execute()
        {
            var outputFile = GetParame<string>("outputFile");
            var codeName = GetParame<string>("codeName");

            FileStream os = File.Create(outputFile);
            MediaFrame frame;
            MediaPacket pkt = new MediaPacket();

            var encoder = MediaEncoder.CreateVideoEncoder(codeName, 352, 288, 25, AVPixelFormat.AV_PIX_FMT_YUV420P, 400000, contextSettings: _ =>
            {
                _.GopSize = 10;
                _.MaxBFrames = 1;
                _.PixFmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
                if (_.Context.codec_id == AVCodecID.AV_CODEC_ID_H264)
                    ffmpeg.av_opt_set(((AVCodecContext*)_)->priv_data, "preset", "slow", 0);
            }); 
            frame = MediaFrame.CreateVideoFrame(encoder.Context.Width, encoder.Context.Height, encoder.Context.PixFmt);
            for (int i = 0; i < 25; i++)
            {
                AVFrame* pframe = frame;
                AVCodecContext* c = encoder.Context;
                /* Y */
                for (int y = 0; y < c->height; y++)
                {
                    for (int x = 0; x < c->width; x++)
                    {
                        pframe->data[0][y * pframe->linesize[0] + x] = (byte)(x + y + i * 3);
                    }
                }

                /* Cb and Cr */
                for (int y = 0; y < c->height / 2; y++)
                {
                    for (int x = 0; x < c->width / 2; x++)
                    {
                        pframe->data[1][y * pframe->linesize[1] + x] = (byte)(128 + y + i * 2);
                        pframe->data[2][y * pframe->linesize[2] + x] = (byte)(64 + x + i * 5);
                    }
                }
                pframe->pts = i;
                foreach (var item in encoder.EncodeFrame(frame, pkt))
                {
                    os.Write(new ReadOnlySpan<byte>(item.AVPacket.data, item.AVPacket.size));
                }
            }
            foreach (var item in encoder.EncodeFrame(null, pkt))
            {
                os.Write(new ReadOnlySpan<byte>(item.AVPacket.data, item.AVPacket.size));
            }

            if (encoder.Context.Context.codec_id == AVCodecID.AV_CODEC_ID_MPEG1VIDEO
                || encoder.Context.Context.codec_id == AVCodecID.AV_CODEC_ID_MPEG2VIDEO)
            {
                byte[] endcode = { 0, 0, 1, 0xb7 };
                os.Write(endcode, 0, endcode.Length);
            }

            os.Dispose();
            encoder.Dispose();
            frame.Dispose();
            pkt.Dispose();

        }
    }
}
