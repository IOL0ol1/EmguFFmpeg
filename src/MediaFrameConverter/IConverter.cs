using System.Collections;
using System.Collections.Generic;

namespace FFmpegSharp
{
    public interface IConverter
    {
        IEnumerable<MediaFrame> Convert(MediaFrame srcframe, MediaFrame dstframe);
    }
}
