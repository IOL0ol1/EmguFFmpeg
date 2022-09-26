using System;

using FFmpeg.AutoGen;

namespace EmguFFmpeg
{

    public static class AVRationalEx
    {
        public static AVRational ToInvert(this AVRational rational)
        {
            return ffmpeg.av_inv_q(rational);
        }

        /// <summary>
        /// Convert a double precision floating point number to a rational.
        /// </summary>
        /// <param name="value">`double` to convert</param>
        /// <param name="max">Maximum allowed numerator and denominator</param>
        /// <returns></returns>
        public static AVRational ToRational(this double value,int max = 100000)
        {
            return ffmpeg.av_d2q(value, max);
        }

        public static double ToDouble(this AVRational rational)
        {
            return ffmpeg.av_q2d(rational);
        }
    }

}
