using FFmpeg.AutoGen;

using System;

namespace EmguFFmpeg
{
    /// <summary>
    /// <see cref="AVAudioFifo"/> wapper
    /// </summary>
    public class AudioFifo : IDisposable
    {
        private unsafe AVAudioFifo* pAudioFifo;

        /// <summary>
        /// alloc <see cref="AVAudioFifo"/>
        /// </summary>
        /// <param name="format"></param>
        /// <param name="channels"></param>
        /// <param name="nbSamples"></param>
        public AudioFifo(AVSampleFormat format, int channels, int nbSamples = 1)
        {
            unsafe
            {
                pAudioFifo = ffmpeg.av_audio_fifo_alloc(format, channels, nbSamples <= 0 ? 1 : nbSamples);
            }
        }

        /// <summary>
        /// Get the current number of samples in the AVAudioFifo available for reading.
        /// </summary>
        public int Size { get { unsafe { return ffmpeg.av_audio_fifo_size(pAudioFifo); } } }

        /// <summary>
        /// Get the current number of samples in the AVAudioFifo available for writing.
        /// </summary>
        public int Space { get { unsafe { return ffmpeg.av_audio_fifo_space(pAudioFifo); } } }

        /// <summary>
        ///  Peek data from an AVAudioFifo.
        /// </summary>
        /// <param name="data"> audio data plane pointers</param>
        /// <param name="nbSamples">number of samples to peek</param>
        /// <returns>
        /// number of samples actually peek, or negative AVERROR code on failure. The number
        /// of samples actually peek will not be greater than nb_samples, and will only be
        /// less than nb_samples if av_audio_fifo_size is less than nb_samples.
        /// </returns>
        public unsafe int Peek(void** data, int nbSamples)
        {
            return ffmpeg.av_audio_fifo_peek(pAudioFifo, data, nbSamples).ThrowExceptionIfError();
        }

        /// <summary>
        /// Peek data from an AVAudioFifo.
        /// </summary>
        /// <param name="data">audio data plane pointers</param>
        /// <param name="nbSamples">number of samples to peek</param>
        /// <param name="Offset">offset from current read position</param>
        /// <returns>
        /// number of samples actually peek, or negative AVERROR code on failure. The number
        /// of samples actually peek will not be greater than nb_samples, and will only be
        /// less than nb_samples if av_audio_fifo_size is less than nb_samples.
        /// </returns>
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
            {
                var ret = ffmpeg.av_audio_fifo_realloc(pAudioFifo, Size + nbSamples).ThrowExceptionIfError();
                return ret;
            }
            return ffmpeg.av_audio_fifo_write(pAudioFifo, data, nbSamples).ThrowExceptionIfError();
        }

        /// <summary>
        /// Read data from an AVAudioFifo.
        /// </summary>
        /// <param name="data">audio data plane pointers</param>
        /// <param name="nbSamples">number of samples to read</param>
        /// <exception cref="FFmpegException"/>
        /// <returns>
        /// number of samples actually read, or negative AVERROR code on failure. The number
        /// of samples actually read will not be greater than nb_samples, and will only be
        /// less than nb_samples if av_audio_fifo_size is less than nb_samples.
        /// </returns>
        public unsafe int Read(void** data, int nbSamples)
        {
            return ffmpeg.av_audio_fifo_read(pAudioFifo, data, nbSamples).ThrowExceptionIfError();
        }

        /// <summary>
        /// Drain data from an <see cref="AVAudioFifo"/>.
        /// </summary>
        /// <param name="nbSamples">number of samples to drain</param>
        /// <returns>0 if OK, or negative AVERROR code on failure</returns>
        /// <exception cref="FFmpegException"/>
        public int Drain(int nbSamples)
        {
            unsafe
            {
                return ffmpeg.av_audio_fifo_drain(pAudioFifo, nbSamples).ThrowExceptionIfError();
            }
        }

        /// <summary>
        /// Reset tha <see cref="AVFifoBuffer"/> buffer
        /// </summary>
        public void Clear()
        {
            unsafe
            {
                ffmpeg.av_audio_fifo_reset(pAudioFifo);
            }
        }

        #region IDisposable Support

        private bool disposedValue = false;

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
