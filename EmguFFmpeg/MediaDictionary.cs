using FFmpeg.AutoGen;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EmguFFmpeg
{
    public unsafe class MediaDictionary : IReadOnlyDictionary<string, string>, ICloneable, IDisposable
    {
        public const DictFlags DefaultFlags = DictFlags.AV_DICT_MATCH_CASE | DictFlags.AV_DICT_DONT_OVERWRITE;
        public const DictFlags FFmpegFlags = DictFlags.AV_DICT_NONE;

        protected AVDictionary* pDictionary = null;

        public static implicit operator AVDictionary*(MediaDictionary value)
        {
            if (value == null) return null;
            return value.pDictionary;
        }

        public static implicit operator AVDictionary**(MediaDictionary value)
        {
            if (value == null) return null;
            fixed (AVDictionary** ppDictionary = &value.pDictionary)
                return ppDictionary;
        }

        private List<KeyValuePair<string, string>> keyValues
        {
            get
            {
                List<KeyValuePair<string, string>> keyValuePairs = new List<KeyValuePair<string, string>>();
                AVDictionaryEntry* t = null;
                while ((t = ffmpeg.av_dict_get(pDictionary, "", t, (int)(DictFlags.AV_DICT_IGNORE_SUFFIX))) != null)
                {
                    keyValuePairs.Add((*t).ToKeyValuePair());
                }
                return keyValuePairs;
            }
        }

        public IReadOnlyList<KeyValuePair<string, string>> KeyValues => keyValues;

        public int Count => ffmpeg.av_dict_count(pDictionary);

        public void Clear()
        {
            fixed (AVDictionary** ppDictionary = &pDictionary)
                ffmpeg.av_dict_free(ppDictionary);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            ((IList<KeyValuePair<string, string>>)keyValues).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return ((IList<KeyValuePair<string, string>>)keyValues).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<KeyValuePair<string, string>>)keyValues).GetEnumerator();
        }

        public void Add(string key, string value, DictFlags flags = DefaultFlags)
        {
            fixed (AVDictionary** ppDictionary = &pDictionary)
                ffmpeg.av_dict_set(ppDictionary, key, value, (int)flags).ThrowExceptionIfError();
        }

        public void Add(string key, long value, DictFlags flags = DefaultFlags)
        {
            fixed (AVDictionary** ppDictionary = &pDictionary)
                ffmpeg.av_dict_set_int(ppDictionary, key, value, (int)flags).ThrowExceptionIfError();
        }

        public string[] GetValue(string key, DictFlags flags)
        {
            List<string> output = new List<string>();
            AVDictionaryEntry* t = null;
            while ((t = ffmpeg.av_dict_get(pDictionary, key, t, (int)flags)) != null)
            {
                output.Add((*t).ToKeyValuePair().Value);
            }
            return output.ToArray();
        }

        public bool Remove(string key, DictFlags flags = DictFlags.AV_DICT_MATCH_CASE)
        {
            int count = 0;
            AVDictionaryEntry* t = null;
            while ((t = ffmpeg.av_dict_get(pDictionary, key, t, (int)flags)) != null)
            {
                Add(key, null, 0);
                count++;
            }
            return count != 0;
        }

        public string this[string key]
        {
            get => GetValue(key, DefaultFlags).First();
            set => Add(key, value, DictFlags.AV_DICT_MATCH_CASE);
        }

        public IEnumerable<string> Keys => keyValues.Select(_ => _.Key);

        public IEnumerable<string> Values => keyValues.Select(_ => _.Value);

        public bool ContainsKey(string key)
        {
            return GetValue(key, DefaultFlags).Length > 0;
        }

        public bool TryGetValue(string key, out string value)
        {
            var values = GetValue(key, DefaultFlags);
            if (values.Length > 0)
            {
                value = values[0];
                return true;
            }
            value = null;
            return true;
        }

        #region IDisposable Support

        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)。
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。
                fixed (AVDictionary** ppDictionary = &pDictionary)
                    ffmpeg.av_dict_free(ppDictionary);

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        ~MediaDictionary()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(false);
        }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            GC.SuppressFinalize(this);
        }

        #endregion

        #region ICloneable

        public MediaDictionary Clone()
        {
            MediaDictionary keyValuePairs = new MediaDictionary();
            fixed (AVDictionary** p = &keyValuePairs.pDictionary)
                ffmpeg.av_dict_copy(p, pDictionary, (int)DictFlags.AV_DICT_MULTIKEY);
            return keyValuePairs;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        #endregion
    }

    [Flags]
    public enum DictFlags : int
    {
        /// <summary>
        /// Default is case insensitive.
        /// </summary>
        AV_DICT_NONE = 0,

        /// <summary>
        /// Only get an entry with exact-case key match. Only relevant in get method.
        /// <para>Add("k1","v1",DictFlags.AV_DICT_IGNORE_SUFFIX);</para>
        /// <para>//get "k1" == "v1"</para>
        /// <para>//get "K1" == null</para>
        /// </summary>
        AV_DICT_MATCH_CASE = 1,

        /// <summary>
        /// Return first entry in a dictionary whose first part corresponds to the search key,
        /// ignoring the suffix of the found key string. Only relevant in get method.
        /// <para>Add("k1","v1",DictFlags.AV_DICT_IGNORE_SUFFIX);</para>
        /// <para>//get "k" == "v1"</para>
        /// </summary>
        AV_DICT_IGNORE_SUFFIX = 2,

        //AV_DICT_DONT_STRDUP_KEY = 4, // Not suppord in managed code
        //AV_DICT_DONT_STRDUP_VAL = 8, // Not suppord in managed code

        /// <summary>
        /// Don't overwrite existing key.
        /// <para>Add("k1",v1);</para>
        /// <para>Add("k1","v2",DictFlags.AV_DICT_DONT_OVERWRITE);</para>
        /// <para>//"k1" == "v1"</para>
        /// </summary>
        AV_DICT_DONT_OVERWRITE = 16,

        /// <summary>
        /// If the key already exists, append to it's value.
        /// <para>Add("k1",v1);</para>
        /// <para>Add("k1","v2",DictFlags.AV_DICT_APPEND);</para>
        /// <para>//"k1" == "v1v2"</para>
        /// </summary>
        AV_DICT_APPEND = 32,

        /// <summary>
        /// Allow to store several equal keys in the dictionary
        /// <para>Add("k1",v1);</para>
        /// <para>Add("k1","v2",DictFlags.AV_DICT_MULTIKEY);</para>
        /// <para>//"k1" == {"v1","v2"}</para>
        /// </summary>
        AV_DICT_MULTIKEY = 64,
    }

    public static class AVDictionaryEntryEx
    {
        public unsafe static KeyValuePair<string, string> ToKeyValuePair(this AVDictionaryEntry entry)
        {
            //ffmpeg.AV_DICT_APPEND
            string key = ((IntPtr)entry.key).PtrToStringUTF8();
            string value = ((IntPtr)entry.value).PtrToStringUTF8();
            return new KeyValuePair<string, string>(key, value);
        }
    }
}