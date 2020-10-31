using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    public unsafe class MediaFilter
    {
        protected AVFilter* pFilter;

        internal MediaFilter(AVFilter* filter)
            : this((IntPtr)filter) { }

        /// <summary>
        /// <see cref="AVFilter"/> adapter.
        /// </summary>
        /// <param name="pAVFilter"></param>
        public MediaFilter(IntPtr pAVFilter)
        {
            if (pAVFilter == IntPtr.Zero)
                throw new FFmpegException(FFmpegException.NullReference);
            pFilter = (AVFilter*)pAVFilter;
        }

        public MediaFilter(string name)
        {
            if ((pFilter = ffmpeg.avfilter_get_by_name(name)) == null)
                throw new FFmpegException(ffmpeg.AVERROR_FILTER_NOT_FOUND);
        }

        public AVFilter AVFilter => *pFilter;

        public string Name => ((IntPtr)pFilter->name).PtrToStringUTF8();

        public string Description => ((IntPtr)pFilter->description).PtrToStringUTF8();

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// get all supported filter.
        /// </summary>
        public static IEnumerable<MediaFilter> Filters
        {
            get
            {
                IntPtr pFilter;
                IntPtr2Ptr opaque = IntPtr2Ptr.Null;
                while ((pFilter = FilterIterate(opaque)) != IntPtr.Zero)
                {
                    yield return new MediaFilter(pFilter);
                }
            }
        }

        private static IntPtr FilterIterate(IntPtr2Ptr opaque)
        {
            return (IntPtr)ffmpeg.av_filter_iterate(opaque);
        }

        public static implicit operator AVFilter*(MediaFilter value)
        {
            if (value == null) return null;
            return value.pFilter;
        }

        public override bool Equals(object obj)
        {
            if (obj is MediaFilter filter)
                return filter.pFilter == pFilter;
            return false;
        }

        public override int GetHashCode()
        {
            return ((IntPtr)pFilter).ToInt32();
        }

        public static class VideoSources
        {
            public const string Buffer = "buffer";
            public const string Cellauto = "cellauto";
            public const string Coreimagesrc = "coreimagesrc";
            public const string Mandelbrot = "mandelbrot";
            public const string Mptestsrc = "mptestsrc";
            public const string Frei0r_src = "frei0r_src";
            public const string Life = "life";
            public const string Allrgb = "allrgb";
            public const string Allyuv = "allyuv";
            public const string Color = "color";
            public const string Haldclutsrc = "haldclutsrc";
            public const string Nullsrc = "nullsrc";
            public const string Pal75bars = "pal75bars";
            public const string Pal100bars = "pal100bars";
            public const string Rgbtestsrc = "rgbtestsrc";
            public const string Smptebars = "smptebars";
            public const string Smptehdbars = "smptehdbars";
            public const string Testsrc = "testsrc";
            public const string Testsrc2 = "testsrc2";
            public const string Yuvtestsrc = "yuvtestsrc";
            public const string Openclsrc = "openclsrc";
            public const string Sierpinski = "sierpinski";
        }

        public static class VideoSinks
        {
            public const string Buffersink = "buffersink";
            public const string Nullsink = "nullsink";
        }

        public static class AudioSources
        {
            public const string Abuffer = "abuffer";
            public const string Aevalsrc = "aevalsrc";
            public const string Anullsrc = "anullsrc";
            public const string Flite = "flite";
            public const string Anoisesrc = "anoisesrc";
            public const string Hilbert = "hilbert";
            public const string Sinc = "sinc";
            public const string Sine = "sine";
        }

        public static class AudioSinks
        {
            public const string Abuffersink = "abuffersink";
            public const string Anullsink = "anullsink";
        }
    }
}
