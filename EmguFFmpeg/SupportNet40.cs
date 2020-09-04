

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
#endif
}
