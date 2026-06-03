using System.Collections.Generic;

namespace FFmpeg.Sharp
{
    /// <summary>
    /// Single-frame converter contract. Implementations transform <c>src</c> into <c>dst</c> in-place and
    /// report the number of frames written (0 if the converter needs more input before it can produce,
    /// 1 for the common 1-in-1-out case).
    /// </summary>
    public interface IConverter
    {
        /// <summary>
        /// Convert <paramref name="src"/> into <paramref name="dst"/>.
        /// </summary>
        /// <param name="src">Source frame.</param>
        /// <param name="dst">Pre-allocated destination frame.</param>
        /// <returns>Number of output frames written: 0 when no output is ready yet, 1 when <paramref name="dst"/> contains valid output.</returns>
        int Convert(MediaFrame src, MediaFrame dst);
    }

    /// <summary>
    /// Many-out-per-in contract. Used by converters that may produce zero or more output frames per input
    /// (e.g. an audio resampler bridging to a fixed encoder frame size).
    /// </summary>
    public interface IBatchConverter
    {
        /// <summary>
        /// Push <paramref name="src"/> into the converter and pull out as many ready frames as possible.
        /// Pass <see langword="null"/> for <paramref name="src"/> to flush.
        /// </summary>
        /// <param name="src">Source frame, or <see langword="null"/> to flush.</param>
        IEnumerable<MediaFrame> Convert(MediaFrame src);
    }
}
