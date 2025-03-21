using System;
using System.Collections;
using System.Collections.Generic;
using FFmpeg.AutoGen.Abstractions;


namespace FFmpegSharp
{

    public static unsafe class MediaDevice
    {
 
        private static InputFormat av_input_audio_device_next_safe(InputFormat format)
        {
            var f = ffmpeg.av_input_audio_device_next(format);
            return f == null ? null : new InputFormat(f);
        }
        private static InputFormat av_input_video_device_next_safe(InputFormat format)
        {
            var f = ffmpeg.av_input_video_device_next(format);
            return f == null ? null : new InputFormat(f);
        }
        private static OutputFormat av_output_audio_device_next_safe(OutputFormat format)
        {
            var f = ffmpeg.av_output_audio_device_next(format);
            return f == null ? null : new OutputFormat(f);
        }
        private static OutputFormat av_output_video_device_next_safe(OutputFormat format)
        {
            var f = ffmpeg.av_output_video_device_next(format);
            return f == null ? null : new OutputFormat(f);
        }

        public static IEnumerable<InputFormat> GetInputAudioDevices()
        {
            InputFormat format = null;
            while ((format = av_input_audio_device_next_safe(format)) != null)
            {
                yield return format;
            }
        }

        public static IEnumerable<InputFormat> GetInputVideoDevices()
        {
            InputFormat format = null;
            while ((format = av_input_video_device_next_safe(format)) != null)
            {
                yield return format;
            }
        }

        public static IEnumerable<OutputFormat> GetOutputAudioDevices()
        {
            OutputFormat format = null;
            while ((format = av_output_audio_device_next_safe(format)) != null)
            {
                yield return format;
            }
        }

        public static IEnumerable<OutputFormat> GetOutputVideoDevices()
        {
            OutputFormat format = null;
            while ((format = av_output_video_device_next_safe(format)) != null)
            {
                yield return format;
            }
        }


        public static void ListDevice(this MediaFormatContext value, Action<AVDeviceInfoList> item)
        {
            AVDeviceInfoList* o = null;
            var count = ffmpeg.avdevice_list_devices(value, &o).ThrowIfError();
            if (count > 0)
                item.Invoke(*o);
            ffmpeg.avdevice_free_list_devices(&o);
        }

        public static void ListInputSources(InputFormat value, Action<AVDeviceInfoList> item, string deviceName = null, MediaDictionary deviceOptions = null)
        {
            AVDeviceInfoList* o = null;
            var count = ffmpeg.avdevice_list_input_sources(value, deviceName, deviceOptions, &o).ThrowIfError();
            if (count > 0)
                item.Invoke(*o);
            ffmpeg.avdevice_free_list_devices(&o);
        }

        public static void ListOutputSinks(OutputFormat value, Action<AVDeviceInfoList> item, string deviceName = null, MediaDictionary deviceOptions = null)
        {
            AVDeviceInfoList* o = null;
            var count = ffmpeg.avdevice_list_output_sinks(value, deviceName, deviceOptions, &o).ThrowIfError();
            if (count > 0)
                item.Invoke(*o);
            ffmpeg.avdevice_free_list_devices(&o);
        }
    }

}
