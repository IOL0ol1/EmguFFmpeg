using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using FFmpeg.AutoGen;

using static FFmpeg.AutoGen.ffmpeg;

namespace EmguFFmpeg
{
    /// <summary>
    /// <see cref="AVDictionary"/>
    /// </summary>
    public unsafe class MediaDictionary2 : IDictionary<string, string>, IDisposable
    {
        public AVDictionary* _handle;

        private MediaDictionary2(AVDictionary* dict)
        {
            _handle = dict;
        }

        public static MediaDictionary2 Empty => new MediaDictionary2(null);

        public static MediaDictionary2 Copy(MediaDictionary2 source)
        {
            AVDictionary* destination = null;
            av_dict_copy(&destination, source, 0).ThrowIfError();
            return new MediaDictionary2(destination);
        }

        public static MediaDictionary2 FromDictionary(IDictionary<string, string> dict)
        {
            MediaDictionary2 md = Empty;
            foreach (var entry in dict)
            {
                md[entry.Key] = entry.Value;
            }
            return md;
        }

        public static implicit operator AVDictionary*(MediaDictionary2 dict) => (AVDictionary*)dict._handle;
        public static implicit operator AVDictionary**(MediaDictionary2 dict)
        {
            fixed (AVDictionary** dictPtr2Ptr = &dict._handle)
                return dictPtr2Ptr;
        }

        #region IDictionary<string, string> entries
        public ICollection<string> Keys => this.Select(x => x.Key).ToArray();

        public ICollection<string> Values => this.Select(x => x.Key).ToArray();

        public int Count => av_dict_count(this);

        public bool IsReadOnly => false;

        public string this[string key]
        {
            get
            {
                AVDictionaryEntry* entry = av_dict_get(this, key, null, (int)DictFlags.MatchCase);
                if (entry == null)
                {
                    throw new KeyNotFoundException(key);
                }

                return ((IntPtr)entry->value).PtrToStringUTF8();
            }
            set
            {
                AVDictionary* ptr = this;
                av_dict_set(&ptr, key, value, (int)DictFlags.None).ThrowIfError();
                _handle = ptr;
            }
        }

        public void Add(string key, string value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value)); // in AVDictionary, value is also not-null.

            if (ContainsKey(key))
            {
                throw new ArgumentException($"An item with the same key has already been added. Key: {key}");
            }

            AVDictionary* ptr = this;
            av_dict_set(&ptr, key, value, (int)DictFlags.DontOverwrite).ThrowIfError();
            _handle = ptr;
        }

        public bool ContainsKey(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            AVDictionaryEntry* entry = av_dict_get(this, key, null, (int)DictFlags.MatchCase);
            return entry != null;
        }

        public bool Remove(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            AVDictionary* ptr = this;
            bool containsKey = ContainsKey(key);

            av_dict_set(&ptr, key, null, 0).ThrowIfError();
            _handle = ptr;
            return containsKey;
        }

        public bool TryGetValue(string key, out string value)
        {
            AVDictionaryEntry* entry = av_dict_get(this, key, null, (int)DictFlags.MatchCase);
            if (entry == null)
            {
                value = null;
                return false;
            }
            else
            {
                value = ((IntPtr)entry->value).PtrToStringUTF8();
                return true;
            }
        }

        void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item)
        {
            Add(item.Key, item.Value);
        }

        bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item)
        {
            return TryGetValue(item.Key, out string value) && value == item.Value;
        }

        bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item)
        {
            if (TryGetValue(item.Key, out string value) && value == item.Value)
            {
                Remove(item.Key);
                return true;
            }
            return false;
        }

        public void Clear() => Close();

        void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < Count) throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            foreach (KeyValuePair<string, string> entry in this)
            {
                array[arrayIndex++] = entry;
            }
        }

        private static KeyValuePair<string, string> GenerateResult(IntPtr ptr)
        {
            var entry = (AVDictionaryEntry*)ptr;
            return new KeyValuePair<string, string>(
                ((IntPtr)entry->key).PtrToStringUTF8(),
                ((IntPtr)entry->value).PtrToStringUTF8());
        }
        private static IntPtr av_dict_get_safe(MediaDictionary2 dict, IntPtr prev)
        {
            return (IntPtr)av_dict_get(dict, "", (AVDictionaryEntry*)prev, (int)DictFlags.IgnoreSuffix);
        }
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            IntPtr opaque = IntPtr.Zero;
            while (true)
            {
                opaque = av_dict_get_safe(this, opaque);
                if (opaque == IntPtr.Zero) yield break;

                yield return GenerateResult(opaque);
            }


        }

        #endregion

        public void Set(string key, string value, DictFlags flags = DictFlags.DontOverwrite)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value)); // in AVDictionary, value is also not-null.

            AVDictionary* ptr = this;
            av_dict_set(&ptr, key, value, (int)flags).ThrowIfError();
            _handle = ptr;
        }

        public void Close()
        {
            AVDictionary* p = this;
            av_dict_free(&p);
            _handle = null;
        }

        public void Dispose()
        {
            Close();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal static class intex
    {
        public static int ThrowIfError(this int ret)
        {
            return ret < 0 ? throw new Exception() : ret;
        }
    }
}
