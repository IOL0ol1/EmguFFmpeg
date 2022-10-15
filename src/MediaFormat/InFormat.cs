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

        /// <summary>
        /// get demuxer format by name
        /// </summary>
        /// <param name="name">e.g. mov,mp4 ...</param>
        public static InFormat Get(string name)
        {
            name = name.Trim().TrimStart('.');
            if (!string.IsNullOrEmpty(name))
            {
                foreach (var format in Formats)
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
        public static IEnumerable<InFormat> Formats
        {
            get
            {
                IntPtr iformat;
                IntPtr2Ptr ifmtOpaque = IntPtr2Ptr.Ptr2Null;
                while ((iformat = av_demuxer_iterate_safe(ifmtOpaque)) != IntPtr.Zero)
                {
                    yield return new InFormat(iformat);
                }
            }
        }

        private static IntPtr av_demuxer_iterate_safe(IntPtr2Ptr opaque)
        {
            return (IntPtr)ffmpeg.av_demuxer_iterate(opaque);
        }

        public string Name => ((IntPtr)pInputFormat->name).PtrToStringUTF8();
        public string LongName => ((IntPtr)pInputFormat->long_name).PtrToStringUTF8();
        public string Extensions => ((IntPtr)pInputFormat->extensions).PtrToStringUTF8();
        public string MimeType => ((IntPtr)pInputFormat->mime_type).PtrToStringUTF8();
    }
}
