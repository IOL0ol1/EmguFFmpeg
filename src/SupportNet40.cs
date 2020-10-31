namespace EmguFFmpeg
{
#if NET40
    using System.Collections.Generic;
    using System.Collections;

    /// <summary>
    /// IReadOnlyCollection interface for net40.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IReadOnlyCollection<out T> : IEnumerable<T>, IEnumerable
    {
        /// <summary>
        /// get count.
        /// </summary>
        int Count { get; }
    }

    /// <summary>
    /// IReadOnlyList interface for net40.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IReadOnlyList<T> : IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>
    {
        /// <summary>
        /// get element by index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        T this[int index] { get; }
    }
#endif
}
