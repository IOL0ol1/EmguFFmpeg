using System.Runtime.CompilerServices;
using FFmpeg.AutoGen;

namespace EmguFFmpeg
{

    public class MediaRational
    {

        private AVRational rational;

        public static MediaRational FromFPS(int fps)
        {
            return new MediaRational(1, fps);
        }

        public MediaRational() { }

        public MediaRational(int num, int den)
        {
            rational.num = num;
            rational.den = den;
        }

        public int Num
        {
            get => rational.num;
            set => rational.num = value;
        }

        public int Den
        {
            get => rational.den;
            set => rational.den = value;
        }



        public static implicit operator AVRational(MediaRational value)
        {
            return value.rational;
        }

        public static implicit operator MediaRational(AVRational value)
        {
            return new MediaRational { rational = value };
        }
    }


    public static class AVRationalEx
    {
        /// <summary>
        /// Convert an <see cref="AVRational"/> to a double use <see cref="ffmpeg.av_q2d(AVRational)"/>.
        /// <para>
        /// NOTE: this will lose precision !!
        /// </para>
        /// </summary>
        /// <param name="rational"></param>
        /// <returns></returns>
        public static double ToDouble(this AVRational rational)
        {
            return ffmpeg.av_q2d(rational);
        }

        /// <summary>
        /// Invert a <see cref="AVRational"/> use <see cref="ffmpeg.av_inv_q(AVRational)"/>
        /// </summary>
        /// <param name="rational"></param>
        /// <returns></returns>
        public static AVRational ToInvert(this AVRational rational)
        {
            return ffmpeg.av_inv_q(rational);
        }

    }

}
