using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;

namespace EmguFFmpeg
{
    public unsafe class OutFormat : MediaFormat
    {
        protected AVOutputFormat* pOutputFormat = null;

        internal OutFormat(AVOutputFormat* oformat)
        {
            if (oformat == null) throw new FFmpegException(FFmpegException.NullReference);
            pOutputFormat = oformat;
        }

        public OutFormat(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                void* ofmtOpaque = null;
                AVOutputFormat* oformat;
                while ((oformat = ffmpeg.av_muxer_iterate(&ofmtOpaque)) != null)
                {
                    OutFormat format = new OutFormat(oformat);
                    // e.g. format.Name == "mov,mp4,m4a,3gp,3g2,mj2"
                    string[] names = format.Name.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var item in names)
                    {
                        if (item == name.ToLower())
                        {
                            pOutputFormat = oformat;
                            return;
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

        public static IReadOnlyList<OutFormat> Formats
        {
            get
            {
                List<OutFormat> result = new List<OutFormat>();
                void* ofmtOpaque = null;
                AVOutputFormat* oformat;
                while ((oformat = ffmpeg.av_muxer_iterate(&ofmtOpaque)) != null)
                {
                    result.Add(new OutFormat(oformat));
                }
                return result;
            }
        }

        public AVOutputFormat AVOutputFormat => *pOutputFormat;

        public static implicit operator AVOutputFormat*(OutFormat value)
        {
            if (value == null) return null;
            return value.pOutputFormat;
        }

        public AVCodecID VideoCodec => pOutputFormat->video_codec;
        public AVCodecID AudioCodec => pOutputFormat->audio_codec;
        public AVCodecID DataCodec => pOutputFormat->data_codec;
        public AVCodecID SubtitleCodec => pOutputFormat->subtitle_codec;
        public OutFormat Next => pOutputFormat->next == null ? null : new OutFormat(pOutputFormat->next);
        public override int Flags => pOutputFormat->flags;
        public override string Name => ((IntPtr)pOutputFormat->name).PtrToStringUTF8();
        public override string LongName => ((IntPtr)pOutputFormat->long_name).PtrToStringUTF8();
        public override string Extensions => ((IntPtr)pOutputFormat->extensions).PtrToStringUTF8();
        public override string MimeType => ((IntPtr)pOutputFormat->mime_type).PtrToStringUTF8();
    }
}