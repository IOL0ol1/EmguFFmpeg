using FFmpeg.AutoGen;

using System;

namespace EmguFFmpeg
{
    public class AudioFifo : IDisposable
    {
        private unsafe AVAudioFifo* pAudioFifo;

        public AudioFifo(AVSampleFormat format, int channels, int nbSamples = 1)
        {
            unsafe
            {
                pAudioFifo = ffmpeg.av_audio_fifo_alloc(format, channels, nbSamples <= 0 ? 1 : nbSamples);
            }
        }

        public int Size { get { unsafe { return ffmpeg.av_audio_fifo_size(pAudioFifo); } } }

        public int Space { get { unsafe { return ffmpeg.av_audio_fifo_space(pAudioFifo); } } }

        public unsafe int Peek(void** data, int nbSamples)
        {
            return ffmpeg.av_audio_fifo_peek(pAudioFifo, data, nbSamples).ThrowExceptionIfError();
        }

        public unsafe int PeekAt(void** data, int nbSamples, int Offset)
        {
            return ffmpeg.av_audio_fifo_peek_at(pAudioFifo, data, nbSamples, Offset).ThrowExceptionIfError();
        }

        /// <summary>
        /// auto realloc if space less than nbSamples
        /// </summary>
        /// <param name="data"></param>
        /// <param name="nbSamples"></param>
        /// <exception cref="FFmpegException"/>
        public unsafe int Add(void** data, int nbSamples)
        {
            if (Space < nbSamples)
                ffmpeg.av_audio_fifo_realloc(pAudioFifo, Size + nbSamples).ThrowExceptionIfError();
            return ffmpeg.av_audio_fifo_write(pAudioFifo, data, nbSamples).ThrowExceptionIfError();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <param name="nbSamples"></param>
        /// <exception cref="FFmpegException"/>
        /// <returns></returns>
        public unsafe int Read(void** data, int nbSamples)
        {
            return ffmpeg.av_audio_fifo_read(pAudioFifo, data, nbSamples).ThrowExceptionIfError();
        }

        /// <summary>
        /// Removes the data without reading it.
        /// </summary>
        /// <param name="nbSamples">number of samples to drain</param>
        /// <exception cref="FFmpegException"/>
        public void Drain(int nbSamples)
        {
            unsafe
            {
                ffmpeg.av_audio_fifo_drain(pAudioFifo, nbSamples).ThrowExceptionIfError();
            }
        }

        public void Clear()
        {
            unsafe
            {
                ffmpeg.av_audio_fifo_reset(pAudioFifo);
            }
        }

        #region IDisposable Support

        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                unsafe
                {
                    ffmpeg.av_audio_fifo_free(pAudioFifo);
                }
                disposedValue = true;
            }
        }

        ~AudioFifo()
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
}