using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;

namespace EmguFFmpeg
{
    public class InFormat : MediaFormat
    {
        protected unsafe AVInputFormat* pInputFormat = null;

        internal unsafe InFormat(AVInputFormat* iformat)
        {
            if (iformat == null) throw new FFmpegException(FFmpegException.NullReference);
            pInputFormat = iformat;
        }

        public InFormat(string name)
        {
            unsafe
            {
                if (!string.IsNullOrEmpty(name))
                {
                    void* ifmtOpaque = null;
                    AVInputFormat* iformat;
                    while ((iformat = ffmpeg.av_demuxer_iterate(&ifmtOpaque)) != null)
                    {
                        InFormat format = new InFormat(iformat);
                        // e.g. format.Name == "mov,mp4,m4a,3gp,3g2,mj2"
                        string[] names = format.Name.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var item in names)
                        {
                            if (item == name.ToLower())
                            {
                                pInputFormat = iformat;
                                return;
                            }
                        }
                    }
                }
                throw new FFmpegException(ffmpeg.AVERROR_DEMUXER_NOT_FOUND);
            }
        }

        public static IReadOnlyList<InFormat> Formats
        {
            get
            {
                unsafe
                {
                    List<InFormat> result = new List<InFormat>();
                    void* ifmtOpaque = null;
                    AVInputFormat* iformat;
                    while ((iformat = ffmpeg.av_demuxer_iterate(&ifmtOpaque)) != null)
                    {
                        result.Add(new InFormat(iformat));
                    }
                    return result;
                }
            }
        }

        public AVInputFormat AVInputFormat { get { unsafe { return *pInputFormat; } } }

        public unsafe static implicit operator AVInputFormat*(InFormat value)
        {
            if (value == null) return null;
            return value.pInputFormat;
        }

        public int RawCodecId { get { unsafe { return pInputFormat->raw_codec_id; } } }
        public InFormat Next { get { unsafe { return pInputFormat->next == null ? null : new InFormat(pInputFormat->next); } } }
        public override int Flags { get { unsafe { return pInputFormat->flags; } } }
        public override string Name { get { unsafe { return ((IntPtr)pInputFormat->name).PtrToStringUTF8(); } } }
        public override string LongName { get { unsafe { return ((IntPtr)pInputFormat->long_name).PtrToStringUTF8(); } } }
        public override string Extensions { get { unsafe { return ((IntPtr)pInputFormat->extensions).PtrToStringUTF8(); } } }
        public override string MimeType { get { unsafe { return ((IntPtr)pInputFormat->mime_type).PtrToStringUTF8(); } } }
    }
}