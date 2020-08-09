using FFmpeg.AutoGen;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmguFFmpeg
{
    public class MediaDictionary : IReadOnlyDictionary<string, string>, ICloneable, IDisposable
    {
        public const DictFlags DefaultFlags = DictFlags.AV_DICT_MATCH_CASE | DictFlags.AV_DICT_DONT_OVERWRITE;
        public const DictFlags Zero = DictFlags.AV_DICT_NONE;

        /// <summary>
        /// DO NOT USE THIS VALUE EVER.
        /// <para>
        /// get real pointer by *(<see cref="ppDictionary"/>)
        /// or use implicit conversion operator
        /// from <see cref="MediaDictionary"/> to <see cref="AVDictionary"/>*
        /// </para>
        /// </summary>
        private unsafe AVDictionary* internalPointerPlaceHolder = null;

        /// <summary>
        /// NOTE: ffmpeg maybe change the value of *<see cref="ppDictionary"/>
        /// </summary>
        private unsafe AVDictionary** ppDictionary = null;

        public MediaDictionary()
        {
            unsafe
            {
                fixed (AVDictionary** pInitPointer = &internalPointerPlaceHolder)
                    ppDictionary = pInitPointer;
            }
        }

        public unsafe static implicit operator AVDictionary*(MediaDictionary value)
        {
            if (value == null)
                return null;
            return *value.ppDictionary;
        }

        public unsafe static implicit operator AVDictionary**(MediaDictionary value)
        {
            if (value == null) return null;
            return value.ppDictionary;
        }

        public unsafe static KeyValuePair<string, string>[] GetKeyValues(AVDictionary* dict)
        {
            List<KeyValuePair<string, string>> keyValuePairs = new List<KeyValuePair<string, string>>();
            AVDictionaryEntry* t = null;
            while ((t = ffmpeg.av_dict_get(dict, "", t, (int)(DictFlags.AV_DICT_IGNORE_SUFFIX))) != null)
            {
                keyValuePairs.Add((*t).ToKeyValuePair());
            }
            return keyValuePairs.ToArray();
        }

        public KeyValuePair<string, string>[] KeyValues
        {
            get { unsafe { return GetKeyValues(*ppDictionary); } }
        }

        public int Count { get { unsafe { return ffmpeg.av_dict_count(*ppDictionary); } } }

        public void Clear()
        {
            unsafe
            {
                ffmpeg.av_dict_free(ppDictionary);
            }
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (var item in KeyValues)
                yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return KeyValues.GetEnumerator();
        }

        public void Add(string key, string value, DictFlags flags = DefaultFlags)
        {
            unsafe
            {
                ffmpeg.av_dict_set(ppDictionary, key, value, (int)flags).ThrowExceptionIfError();
            }
        }

        public void Add(string key, long value, DictFlags flags = DefaultFlags)
        {
            unsafe
            {
                ffmpeg.av_dict_set_int(ppDictionary, key, value, (int)flags).ThrowExceptionIfError();
            }
        }

        public string[] GetValue(string key, DictFlags flags)
        {
            unsafe
            {
                List<string> output = new List<string>();
                AVDictionaryEntry* t = null;
                while ((t = ffmpeg.av_dict_get(*ppDictionary, key, t, (int)flags)) != null)
                {
                    output.Add((*t).ToKeyValuePair().Value);
                }
                return output.ToArray();
            }
        }

        public bool Remove(string key, DictFlags flags = DictFlags.AV_DICT_MATCH_CASE)
        {
            unsafe
            {
                int count = 0;
                AVDictionaryEntry* t = null;
                while ((t = ffmpeg.av_dict_get(*ppDictionary, key, t, (int)flags)) != null)
                {
                    Add(key, null, 0);
                    count++;
                }
                return count != 0;
            }
        }

        public string this[string key]
        {
            get => GetValue(key, DefaultFlags).First();
            set => Add(key, value, DictFlags.AV_DICT_MATCH_CASE);
        }

        public IEnumerable<string> Keys => KeyValues.Select(_ => _.Key);

        public IEnumerable<string> Values => KeyValues.Select(_ => _.Value);

        public bool ContainsKey(string key)
        {
            return GetValue(key, DefaultFlags).Length > 0;
        }

        public bool TryGetValue(string key, out string value)
        {
            string[] values = GetValue(key, DefaultFlags);
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
            unsafe
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: 释放托管状态(托管对象)。
                    }

                    // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                    // TODO: 将大型字段设置为 null。
                    ffmpeg.av_dict_free(ppDictionary);
                    internalPointerPlaceHolder = null;
                    ppDictionary = null;

                    disposedValue = true;
                }
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
            unsafe
            {
                MediaDictionary keyValuePairs = new MediaDictionary();
                ffmpeg.av_dict_copy(keyValuePairs, *ppDictionary, (int)DictFlags.AV_DICT_MULTIKEY);
                return keyValuePairs;
            }
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        #endregion

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var item in KeyValues)
                stringBuilder.Append(item);
            return stringBuilder.ToString();
        }
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