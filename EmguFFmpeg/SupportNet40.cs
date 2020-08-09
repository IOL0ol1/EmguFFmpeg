

namespace EmguFFmpeg
{
#if NET40
    using System.Collections.Generic;
    using System.Collections;

    public interface IReadOnlyCollection<out T> : IEnumerable<T>, IEnumerable
    {
        int Count { get; }
    }


    public interface IReadOnlyList<T> : IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>
    {
        T this[int index] { get; }
    }


    public interface IReadOnlyDictionary<TKey, TValue> : IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
    {

        TValue this[TKey key] { get; }

        IEnumerable<TKey> Keys { get; }

        IEnumerable<TValue> Values { get; }

        bool ContainsKey(TKey key);

        bool TryGetValue(TKey key, out TValue value);
    }
#endif
}
