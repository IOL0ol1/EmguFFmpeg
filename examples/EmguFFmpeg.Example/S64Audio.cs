// TODO: move this part to extension

using System;
using FFmpeg.AutoGen;

namespace EmguFFmpeg.Example
{
    namespace EmgucvExtern
    {
        internal class S64Audio
        {
            public S64Audio()
            {
                Planar();
                Packet();
            }

            private void Planar()
            {
                AudioFrame dblpPlanarFrame = new AudioFrame(2, 1024, AVSampleFormat.AV_SAMPLE_FMT_S64, 44100);
                var cv64fH1C1 = dblpPlanarFrame.ToMat(); // Cv64F, width 1024, height 1, number of channel 2
                var data = cv64fH1C1.GetData();
                int[] lengths = new int[data.Rank];
                for (int i = 0; i < lengths.Length; i++)
                    lengths[i] = data.GetLength(i);
                var output = Array.CreateInstance(typeof(long), lengths);
                Buffer.BlockCopy(data, 0, output, 0, data.Length * sizeof(long)); // output is long[1,1024,2]
            }

            private void Packet()
            {
                AudioFrame dblpPacketFrame = new AudioFrame(2, 1024, AVSampleFormat.AV_SAMPLE_FMT_S64P, 44100);
                Emgu.CV.Mat cv64fH2C1 = dblpPacketFrame.ToMat(); // Cv64F, width 1024, height 2, number of channel 1
                var data = cv64fH2C1.GetData();
                int[] lengths = new int[data.Rank];
                for (int i = 0; i < lengths.Length; i++)
                    lengths[i] = data.GetLength(i);
                var output = Array.CreateInstance(typeof(long), lengths);
                Buffer.BlockCopy(data, 0, output, 0, data.Length * sizeof(long)); // output is long[2,1024]
            }
        }
    }

    //namespace OpenCvSharpExtern
    //{
    //    using OpenCvSharp;

    //    class S64Audio
    //    {
    //        public S64Audio()
    //        {
    //            Planar();
    //            Packet();
    //        }

    //        private void Planar()
    //        {
    //            AudioFrame dblpPlanarFrame = new AudioFrame(AVSampleFormat.AV_SAMPLE_FMT_S64, 2, 1024, 44100);
    //            var cv64fH1C1 = dblpPlanarFrame.ToMat(); // Cv64F, width 1024, height 1, number of channel 2
    //            cv64fH1C1.GetRectangularArray<Vec2d>(out var data); // Vec*d, * is number of channel

    //            var r0 = data.GetLength(0);
    //            var r1 = data.GetLength(1);
    //            var r2 = data[0, 0].Count(); // number of channel
    //            long[,,] output = new long[r0, r1, r2];
    //            for (int i = 0; i < r0; i++)
    //            {
    //                for (int j = 0; j < r1; j++)
    //                {
    //                    for (int k = 0; k < r2; k++)
    //                    {
    //                        output[i, j, k] = data[i, j].ToInt64Bits(r2)[k];
    //                    }
    //                }
    //            } // output is long[1,1024,2]
    //        }

    //        private void Packet()
    //        {
    //            AudioFrame dblpPacketFrame = new AudioFrame(AVSampleFormat.AV_SAMPLE_FMT_S64P, 2, 1024, 44100);
    //            var cv64fH2C1 = dblpPacketFrame.ToMat(); // Cv64F, width 1024, height 2, number of channel 1
    //            cv64fH2C1.GetRectangularArray<double>(out var data);  // data is double[2,1024]
    //            long[,] output = new long[data.GetLength(0), data.GetLength(1)];
    //            Buffer.BlockCopy(data, 0, output, 0, data.Length * sizeof(long)); // output is long[2,1024]
    //        }

    //    }
    //}
}