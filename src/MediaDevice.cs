using System;
using System.Collections;
using System.Collections.Generic;
using FFmpeg.AutoGen;


namespace FFmpeg.Sharp
{

    public static unsafe class MediaDevice
    {

        static MediaDevice()
        {
            //ffmpeg.avdevice_register_all();
        }

        public static IEnumerable<MediaInputFormat> GetInputAudioDevices()
            => NativeIterate.Chained<MediaInputFormat>(prev =>
            {
                var f = ffmpeg.av_input_audio_device_next(prev);
                return f == null ? null : new MediaInputFormat(f);
            });

        public static IEnumerable<MediaInputFormat> GetInputVideoDevices()
            => NativeIterate.Chained<MediaInputFormat>(prev =>
            {
                var f = ffmpeg.av_input_video_device_next(prev);
                return f == null ? null : new MediaInputFormat(f);
            });

        public static IEnumerable<MediaOutputFormat> GetOutputAudioDevices()
            => NativeIterate.Chained<MediaOutputFormat>(prev =>
            {
                var f = ffmpeg.av_output_audio_device_next(prev);
                return f == null ? null : new MediaOutputFormat(f);
            });

        public static IEnumerable<MediaOutputFormat> GetOutputVideoDevices()
            => NativeIterate.Chained<MediaOutputFormat>(prev =>
            {
                var f = ffmpeg.av_output_video_device_next(prev);
                return f == null ? null : new MediaOutputFormat(f);
            });

        /// <summary>
        /// List devices autodetected for this format context's device. Wraps <c>avdevice_list_devices</c>.
        /// The returned list is OWNED by the caller — dispose it to free the native list.
        /// </summary>
        public static MediaDeviceInfoList ListDevices(this MediaFormatContext value)
        {
            AVDeviceInfoList* list = null;
            int ret = ffmpeg.avdevice_list_devices(value, &list);
            if (ret < 0)
            {
                if (list != null) ffmpeg.avdevice_free_list_devices(&list);
                ret.ThrowIfError();
            }
            return new MediaDeviceInfoList(list);
        }

        /// <summary>
        /// List autodetected input sources for a device demuxer. Wraps <c>avdevice_list_input_sources</c>.
        /// The returned list is OWNED by the caller — dispose it to free the native list.
        /// </summary>
        public static MediaDeviceInfoList ListInputSources(MediaInputFormat value, string deviceName = null, MediaDictionary deviceOptions = null)
        {
            AVDeviceInfoList* list = null;
            int ret = ffmpeg.avdevice_list_input_sources(value, deviceName, deviceOptions, &list);
            if (ret < 0)
            {
                if (list != null) ffmpeg.avdevice_free_list_devices(&list);
                ret.ThrowIfError();
            }
            return new MediaDeviceInfoList(list);
        }

        /// <summary>
        /// List autodetected output sinks for a device muxer. Wraps <c>avdevice_list_output_sinks</c>.
        /// The returned list is OWNED by the caller — dispose it to free the native list.
        /// </summary>
        public static MediaDeviceInfoList ListOutputSinks(MediaOutputFormat value, string deviceName = null, MediaDictionary deviceOptions = null)
        {
            AVDeviceInfoList* list = null;
            int ret = ffmpeg.avdevice_list_output_sinks(value, deviceName, deviceOptions, &list);
            if (ret < 0)
            {
                if (list != null) ffmpeg.avdevice_free_list_devices(&list);
                ret.ThrowIfError();
            }
            return new MediaDeviceInfoList(list);
        }
    }

    /// <summary>
    /// Owns one <see cref="AVDeviceInfoList"/> (the result of an avdevice_list_* call) and exposes its
    /// devices. Dispose to free the native list — the <see cref="MediaDeviceInfo"/> items borrow from it
    /// and must not be used afterwards.
    /// </summary>
    public unsafe class MediaDeviceInfoList : IDisposable, IReadOnlyList<MediaDeviceInfo>
    {
        protected AVDeviceInfoList* pDeviceInfoList;

        public static implicit operator AVDeviceInfoList*(MediaDeviceInfoList value)
        {
            return value == null ? null : value.pDeviceInfoList;
        }

        public MediaDeviceInfoList(AVDeviceInfoList* pDeviceInfoList)
        {
            this.pDeviceInfoList = pDeviceInfoList;
        }

        public int Count => pDeviceInfoList == null ? 0 : pDeviceInfoList->nb_devices;

        /// <summary>Index of the default device, or -1 if unknown.</summary>
        public int DefaultDevice => pDeviceInfoList == null ? -1 : pDeviceInfoList->default_device;

        public MediaDeviceInfo this[int index] =>
            index >= 0 && index < Count
            ? new MediaDeviceInfo(pDeviceInfoList->devices[index])
            : throw new ArgumentOutOfRangeException(nameof(index));

        public IEnumerator<MediaDeviceInfo> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (pDeviceInfoList != null)
                {
                    fixed (AVDeviceInfoList** pp = &pDeviceInfoList)
                        ffmpeg.avdevice_free_list_devices(pp);
                }
                disposedValue = true;
            }
        }

        ~MediaDeviceInfoList()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Borrowed view of one <see cref="AVDeviceInfo"/> — valid only while the owning
    /// <see cref="MediaDeviceInfoList"/> is alive.
    /// </summary>
    public unsafe class MediaDeviceInfo
    {
        protected AVDeviceInfo* pDeviceInfo = null;

        public MediaDeviceInfo(AVDeviceInfo* pDeviceInfo)
        {
            this.pDeviceInfo = pDeviceInfo;
        }

        public string DeviceName => ((IntPtr)pDeviceInfo->device_name).PtrToStringUTF8();
        public string DeviceDescription => ((IntPtr)pDeviceInfo->device_description).PtrToStringUTF8();

        public IReadOnlyList<AVMediaType> MediaTypes
        {
            get
            {
                var output = new List<AVMediaType>();
                for (int i = 0; i < pDeviceInfo->nb_media_types; i++)
                {
                    output.Add(pDeviceInfo->media_types[i]);
                }
                return output;
            }
        }

        public int NbMediaTypes => pDeviceInfo->nb_media_types;
    }
}
