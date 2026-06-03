using System;
using FFmpeg.AutoGen;


namespace FFmpeg.Sharp
{
    public unsafe partial class MediaStream
    {
        /// <summary>
        /// Direct access to the codec parameters of this stream.
        /// </summary>
        public ref AVCodecParameters CodecparRef => ref *pStream->codecpar;

        /// <summary>
        /// Convert to TimeSpan.
        /// </summary>
        /// <remarks>
        /// throw exception when <paramref name="pts"/> &lt; 0.
        /// </remarks>
        /// <param name="pts"></param>
        /// <exception cref="FFmpegException"/>
        /// <returns></returns>
        public TimeSpan ToTimeSpan(long pts)
        {
            return TimeSpan.FromSeconds(pts * ffmpeg.av_q2d(pStream->time_base));
        }

        /// <summary>
        /// Convert to TimeSpan.
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public bool TryToTimeSpan(long pts, out TimeSpan timeSpan)
        {
            timeSpan = TimeSpan.Zero;
            if (pts < 0)
                return false;
            timeSpan = TimeSpan.FromSeconds(pts * ffmpeg.av_q2d(pStream->time_base));
            return true;
        }
    }
}
