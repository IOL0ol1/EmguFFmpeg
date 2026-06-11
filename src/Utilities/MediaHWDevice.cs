using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    /// <summary>
    /// Hardware device type enumeration helpers (the libavutil/hwcontext.h layer).
    /// Stateful HW plumbing (device/frames contexts) lives on <see cref="MediaCodecContext"/>.
    /// </summary>
    public static class MediaHWDevice
    {
        /// <summary>
        /// Look up an <see cref="AVHWDeviceType"/> by name via <see cref="ffmpeg.av_hwdevice_find_type_by_name(string)"/>.
        /// Returns <see cref="AVHWDeviceType.AV_HWDEVICE_TYPE_NONE"/> when the name is unknown and never throws —
        /// unlike <see cref="MediaCodecContext.InitHWDeviceContext(string, string, MediaDictionary, int, bool)"/>,
        /// which throws on an unknown type name.
        /// </summary>
        /// <param name="name">The device type name, e.g. "cuda", "vaapi", "d3d11va".</param>
        public static AVHWDeviceType FindTypeByName(string name)
            => ffmpeg.av_hwdevice_find_type_by_name(name);

        /// <summary>
        /// Get all supported hardware device types via <see cref="ffmpeg.av_hwdevice_iterate_types(AVHWDeviceType)"/>.
        /// </summary>
        public static IEnumerable<AVHWDeviceType> GetTypes()
        {
            var type = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE;
            while ((type = ffmpeg.av_hwdevice_iterate_types(type)) != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
            {
                yield return type;
            }
        }
    }
}
