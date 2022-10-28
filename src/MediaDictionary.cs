using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FFmpeg.AutoGen;

namespace FFmpegSharp
{
    public unsafe class MediaDictionary : IDictionary<string, string>, IDisposable
    {
        protected internal AVDictionary* pDictionary = null;

        public MediaDictionary(AVDictionary* ptr, bool isDisposeByOwner = true)
        {
            pDictionary = ptr;
            disposedValue = !isDisposeByOwner;
        }

        public MediaDictionary() { }

        public MediaDictionary(IEnumerable<KeyValuePair<string, string>> dictionary)
            : this()
        {
            foreach (var item in dictionary)
            {
                Add(item, AVDictWriteFlags.MultiKey);
            }
        }

        public string this[string key]
        {
            get
            {
                AVDictionaryEntry* entry;
                if ((entry = (AVDictionaryEntry*)av_dict_get_safe(this, key, IntPtr.Zero, (int)AVDictReadFlags.MatchCase)) != null)
                    return (*entry).GetValue();
                throw new KeyNotFoundException();
            }
            set
            {
                Add(key, value, (int)AVDictWriteFlags.None);
            }
        }

        public IEnumerable<string> Get(string key, AVDictReadFlags flags = 0)
        {
            if (TryGetValues(key, flags, out var _v))
                return _v;
            return new string[0];
        }

        public ICollection<string> Keys => this.Select(_ => _.Key).ToArray();

        public ICollection<string> Values => this.Select(_ => _.Value).ToArray();

        public int Count => ffmpeg.av_dict_count(pDictionary);

        public bool IsReadOnly => false;

        public void Add(string key, string value)
        {
            if (key == null || value == null)
                throw new ArgumentNullException();
            if (ContainsKey(key))
                throw new ArgumentException();
            Add(key, value, AVDictWriteFlags.DontOverwrite);
        }

        public int Add(string key, string value, AVDictWriteFlags flags)
        {
            fixed (AVDictionary** p = &pDictionary)
            {
                return ffmpeg.av_dict_set(p, key, value, (int)flags).ThrowIfError();
            }
        }

        public int Add(KeyValuePair<string, string> item, AVDictWriteFlags flags)
        {
            return Add(item.Key, item.Value, flags);
        }

        public void Add(KeyValuePair<string, string> item)
        {
            Add(item.Key, item.Value);
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return TryGetValue(item.Key, out var value) && value == item.Value;
        }

        public bool ContainsKey(string key)
        {
            return av_dict_get_safe(this, key, IntPtr.Zero, (int)AVDictReadFlags.MatchCase) != IntPtr.Zero;
        }

        public bool ContainsKey(string key, AVDictReadFlags flags)
        {
            return av_dict_get_safe(this, key, IntPtr.Zero, (int)flags) != IntPtr.Zero;
        }

        /// <summary>
        /// Full copy
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < Count) throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            foreach (KeyValuePair<string, string> entry in this)
            {
                if (++arrayIndex > 0)
                {
                    array[arrayIndex - 1] = entry;
                }
            }
        }

        #region IEnumerator

        private static IntPtr av_dict_get_safe(MediaDictionary dict, string key, IntPtr prev, int flags)
        {
            return (IntPtr)ffmpeg.av_dict_get(dict, key, (AVDictionaryEntry*)prev, flags);
        }

        private static string AVDictionaryEntryIntPtrGetValue(IntPtr intPtr)
        {
            return (*(AVDictionaryEntry*)intPtr).GetValue();
        }

        private static KeyValuePair<string, string> GetEntry(IntPtr intPtr)
        {
            AVDictionaryEntry entry = *(AVDictionaryEntry*)intPtr;
            return new KeyValuePair<string, string>(entry.GetKey(), entry.GetValue());
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            IntPtr prev = IntPtr.Zero;
            while ((prev = av_dict_get_safe(this, string.Empty, prev, (int)AVDictReadFlags.IgnoreSuffix)) != IntPtr.Zero)
            {
                yield return GetEntry(prev);
            }
        }

        #endregion IEnumerator

        /// <summary>
        /// remove first match entry, can call multiple times to delete a entry with the same key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(string key)
        {
            return Add(key, null, (int)AVDictWriteFlags.None) == 0;
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            if (TryGetValue(item.Key, out var value) && value == item.Value)
            {
                return Remove(item.Key);
            }
            return false;
        }

        /// <summary>
        /// remove all
        /// </summary>
        public void Clear()
        {
            fixed (AVDictionary** pp = &pDictionary)
            {
                ffmpeg.av_dict_free(pp);
            }
        }

        public bool TryGetValue(string key, out string value)
        {
            AVDictionaryEntry* entry;
            if ((entry = (AVDictionaryEntry*)av_dict_get_safe(this, key, IntPtr.Zero, (int)AVDictReadFlags.MatchCase)) != null)
            {
                value = (*entry).GetValue();
                return true;
            }
            value = default;
            return false;
        }

        public bool TryGetValues(string key, AVDictReadFlags flags, out string[] values)
        {
            var list = new List<string>();
            AVDictionaryEntry* prev = null;
            while ((prev = (AVDictionaryEntry*)av_dict_get_safe(this, key, (IntPtr)prev, (int)flags)) != null)
            {
                list.Add((*prev).GetValue());
            }
            values = list.ToArray();
            return values.Length > 0;
        }

        public MediaDictionary Copy(int flags = 0)
        {
            var output = new MediaDictionary();
            fixed (AVDictionary** p = &output.pDictionary)
            {
                ffmpeg.av_dict_copy(p, pDictionary, flags);
            }
            return output;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


        public static implicit operator AVDictionary*(MediaDictionary value)
        {
            if (value == null) return null;
            return value.pDictionary;
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Clear();
                pDictionary = null;
                disposedValue = true;
            }
        }

        ~MediaDictionary()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


    }

    public unsafe static class AVDictionaryEntryEx
    {
        /// <summary>
        /// convert <see cref="AVDictionaryEntry"/> to <see cref="KeyValuePair{TKey, TValue}"/>
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static KeyValuePair<string, string> GetEntry(this AVDictionaryEntry entry)
        {
            return new KeyValuePair<string, string>(entry.GetKey(), entry.GetValue());
        }

        /// <summary>
        /// get AVDictionaryEntry key
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static string GetKey(this AVDictionaryEntry entry)
        {
            return ((IntPtr)entry.key).PtrToStringUTF8();
        }

        /// <summary>
        /// get AVDictionaryEntry value
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static string GetValue(this AVDictionaryEntry entry)
        {

            return ((IntPtr)entry.value).PtrToStringUTF8();
        }
    }

    [Flags]
    public enum AVDictReadFlags : int
    {
        /// <summary>
        /// case insensitive and exact match.
        /// </summary>
        None = 0,

        /// <summary>
        /// Only get an entry with exact-case key match. Only relevant in av_dict_get().
        /// <para>{"k1","v1"}</para>
        /// <para>//get "k1" is "v1"</para>
        /// <para>//get "K1" is null</para>
        /// </summary>
        MatchCase = ffmpeg.AV_DICT_MATCH_CASE,

        /// <summary>
        /// Return entry in a dictionary whose first part corresponds to the search key,
        /// ignoring the suffix of the found key string. Only relevant in av_dict_get().
        /// <para>{"k1","v1"}</para>
        /// <para>{"k1","v2"}</para>
        /// <para>{"k2","v3"}</para>
        /// <para>get "k" is {"v1","v2","v3"}</para>
        /// </summary>
        IgnoreSuffix = ffmpeg.AV_DICT_IGNORE_SUFFIX,
    }

    [Flags]
    public enum AVDictWriteFlags : int
    {
        /// <summary>
        /// case insensitive and overwrite.
        /// </summary>
        None = 0,

        /// <summary>
        /// Take ownership of a key that's been
        /// allocated with av_malloc() or another memory allocation function.
        /// </summary>
        [Obsolete("Not suppord in managed code", true)]
        DnotStrDupKey = ffmpeg.AV_DICT_DONT_STRDUP_KEY,

        /// <summary>
        /// Take ownership of a value that's been
        /// allocated with av_malloc() or another memory allocation function.
        /// </summary>
        [Obsolete("Not suppord in managed code", true)]
        DontStrDupVal = ffmpeg.AV_DICT_DONT_STRDUP_VAL,

        /// <summary>
        /// Don't overwrite existing key.
        /// </summary>
        DontOverwrite = ffmpeg.AV_DICT_DONT_OVERWRITE,

        /// <summary>
        /// If the key already exists, append to it's value.
        /// </summary>
        Append = ffmpeg.AV_DICT_APPEND,

        /// <summary>
        /// Allow to store several equal keys in the dictionary
        /// </summary>
        MultiKey = ffmpeg.AV_DICT_MULTIKEY,
    }
}
