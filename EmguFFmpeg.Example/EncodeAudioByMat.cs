using Emgu.CV;

using EmguFFmpeg.EmgucvExtern;

using System;
using System.Runtime.InteropServices;

namespace EmguFFmpeg.Example
{
    public class EncodeAudioByMat
    {
        public EncodeAudioByMat(string output)
        {
            using (MediaWriter writer = new MediaWriter(output))
            {
                writer.AddStream(MediaEncode.CreateAudioEncode(writer.Format, 2, 44100));
                writer.Initialize();

                AudioFrame dstFrame = AudioFrame.CreateFrameByCodec(writer[0].Codec);
                SampleConverter converter = new SampleConverter(dstFrame);

                using (Mat mat = CreateMat(writer[0].Codec.AVCodecContext.channels))
                {
                    long pts = 0;
                    for (int i = 0; i < 1000; i++)
                    {
                        foreach (var item in converter.Convert(mat.ToAudioFrame(dstSampleRate: writer[0].Codec.AVCodecContext.sample_rate)))
                        {
                            pts += item.NbSamples;
                            item.Pts = pts;
                            foreach (var packet in writer[0].WriteFrame(item))
                            {
                                writer.WritePacket(packet);
                            }
                        }
                    }
                }
                writer.FlushMuxer();
            }
        }

        public Mat CreateMat(int channel)
        {
            int nbsample = 360 * 3;
            Mat mat = new Mat(channel, nbsample, Emgu.CV.CvEnum.DepthType.Cv64F, 1);
            double[] data = new double[nbsample];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Math.Sin(Math.PI * 2 / (i + 1));
            }
            for (int i = 0; i < mat.Height; i++)
            {
                Marshal.Copy(data, 0, mat.DataPointer + i * mat.Step, data.Length);
            }
            return mat;
        }
    }
}