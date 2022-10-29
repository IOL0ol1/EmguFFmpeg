using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;
using FFmpegSharp.Internal;

namespace FFmpegSharp
{
    /// <summary>
    /// <see cref="AVOutputFormat"/> wapper
    /// </summary>
    public unsafe class OutFormat : OutFormatBase
    {

        public OutFormat(AVOutputFormat* oformat)
            : base(oformat)
        {        }

        internal OutFormat(IntPtr pAVOutputFormat)
            : this((AVOutputFormat*)pAVOutputFormat)
        { }

        /// <summary>
        /// get muxer format by name,e.g. "mp4" ".mp4"
        /// </summary>
        /// <param name="name"></param>
        public static OutFormat Get(string name)
        {
            name = name.Trim().TrimStart('.');
            if (!string.IsNullOrEmpty(name))
            {
                foreach (var format in GetFormats())
                {
                    // e.g. format.Name == "mov,mp4,m4a,3gp,3g2,mj2"
                    string[] names = format.Name.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var item in names)
                    {
                        if (string.Compare(item, name, true) == 0)
                        {
                            return format;
                        }
                    }
                }
            }
            throw new FFmpegException(ffmpeg.AVERROR_MUXER_NOT_FOUND);
        }

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
        public static OutFormat GuessFormat(string shortName, string fileName, string mimeType)
        {
            return new OutFormat(ffmpeg.av_guess_format(shortName, fileName, mimeType));
        }

        /// <summary>
        /// get all supported output formats
        /// </summary>
        public static IEnumerable<OutFormat> GetFormats()
        {
            IntPtr oformat;
            IntPtrPtr opaque = new IntPtrPtr();
            while ((oformat = av_muxer_iterate_safe(opaque)) != IntPtr.Zero)
            {
                yield return new OutFormat(oformat);
            }
        }

        #region Safe wapper for IEnumerable

        private static IntPtr av_muxer_iterate_safe(IntPtrPtr ptr)
        {
            fixed (void** pp = &ptr.Ptr)
            {
                return (IntPtr)ffmpeg.av_muxer_iterate(pp);
            }
        }

        #endregion Safe wapper for IEnumerable

        public string Name => ((IntPtr)pOutputFormat->name).PtrToStringUTF8();
        public string LongName => ((IntPtr)pOutputFormat->long_name).PtrToStringUTF8();
        public string Extensions => ((IntPtr)pOutputFormat->extensions).PtrToStringUTF8();
        public string MimeType => ((IntPtr)pOutputFormat->mime_type).PtrToStringUTF8();
    }
}
