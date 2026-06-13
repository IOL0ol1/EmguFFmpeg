using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    /// <summary>
    /// <see cref="AVInputFormat"/> wapper
    /// </summary>
    public unsafe partial class MediaInputFormat
    {
        /// <summary>
        /// Find AVInputFormat based on the short name of the input format.
        /// </summary>
        /// <param name="shortName"></param>
        /// <returns></returns>
        public static MediaInputFormat FindFormat(string shortName)
        {
            var f = ffmpeg.av_find_input_format(shortName);
            return f == null ? null : new MediaInputFormat(f);
        }

        /// <summary>
        /// Iterate over all registered demuxers.
        /// </summary>
        public static IEnumerable<MediaInputFormat> GetFormats()
            => NativeIterate.Cursor(o => (IntPtr)ffmpeg.av_demuxer_iterate(o), p => new MediaInputFormat(p));

        /// <summary>
        /// A comma separated list of short names for the format. New names may be appended
        ///     with a minor bump.
        /// </summary>
        public string Name => ((IntPtr)pInputFormat->name).PtrToStringUTF8();

        /// <summary>
        /// Descriptive name for the format, meant to be more human-readable than name. You
        ///     should use the NULL_IF_CONFIG_SMALL() macro to define it.
        /// </summary>
        public string LongName => ((IntPtr)pInputFormat->long_name).PtrToStringUTF8();

        /// <summary>
        /// If extensions are defined, then no probe is done. You should usually not use
        ///     extension format guessing because it is not reliable enough
        /// </summary>
        public string Extensions => ((IntPtr)pInputFormat->extensions).PtrToStringUTF8();

        /// <summary>
        /// Comma-separated list of mime types. It is used check for matching mime types
        ///     while probing.
        /// </summary>
        public string MimeType => ((IntPtr)pInputFormat->mime_type).PtrToStringUTF8();
    }
}
