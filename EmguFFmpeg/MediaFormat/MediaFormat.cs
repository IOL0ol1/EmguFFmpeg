using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;

namespace EmguFFmpeg
{
    public unsafe abstract class MediaFormat
    {
        public abstract int Flags { get; }
        public abstract string Name { get; }
        public abstract string LongName { get; }
        public abstract string Extensions { get; }
        public abstract string MimeType { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}