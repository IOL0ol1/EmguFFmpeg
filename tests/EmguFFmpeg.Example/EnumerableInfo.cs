using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                log?.Invoke($"{tab}InFormat\tName:{_.Name}\tLongName:{_.LongName}\tMimeType:{_.MimeType}\tExtensions:{_.Extensions}");
                tab = tab.Append("\t");
            });
            log?.Invoke("---------------------");
            OutFormat.Formats.ForEach(_ =>
            {
                StringBuilder tab = new StringBuilder();
                log?.Invoke($"{tab}InFormat\tName:{_.Name}\tLongName:{_.LongName}\tMimeType:{_.MimeType}\tExtensions:{_.Extensions}");
                tab = tab.Append("\t");
            });
            log?.Invoke("---------------------");
            MediaDecoder.Decodes.ForEach(_ =>
            {
                log?.Invoke($"Decode {_} " +
                  (_.SupportedHardware.Any() ? $"SupportedHardware:{string.Join(", ", _.SupportedHardware.Select(_1 => ffmpeg.av_hwdevice_get_type_name(_1.device_type)))} " : "") +
                  (_.SupportedPixelFmts.Any() ? $"SupportedPixelFmts:{string.Join(",", _.SupportedPixelFmts)} " : "") +
                  (_.SupportedFrameRates.Any() ? $"SupportedFrameRates:{string.Join(", ", _.SupportedFrameRates)} " : "") +
                  (_.SupportedSampelFmts.Any() ? $"SupportedSampelFmts:{string.Join(", ", _.SupportedSampelFmts)} " : "") +
                  (_.SupportedSampleRates.Any() ? $"SupportedSampleRates:{string.Join(", ", _.SupportedSampleRates)} " : "") +
                  (_.SupportedChannelLayout.Any() ? $"SupportedChannelLayout:{string.Join(", ", _.SupportedChannelLayout)} " : "") 
                  );
            });

            log?.Invoke("---------------------");
            MediaEncoder.Encodes.ForEach(_ =>
            {
                log?.Invoke($"Encode {_} " +
                  (_.SupportedHardware.Any() ? $"SupportedHardware:{string.Join(", ", _.SupportedHardware.Select(_1 => ffmpeg.av_hwdevice_get_type_name(_1.device_type)))} " : "") +
                  (_.SupportedPixelFmts.Any() ? $"SupportedPixelFmts:{string.Join(",", _.SupportedPixelFmts)} " : "") +
                  (_.SupportedFrameRates.Any() ? $"SupportedFrameRates:{string.Join(", ", _.SupportedFrameRates)} " : "") +
                  (_.SupportedSampelFmts.Any() ? $"SupportedSampelFmts:{string.Join(", ", _.SupportedSampelFmts)} " : "") +
                  (_.SupportedSampleRates.Any() ? $"SupportedSampleRates:{string.Join(", ", _.SupportedSampleRates)} " : "") +
                  (_.SupportedChannelLayout.Any() ? $"SupportedChannelLayout:{string.Join(", ", _.SupportedChannelLayout)} " : "") 
                  //(_.SupportedChLayout.Any() ? $"SupportedChLayout:{string.Join(", ", _.SupportedChLayout)} " : "")
                    );


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
