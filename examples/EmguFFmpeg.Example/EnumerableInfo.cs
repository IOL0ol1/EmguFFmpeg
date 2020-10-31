using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FFmpeg.AutoGen;

namespace EmguFFmpeg.Example
{
    public class EnumerableInfo
    {
        public EnumerableInfo(Action<string> log)
        {
            log?.Invoke("---------------------");
            InFormat.Formats.ForEach(_ =>
            {
                StringBuilder tab = new StringBuilder();
                do
                {
                    log?.Invoke($"{tab}InFormat\tName:{_.Name}\tLongName:{_.LongName}\tMimeType:{_.MimeType}\tExtensions:{_.Extensions}");
                    tab = tab.Append("\t");
                } while (_.Next != null);
            });
            log?.Invoke("---------------------");
            OutFormat.Formats.ForEach(_ =>
            {
                StringBuilder tab = new StringBuilder();
                do
                {
                    log?.Invoke($"{tab}InFormat\tName:{_.Name}\tLongName:{_.LongName}\tMimeType:{_.MimeType}\tExtensions:{_.Extensions}");
                    tab = tab.Append("\t");
                } while (_.Next != null);
            });
            log?.Invoke("---------------------");
            MediaDecoder.Decodes.ForEach(_ =>
            {
                log?.Invoke($"Decode {_} " +
                    $"SupportedHardware:{string.Join(", ", _.SupportedHardware.Select(_1 => ffmpeg.av_hwdevice_get_type_name(_1.device_type)))} " +
                    $"SupportedPixelFmts:{string.Join(",", _.SupportedPixelFmts)} " +
                    $"SupportedFrameRates:{string.Join(", ", _.SupportedFrameRates)} " +
                    $"SupportedSampelFmts:{string.Join(", ", _.SupportedSampelFmts)} " +
                    $"SupportedSampleRates:{string.Join(", ", _.SupportedSampleRates)} " +
                    $"SupportedChannelLayout:{string.Join(", ", _.SupportedChannelLayout)} ");
            });

            log?.Invoke("---------------------");
            MediaEncoder.Encodes.ForEach(_ =>
            {
                log?.Invoke($"Encode {_} " +
                    $"SupportedHardware:{string.Join(", ", _.SupportedHardware)} " +
                    $"SupportedPixelFmts:{string.Join(",", _.SupportedPixelFmts)} " +
                    $"SupportedFrameRates:{string.Join(", ", _.SupportedFrameRates)} " +
                    $"SupportedSampelFmts:{string.Join(", ", _.SupportedSampelFmts)} " +
                    $"SupportedSampleRates:{string.Join(", ", _.SupportedSampleRates)} " +
                    $"SupportedChannelLayout:{string.Join(", ", _.SupportedChannelLayout)} ");
            });
            log?.Invoke("---------------------");
            MediaFilter.Filters.ForEach(_ => log?.Invoke($"Filter {_}"));
        }
    }

    internal static class LinqForEach
    {
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> ts, Action<T> action)
        {
            foreach (var item in ts)
            {
                action?.Invoke(item);
            }
            return ts;
        }
    }
}