using System;
using System.Collections;
using System.Collections.Generic;
using FFmpeg.AutoGen.Abstractions;


namespace FFmpegSharp
{

    public static unsafe class MediaDevice
    {
 
        private static MediaInputFormat av_input_audio_device_next_safe(MediaInputFormat format)
        {
            var f = ffmpeg.av_input_audio_device_next(format);
            return f == null ? null : new MediaInputFormat(f);
        }
        private static MediaInputFormat av_input_video_device_next_safe(MediaInputFormat format)
        {
            var f = ffmpeg.av_input_video_device_next(format);
            return f == null ? null : new MediaInputFormat(f);
        }
        private static MediaOutputFormat av_output_audio_device_next_safe(MediaOutputFormat format)
        {
            var f = ffmpeg.av_output_audio_device_next(format);
            return f == null ? null : new MediaOutputFormat(f);
        }
        private static MediaOutputFormat av_output_video_device_next_safe(MediaOutputFormat format)
        {
            var f = ffmpeg.av_output_video_device_next(format);
            return f == null ? null : new MediaOutputFormat(f);
        }

        public static IEnumerable<MediaInputFormat> GetInputAudioDevices()
        {
            MediaInputFormat format = null;
            while ((format = av_input_audio_device_next_safe(format)) != null)
            {
                yield return format;
            }
        }

        public static IEnumerable<MediaInputFormat> GetInputVideoDevices()
        {
            MediaInputFormat format = null;
            while ((format = av_input_video_device_next_safe(format)) != null)
            {
                yield return format;
            }
        }

        public static IEnumerable<MediaOutputFormat> GetOutputAudioDevices()
        {
            MediaOutputFormat format = null;
            while ((format = av_output_audio_device_next_safe(format)) != null)
            {
                yield return format;
            }
        }

        public static IEnumerable<MediaOutputFormat> GetOutputVideoDevices()
        {
            MediaOutputFormat format = null;
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

        public static void ListInputSources(MediaInputFormat value, Action<AVDeviceInfoList> item, string deviceName = null, MediaDictionary deviceOptions = null)
        {
            AVDeviceInfoList* o = null;
            var count = ffmpeg.avdevice_list_input_sources(value, deviceName, deviceOptions, &o).ThrowIfError();
            if (count > 0)
                item.Invoke(*o);
            ffmpeg.avdevice_free_list_devices(&o);
        }

        public static void ListOutputSinks(MediaOutputFormat value, Action<AVDeviceInfoList> item, string deviceName = null, MediaDictionary deviceOptions = null)
        {
            AVDeviceInfoList* o = null;
            var count = ffmpeg.avdevice_list_output_sinks(value, deviceName, deviceOptions, &o).ThrowIfError();
            if (count > 0)
                item.Invoke(*o);
            ffmpeg.avdevice_free_list_devices(&o);
        }
    }

}
