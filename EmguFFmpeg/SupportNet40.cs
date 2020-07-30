
using System.Collections.Generic;

namespace EmguFFmpeg
{
#if NET40
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

    /// <summary>
    /// for support net formework 4.0
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class FFList<T> : List<T>, IReadOnlyList<T>
    {
        public FFList() : base() { }
        public FFList(int capacity) : base(capacity) { }
        public FFList(IEnumerable<T> collection) : base(collection) { }
    }

}
