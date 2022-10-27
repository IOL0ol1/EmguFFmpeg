using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;
using FFmpegSharp.Internal;

namespace FFmpegSharp
{
    /// <summary>
    /// <see cref="AVInputFormat"/> wapper
    /// </summary>
    public unsafe class InFormat : InFormatBase
    {

        public InFormat(AVInputFormat* iformat) : base(iformat)
        {
            pInputFormat = iformat;
        }

        internal InFormat(IntPtr pAVInputFormat)
            : this((AVInputFormat*)pAVInputFormat)
        { }


        public static InFormat FindFormat(string shortName)
        {
            var f = ffmpeg.av_find_input_format(shortName);
            return f == null ? null : new InFormat(f);
        }

        /// <summary>
        /// get demuxer format by name
        /// </summary>
        /// <param name="name">e.g. mov,mp4 ...</param>
        public static InFormat Get(string name)
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
            throw new FFmpegException(ffmpeg.AVERROR_DEMUXER_NOT_FOUND);
        }

        /// <summary>
        /// get all supported input formats.
        /// </summary>
        public static IEnumerable<InFormat> GetFormats()
        {
            IntPtr iformat;
            IntPtrPtr opaque = new IntPtrPtr();
            while ((iformat = av_demuxer_iterate_safe(opaque)) != IntPtr.Zero)
            {
                yield return new InFormat(iformat);
            }
        }

        private static IntPtr av_demuxer_iterate_safe(IntPtrPtr opaque)
        {
            fixed(void** pp = &opaque.Ptr)
            return (IntPtr)ffmpeg.av_demuxer_iterate(pp);
        }

        public string Name => ((IntPtr)pInputFormat->name).PtrToStringUTF8();
        public string LongName => ((IntPtr)pInputFormat->long_name).PtrToStringUTF8();
        public string Extensions => ((IntPtr)pInputFormat->extensions).PtrToStringUTF8();
        public string MimeType => ((IntPtr)pInputFormat->mime_type).PtrToStringUTF8();
    }
}
