using System;
using System.Collections.Generic;

namespace FFmpeg.Sharp
{
    /// <summary>
    /// Shared adapters for FFmpeg's native enumeration patterns. Iterator methods (yield) cannot
    /// contain pointer code, so every enumeration used to carry its own "safe step" helper — these
    /// three primitives replace them all. The single unsafe step lives in <see cref="Step"/>.
    /// </summary>
    internal static unsafe class NativeIterate
    {
        public delegate IntPtr CursorFn(void** opaque);

        /// <summary>The <c>av_xxx_iterate(void** opaque)</c> family (codecs, muxers, demuxers, filters, parsers).</summary>
        public static IEnumerable<T> Cursor<T>(CursorFn next, Func<IntPtr, T> wrap)
        {
            IntPtr cursor = default, item;
            while ((item = Step(next, ref cursor)) != IntPtr.Zero)
                yield return wrap(item);
        }

        // The cursor round-trips through a local so its address can be taken without `fixed`
        // (only heap locations need pinning).
        private static IntPtr Step(CursorFn next, ref IntPtr cursor)
        {
            void* p = (void*)cursor;
            var item = next(&p);
            cursor = (IntPtr)p;
            return item;
        }

        /// <summary>The <c>get(i)</c>-until-NULL family (e.g. <c>avcodec_get_hw_config</c>).</summary>
        public static IEnumerable<T> Indexed<T>(Func<int, IntPtr> get, Func<IntPtr, T> wrap)
        {
            IntPtr item;
            for (int i = 0; (item = get(i)) != IntPtr.Zero; i++)
                yield return wrap(item);
        }

        /// <summary>The <c>next(prev)</c>-until-null family (e.g. <c>av_input_audio_device_next</c>).</summary>
        public static IEnumerable<T> Chained<T>(Func<T, T> next) where T : class
        {
            T item = null;
            while ((item = next(item)) != null)
                yield return item;
        }
    }
}
