using System;
using System.Collections;
using System.Collections.Generic;
using FFmpeg.AutoGen;
using FFmpegSharp.Internal;

namespace FFmpegSharp
{

    public unsafe static class MediaDevice
    {

        static MediaDevice()
        {
            ffmpeg.avdevice_register_all();
        }

        private static InFormat av_input_audio_device_next_safe(InFormat format) => new InFormat(ffmpeg.av_input_audio_device_next(format));
        private static InFormat av_input_video_device_next_safe(InFormat format) => new InFormat(ffmpeg.av_input_video_device_next(format));
        private static OutFormat av_output_audio_device_next_safe(OutFormat format) => new OutFormat(ffmpeg.av_output_audio_device_next(format));
        private static OutFormat av_output_video_device_next_safe(OutFormat format) => new OutFormat(ffmpeg.av_output_video_device_next(format));

        public static IEnumerable<InFormat> GetInputAudioDevices()
        {
            InFormat format = null;
            while ((format = av_input_audio_device_next_safe(format)) != null)
            {
                yield return format;
            }
        }

        public static IEnumerable<InFormat> GetInputVideoDevices()
        {
            InFormat format = null;
            while ((format = av_input_video_device_next_safe(format)) != null)
            {
                yield return format;
            }
        }

        public static IEnumerable<OutFormat> GetOutputAudioDevices()
        {
            OutFormat format = null;
            while ((format = av_output_audio_device_next_safe(format)) != null)
            {
                yield return format;
            }
        }

        public static IEnumerable<OutFormat> GetOutputVideoDevices()
        {
            OutFormat format = null;
            while ((format = av_output_video_device_next_safe(format)) != null)
            {
                yield return format;
            }
        }


        public static MediaDeviceInfoLists ListDevice(this MediaFormatContextBase value)
        {
            AVDeviceInfoList** o = (AVDeviceInfoList**)IntPtr2Ptr.Ptr2Null.Ptr2Ptr;
            var count = ffmpeg.avdevice_list_devices(value, o).ThrowIfError();
            return new MediaDeviceInfoLists(o, count);
        }


        public static MediaDeviceInfoLists ListInputSources(this InFormat value, string deviceName, MediaDictionary deviceOptions)
        {
            AVDeviceInfoList** o = (AVDeviceInfoList**)IntPtr2Ptr.Ptr2Null.Ptr2Ptr;
            var count = ffmpeg.avdevice_list_input_sources(value, deviceName, deviceOptions, o).ThrowIfError();
            return new MediaDeviceInfoLists(o, count);
        }

        public static MediaDeviceInfoLists ListOutputSinks(this OutFormat value, string deviceName, MediaDictionary deviceOptions)
        {
            AVDeviceInfoList** o = (AVDeviceInfoList**)IntPtr2Ptr.Ptr2Null.Ptr2Ptr;
            var count = ffmpeg.avdevice_list_output_sinks(value, deviceName, deviceOptions, o).ThrowIfError();
            return new MediaDeviceInfoLists(o, count);
        }
    }


    public unsafe class MediaDeviceInfoLists : IDisposable, IReadOnlyList<MediaDeviceInfoList>
    {
        protected AVDeviceInfoList** ppDeviceInfoList = null;

        public static implicit operator AVDeviceInfoList**(MediaDeviceInfoLists value)
        {
            return value.ppDeviceInfoList;
        }

        public MediaDeviceInfoLists(AVDeviceInfoList** ppDeviceInfoList, int count = 0)
        {
            this.ppDeviceInfoList = ppDeviceInfoList;
            Count = count;
        }

        public IEnumerator<MediaDeviceInfoList> GetEnumerator()
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

        public int Count { get; private set; }

        public MediaDeviceInfoList this[int index] =>
            index < Count
            ? new MediaDeviceInfoList(ppDeviceInfoList[index])
            : throw new ArgumentOutOfRangeException();

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // nothing
                }
                ffmpeg.avdevice_free_list_devices(ppDeviceInfoList);
                disposedValue = true;
            }
        }

        ~MediaDeviceInfoLists()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


    }


    public unsafe class MediaDeviceInfoList
    {
        public AVDeviceInfoList* pDeviceInfoList = null;

        public MediaDeviceInfoList(AVDeviceInfoList* pDeviceInfoList)
        {
            this.pDeviceInfoList = pDeviceInfoList;
        }

        public IReadOnlyList<MediaDeviceInfo> Devices
        {
            get
            {
                var output = new List<MediaDeviceInfo>();
                for (int i = 0; i < pDeviceInfoList->nb_devices; i++)
                {
                    output.Add(new MediaDeviceInfo(pDeviceInfoList->devices[i]));
                }
                return output;
            }
        }

        public int NbDevice => pDeviceInfoList->nb_devices;

        public int DefaultDevice => pDeviceInfoList->default_device;
    }

    public unsafe class MediaDeviceInfo
    {
        protected AVDeviceInfo* pDeviceInfo = null;

        public MediaDeviceInfo(AVDeviceInfo* pDeviceInfo)
        {
            this.pDeviceInfo = pDeviceInfo;
        }

        public string DeviceName => ((IntPtr)pDeviceInfo->device_name).ToString();
        public string DeviceDescripton => ((IntPtr)pDeviceInfo->device_description).ToString();

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
