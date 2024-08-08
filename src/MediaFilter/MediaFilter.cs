using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;

namespace FFmpegSharp
{
    public unsafe partial class MediaFilter 
    {

         
        public MediaFilter(string name) : this(ffmpeg.avfilter_get_by_name(name))
        { }

        public string Name => ((IntPtr)pFilter->name).PtrToStringUTF8();

        public string Description => ((IntPtr)pFilter->description).PtrToStringUTF8();

        /// <summary>
        /// get all supported filter.
        /// </summary>
        public static IEnumerable<MediaFilter> GetGetFilters()
        {
            IntPtr pFilter;
            IntPtrRef opaque = new IntPtrRef();
            while ((pFilter = av_filter_iterate_safe(opaque)) != IntPtr.Zero)
            {
                yield return new MediaFilter(pFilter);
            }
        }

        protected static IntPtr av_filter_iterate_safe(IntPtrRef opaque)
        {
            fixed (void** pp = &opaque.IntPtr)
            {
                return (IntPtr)ffmpeg.av_filter_iterate(pp);
            }
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
