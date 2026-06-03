using System.Collections;
using System.Collections.Generic;

namespace FFmpeg.Sharp
{
    public interface IConverter
    {
        IEnumerable<MediaFrame> Convert(MediaFrame srcframe, MediaFrame dstframe);
    }
}
