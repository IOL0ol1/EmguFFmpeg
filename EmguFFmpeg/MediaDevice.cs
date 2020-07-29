using FFmpeg.AutoGen;

using System.Collections.Generic;

namespace EmguFFmpeg
{
    public class MediaDevice
    {
        public bool IsDefaultDevice { get; private set; }
        public string DeviceName { get; private set; }
        public string DeviceDescription { get; private set; }

        private MediaDevice()
        { }

        public static void InitializeDevice()
        {
            ffmpeg.avdevice_register_all();
        }

        public static MediaDictionary ListDevicesOptions
        {
            get
            {
                MediaDictionary dict = new MediaDictionary();
                dict.Add("list_devices", "true");
                return dict;
            }
        }

        public static IReadOnlyList<IReadOnlyList<MediaDevice>> GetDeviceInfos(MediaFormat device, MediaDictionary options = null)
        {
            unsafe
            {
                string parame = "list";
                AVFormatContext* pFmtCtx = ffmpeg.avformat_alloc_context();
                ffmpeg.av_log(null, (int)LogLevel.Verbose, "--------------------------");
                if (device is InFormat iformat)
                    ffmpeg.avformat_open_input(&pFmtCtx, parame, iformat, options);
                else if (device is OutFormat oformat)
                    ffmpeg.avformat_alloc_output_context2(&pFmtCtx, oformat, null, parame);
                ffmpeg.av_log(null, (int)LogLevel.Verbose, "--------------------------");
                ffmpeg.avformat_free_context(pFmtCtx);
                return new List<List<MediaDevice>>();
            }
        }

        /* ffmpeg not implemented
        private static List<List<MediaDevice>> CopyAndFree(AVDeviceInfoList** ppDeviceInfoList, int deviceInfoListLength)
        {
            unsafe
            {
                List<List<MediaDevice>> result = new List<List<MediaDevice>>();
                if (deviceInfoListLength > 0 && ppDeviceInfoList != null)
                {
                    for (int i = 0; i < deviceInfoListLength; i++)
                    {
                        List<MediaDevice> infos = new List<MediaDevice>();
                        for (int j = 0; j < ppDeviceInfoList[i]->nb_devices; j++)
                        {
                            AVDeviceInfo* deviceInfo = ppDeviceInfoList[i]->devices[j];
                            MediaDevice info = new MediaDevice()
                            {
                                DeviceName = System.Runtime.InteropServices.Marshal.PtrToStringAnsi((System.IntPtr)deviceInfo->device_name),
                                DeviceDescription = System.Runtime.InteropServices.Marshal.PtrToStringAnsi((System.IntPtr)deviceInfo->device_description),
                                IsDefaultDevice = j == ppDeviceInfoList[i]->default_device,
                            };
                            infos.Add(info);
                        }
                        result.Add(infos);
                    }
                    ffmpeg.avdevice_free_list_devices(ppDeviceInfoList);
                }
                return result;
            }
        }

        public static IReadOnlyList<IReadOnlyList<MediaDevice>> GetDeviceInfos(MediaFormat device, MediaDictionary options = null)
        {
            unsafe
            {
                int deviceInfoListLength = 0;
                AVDeviceInfoList* pDeviceInfoList = null;
                if (device is InFormat iformat)
                    deviceInfoListLength = ffmpeg.avdevice_list_input_sources(iformat, null, options, &pDeviceInfoList);
                else if (device is OutFormat oformat)
                    deviceInfoListLength = ffmpeg.avdevice_list_output_sinks(oformat, null, options, &pDeviceInfoList);
                deviceInfoListLength.ThrowExceptionIfError();
                return CopyAndFree(&pDeviceInfoList, deviceInfoListLength);
            }
        }

        public static IReadOnlyList<IReadOnlyList<MediaDevice>> GetOutputDeviceInfos(string deviceName, MediaDictionary options = null)
        {
            unsafe
            {
                AVDeviceInfoList* pDeviceInfoList = null;
                int deviceInfoListLength = ffmpeg.avdevice_list_output_sinks(null, deviceName, options, &pDeviceInfoList);
                return CopyAndFree(&pDeviceInfoList, deviceInfoListLength);
            }
        }
        public static IReadOnlyList<IReadOnlyList<MediaDevice>> GetInputDeviceInfos(string deviceName, MediaDictionary options = null)
        {
            unsafe
            {
                AVDeviceInfoList* pDeviceInfoList = null;
                int deviceInfoListLength = ffmpeg.avdevice_list_input_sources(null, deviceName, options, &pDeviceInfoList);
                return CopyAndFree(&pDeviceInfoList, deviceInfoListLength);
            }
        }
        */
    }
}