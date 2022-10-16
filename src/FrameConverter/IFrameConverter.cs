using System.Collections.Generic;

namespace FFmpegSharp
{
    public interface IFrameConverter
    {
        IEnumerable<MediaFrame> Convert(MediaFrame srcframe, MediaFrame dstframe);


    }

}
