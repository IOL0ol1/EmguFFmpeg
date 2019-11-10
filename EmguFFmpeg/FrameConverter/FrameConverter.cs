using FFmpeg.AutoGen;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace EmguFFmpeg
{
    public abstract class FrameConverter : IDisposable
    {
        protected bool isDisposing = false;

        protected MediaFrame dstFrame;

        public virtual IEnumerable<MediaFrame> Convert(MediaFrame frame)
        {
            throw new FFmpegException(FFmpegException.NotImplemented);
        }

        #region IDisposable Support

        protected abstract void Dispose(bool disposing);

        ~FrameConverter()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    public abstract class FrameConverter<T> : FrameConverter where T : MediaFrame
    {
        public new abstract IEnumerable<T> Convert(MediaFrame frame);
    }
}