using System;
using FFmpeg.AutoGen;
using FFmpegSharp.Internal;

namespace FFmpegSharp
{
    public unsafe class MediaStream : MediaStreamBase
    {
        public MediaStream(AVStream* stream)
            : base(stream)
        { }

        public AVCodecParameters* Codecpar
        {
            get => pStream->codecpar;
            set => pStream->codecpar = value;
        }

        public AVCodecParameters CodecparRef => *pStream->codecpar;

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
            return TimeSpan.FromSeconds(pts * ffmpeg.av_q2d(TimeBase));
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
            timeSpan = TimeSpan.FromSeconds(pts * ffmpeg.av_q2d(TimeBase));
            return true;
        }
    }
}
