using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace FFmpegSharp
{
    /// <summary>
    /// <see cref="AVOutputFormat"/> wapper
    /// </summary>
    public unsafe partial class OutputFormat
    {

        /// <summary>
        /// Return the output format in the list of registered output formats which best matches the
        /// provided parameters, or return NULL if there is no match.
        /// </summary>
        /// <param name="shortName">
        /// if non-NULL checks if short_name matches with the names of the registered formats
        /// </param>
        /// <param name="fileName">
        /// if non-NULL checks if filename terminates with the extensions of the registered formats
        /// </param>
        /// <param name="mimeType">
        /// if non-NULL checks if mime_type matches with the MIME type of the registered formats
        /// </param>
        /// <returns></returns>
        public static OutputFormat GuessFormat(string shortName, string fileName, string mimeType)
        {
            return new OutputFormat(ffmpeg.av_guess_format(shortName, fileName, mimeType));
        }

        /// <summary>
        /// Return the output format in the list of registered output formats which best matches the
        /// provided parameters, or return NULL if there is no match.
        /// </summary>
        /// <param name="name">if non-NULL checks if name matches with the names of the registered formats
        /// or name terminates with the extensions of the registered formats
        /// or name matches with the MIME type of the registered formats
        /// </param>
        /// <returns></returns>
        public static OutputFormat GuessFormat(string name)
        {
            if (name != null)
            {
                var pFormat = ffmpeg.av_guess_format(name, null, null);
                if (pFormat != null) return new OutputFormat(pFormat);
                pFormat = ffmpeg.av_guess_format(null, name, null);
                if (pFormat != null) return new OutputFormat(pFormat);
                pFormat = ffmpeg.av_guess_format(null, null, name);
                if (pFormat != null) return new OutputFormat(pFormat);
            }
            return null;
        }

        /// <summary>
        /// Iterate over all registered muxers.
        /// </summary>
        public static IEnumerable<OutputFormat> GetFormats()
        {
            IntPtr oformat;
            IntPtrRef opaque = new IntPtrRef();
            while ((oformat = av_muxer_iterate_safe(opaque)) != IntPtr.Zero)
            {
                yield return new OutputFormat(oformat);
            }
        }

        protected static IntPtr av_muxer_iterate_safe(IntPtrRef ptr)
        {
            fixed (void** pp = &ptr.IntPtr)
            {
                return (IntPtr)ffmpeg.av_muxer_iterate(pp);
            }
        }

        public string Name => ((IntPtr)pOutputFormat->name).PtrToStringUTF8();
        /// <summary>
        /// Descriptive name for the format, meant to be more human-readable than name. You
        ///     should use the NULL_IF_CONFIG_SMALL() macro to define it.
        /// </summary>
        public string LongName => ((IntPtr)pOutputFormat->long_name).PtrToStringUTF8();

        /// <summary>
        /// comma-separated filename extensions
        /// </summary>
        public string Extensions => ((IntPtr)pOutputFormat->extensions).PtrToStringUTF8();
        public string MimeType => ((IntPtr)pOutputFormat->mime_type).PtrToStringUTF8();
    }
}
