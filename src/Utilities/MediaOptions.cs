using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    /// <summary>
    /// Generic AVOption helpers over any AVClass object (the libavutil/opt.h layer).
    /// <para>
    /// All methods operate on BORROWED objects — no ownership is taken and nothing is freed.
    /// Wrapper classes have implicit operators to their raw pointers, so they can be passed directly.
    /// </para>
    /// </summary>
    public static unsafe class MediaOptions
    {
        /// <summary>
        /// Get an option value as a string via <see cref="ffmpeg.av_opt_get(void*, string, int, byte**)"/>.
        /// Never throws on option-not-found.
        /// </summary>
        /// <param name="obj">A struct whose first element is a pointer to an AVClass.</param>
        /// <param name="name">The name of the option to get.</param>
        /// <param name="searchFlags">Flags passed to av_opt_find2.</param>
        /// <returns>The option value, or <see langword="null"/> when the call fails (e.g. option not found).</returns>
        public static string Get(void* obj, string name, int searchFlags = ffmpeg.AV_OPT_SEARCH_CHILDREN)
        {
            byte* val = null;
            int ret = ffmpeg.av_opt_get(obj, name, searchFlags, &val);
            if (ret < 0) return null;
            var result = ((IntPtr)val).PtrToStringUTF8();
            ffmpeg.av_freep(&val);
            return result;
        }

        /// <summary>
        /// Set an option from a string via <see cref="ffmpeg.av_opt_set(void*, string, string, int)"/>.
        /// </summary>
        /// <param name="obj">A struct whose first element is a pointer to an AVClass.</param>
        /// <param name="name">The name of the option to set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="searchFlags">Flags passed to av_opt_find2.</param>
        /// <returns>Raw FFmpeg return code (0 on success, negative AVERROR on failure).</returns>
        public static int Set(void* obj, string name, string value, int searchFlags = ffmpeg.AV_OPT_SEARCH_CHILDREN)
            => ffmpeg.av_opt_set(obj, name, value, searchFlags);

        /// <summary>
        /// Set an integer option via <see cref="ffmpeg.av_opt_set_int(void*, string, long, int)"/>.
        /// </summary>
        /// <param name="obj">A struct whose first element is a pointer to an AVClass.</param>
        /// <param name="name">The name of the option to set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="searchFlags">Flags passed to av_opt_find2.</param>
        /// <returns>Raw FFmpeg return code (0 on success, negative AVERROR on failure).</returns>
        public static int SetInt(void* obj, string name, long value, int searchFlags = ffmpeg.AV_OPT_SEARCH_CHILDREN)
            => ffmpeg.av_opt_set_int(obj, name, value, searchFlags);
    }
}
