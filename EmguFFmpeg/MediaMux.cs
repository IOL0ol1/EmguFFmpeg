using FFmpeg.AutoGen;

using System;
using System.Collections;
using System.Collections.Generic;

namespace FFmpegManaged
{
    public unsafe abstract class MediaMux : IDisposable, IReadOnlyList<MediaStream>
    {
        protected AVFormatContext* pFormatContext;

        public AVFormatContext AVFormatContext => *pFormatContext;

        public MediaFormat Format { get; protected set; }

        public static implicit operator AVFormatContext*(MediaMux value)
        {
            if (value == null) return null;
            return value.pFormatContext;
        }

        public abstract void DumpInfo();

        public void Close()
        {
            Dispose();
        }

        #region IReadOnlyList<MediaStream>

        protected List<MediaStream> streams = new List<MediaStream>();

        public int Count => streams.Count;

        public MediaStream this[int index] => streams[index];

        public IEnumerator<MediaStream> GetEnumerator()
        {
            return streams.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IDisposable Support

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MediaMux()
        {
            Dispose(false);
        }

        protected abstract void Dispose(bool disposing);

        #endregion
    }
}