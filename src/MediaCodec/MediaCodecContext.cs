using System;

using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    public unsafe partial class MediaCodecContext : MediaCodecContextSettings, IDisposable
    {
        private bool disposedValue;

        public static MediaCodecContext Create(Action<MediaCodecContextSettings> beforeOpenSetting, MediaCodec codec = null, MediaDictionary opts = null)
        {
            return new MediaCodecContext(codec).Open(beforeOpenSetting, null, opts);
        }

        public MediaCodecContext(AVCodecContext* pAVCodecContext, bool isDisposeByOwner = true)
        {
            pCodecContext = pAVCodecContext;
            disposedValue = !isDisposeByOwner;
        }

        public MediaCodecContext(MediaCodec codec = null)
                    : this(ffmpeg.avcodec_alloc_context3(codec), true)
        { }

        /// <summary>
        /// <see cref="ffmpeg.avcodec_open2(AVCodecContext*, AVCodec*, AVDictionary**)"/>
        /// </summary>
        /// <param name="beforeOpenSetting"></param>
        /// <param name="codec"></param>
        /// <param name="opts"></param>
        public MediaCodecContext Open(Action<MediaCodecContextSettings> beforeOpenSetting = null, MediaCodec codec = null, MediaDictionary opts = null)
        {
            beforeOpenSetting?.Invoke(this);
            ffmpeg.avcodec_open2(pCodecContext, codec, opts).ThrowIfError();
            return this;
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                fixed (AVCodecContext** ppCodecContext = &pCodecContext)
                {
                    ffmpeg.avcodec_free_context(ppCodecContext);
                }
                disposedValue = true;
            }
        }

        ~MediaCodecContext()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
