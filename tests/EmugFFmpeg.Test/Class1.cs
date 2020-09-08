using EmguFFmpeg;

using FFmpeg.AutoGen;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;

namespace EmguFFmpeg2
{
    public unsafe class MediaDictionary : IDictionary<string, string>, IDisposable
    {
        public const DictFlags DefaultFlags = DictFlags.MatchCase | DictFlags.DontOverwrite;

        /// <summary>
        /// NOTE: ffmpeg maybe change the value of *<see cref="pDictionary"/>
        /// </summary>
        private AVDictionary* pDictionary = null;

        public MediaDictionary(IntPtr pAVDictionary)
        {
            pDictionary = (AVDictionary*)pAVDictionary;
        }

        internal MediaDictionary(AVDictionary* ptr)
        {
            pDictionary = ptr;
        }

        public string this[string key]
        {
            get
            {
                AVDictionaryEntry* entry;
                if ((entry = (AVDictionaryEntry*)DictGet(this, key, IntPtr.Zero, DictFlags.MatchCase)) != null)
                    return ((IntPtr)entry->value).PtrToStringUTF8();
                throw new KeyNotFoundException();
            }
            set
            {
                Add(key, value, DictFlags.None);
            }
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
            Add(key, value, DictFlags.DontOverwrite);
        }

        public int Add(string key, string value, DictFlags flags)
        {
            fixed (AVDictionary** pp = &pDictionary)
            {
                return ffmpeg.av_dict_set(pp, key, value, (int)flags).ThrowIfError();
            }
        }

        public void Add(KeyValuePair<string, string> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            fixed (AVDictionary** pp = &pDictionary)
            {
                ffmpeg.av_dict_free(pp);
            }
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return TryGetValue(item.Key, out var value) && value == item.Value;
        }

        public bool ContainsKey(string key)
        {
            return DictGet(this, key, IntPtr.Zero, DictFlags.MatchCase) != IntPtr.Zero;
        }

        public bool ContainsKey(string key, DictFlags flags)
        {
            return DictGet(this, key, IntPtr.Zero, flags) != IntPtr.Zero;
        }


        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }


        #region IEnumerator
        private static IntPtr DictGet(MediaDictionary dict, string key, IntPtr prev, DictFlags flags)
        {
            return (IntPtr)ffmpeg.av_dict_get(dict.pDictionary, key, (AVDictionaryEntry*)prev, (int)flags);
        }

        private static KeyValuePair<string, string> GetEntry(IntPtr prev)
        {
            var entry = (AVDictionaryEntry*)prev;
            return new KeyValuePair<string, string>(
                ((IntPtr)entry->key).PtrToStringUTF8(),
                ((IntPtr)entry->value).PtrToStringUTF8());
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (var item in GetEnumerable(string.Empty, DictFlags.IgnoreSuffix))
            {
                yield return item;
            }
        }

        public IEnumerable<KeyValuePair<string, string>> GetEnumerable(string key, DictFlags flags)
        {
            IntPtr prev = IntPtr.Zero;
            while ((prev = DictGet(this, key, prev, flags)) != IntPtr.Zero)
            {
                yield return GetEntry(prev);
            }
        }
        #endregion

        public bool Remove(string key)
        {
            try
            {
                Add(key, null, DictFlags.None);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            if (TryGetValue(item.Key, out var value) && value == item.Value)
            {
                return Remove(item.Key);
            }
            return false;
        }

        public bool TryGetValue(string key, out string value)
        {
            IntPtr entry;
            if ((entry = DictGet(this, key, IntPtr.Zero, DictFlags.MatchCase)) != IntPtr.Zero)
            {
                value = GetEntry(entry).Value;
                return true;
            }
            value = default;
            return false;
        }

        public bool TryGetValues(string key, DictFlags flags, out string[] values)
        {
            var list = new List<string>();
            AVDictionaryEntry* prev = null;
            while ((prev = (AVDictionaryEntry*)DictGet(this, key, (IntPtr)prev, flags)) != null)
            {
                list.Add(((IntPtr)prev->value).PtrToStringUTF8());
            }
            values = list.ToArray();
            return values.Length > 0;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static implicit operator AVDictionary**(MediaDictionary value)
        {
            if (value == null) return null;
            fixed (AVDictionary** ppDictionary = &value.pDictionary)
                return ppDictionary;
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Clear();
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

    [Flags]
    public enum DictFlags : int
    {
        /// <summary>
        /// Default is case insensitive.
        /// </summary>
        None = 0,

        /// <summary>
        /// Only get an entry with exact-case key match. Only relevant in get method.
        /// <para>Add("k1","v1",DictFlags.AV_DICT_IGNORE_SUFFIX);</para>
        /// <para>//get "k1" == "v1"</para>
        /// <para>//get "K1" == null</para>
        /// </summary>
        MatchCase = 1,

        /// <summary>
        /// Return first entry in a dictionary whose first part corresponds to the search key,
        /// ignoring the suffix of the found key string. Only relevant in get method.
        /// <para>Add("k1","v1",DictFlags.AV_DICT_IGNORE_SUFFIX);</para>
        /// <para>//get "k" == "v1"</para>
        /// </summary>
        IgnoreSuffix = 2,

        //AV_DICT_DONT_STRDUP_KEY = 4, // Not suppord in managed code
        //AV_DICT_DONT_STRDUP_VAL = 8, // Not suppord in managed code

        /// <summary>
        /// Don't overwrite existing key.
        /// <para>Add("k1",v1);</para>
        /// <para>Add("k1","v2",DictFlags.AV_DICT_DONT_OVERWRITE);</para>
        /// <para>//"k1" == "v1"</para>
        /// </summary>
        DontOverwrite = 16,

        /// <summary>
        /// If the key already exists, append to it's value.
        /// <para>Add("k1",v1);</para>
        /// <para>Add("k1","v2",DictFlags.AV_DICT_APPEND);</para>
        /// <para>//"k1" == "v1v2"</para>
        /// </summary>
        Append = 32,

        /// <summary>
        /// Allow to store several equal keys in the dictionary
        /// <para>Add("k1",v1);</para>
        /// <para>Add("k1","v2",DictFlags.AV_DICT_MULTIKEY);</para>
        /// <para>//"k1" == {"v1","v2"}</para>
        /// </summary>
        MultiKey = 64,
    }


}
