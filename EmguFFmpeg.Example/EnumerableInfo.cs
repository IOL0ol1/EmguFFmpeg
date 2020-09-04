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
            InFormat.Formats.ForEach(_ => log?.Invoke(_.ToString()));
            log?.Invoke("---------------------");
            OutFormat.Formats.ForEach(_ => log?.Invoke(_.ToString()));
            log?.Invoke("---------------------");
            MediaDecoder.Decodes.ForEach(_ =>
            {
                log?.Invoke(_.ToString());
            });

            log?.Invoke("---------------------");
            MediaEncoder.Encodes.ForEach(_ => log?.Invoke(_.ToString()));
            log?.Invoke("---------------------");
            MediaFilter.Filters.ForEach(_ => log?.Invoke(_.ToString()));
        }

        
    }

    internal static class LinqForEach
    {
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> ts,Action<T> action)
        {
            foreach (var item in ts)
            {
                action?.Invoke(item);
            }
            return ts;
        }
    }
}
