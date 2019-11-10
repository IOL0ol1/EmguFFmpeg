using FFmpeg.AutoGen;

using System;

namespace EmguFFmpeg
{
    public unsafe class AudioFifo : IDisposable
    {
        private AVAudioFifo* pAudioFifo;

        public AudioFifo(AVSampleFormat format, ulong channelLayout, int nbSamples = 1) : this(format, ffmpeg.av_get_channel_layout_nb_channels(channelLayout), nbSamples)
        { }

        public AudioFifo(AVSampleFormat format, int channels, int nbSamples = 1)
        {
            pAudioFifo = ffmpeg.av_audio_fifo_alloc(format, channels, nbSamples <= 0 ? 1 : nbSamples);
        }

        public int Size => ffmpeg.av_audio_fifo_size(pAudioFifo);

        public int Space => ffmpeg.av_audio_fifo_space(pAudioFifo);

        public int Peek(void** data, int nbSamples)
        {
            return ffmpeg.av_audio_fifo_peek(pAudioFifo, data, nbSamples).ThrowExceptionIfError();
        }

        public int PeekAt(void** data, int nbSamples, int Offset)
        {
            return ffmpeg.av_audio_fifo_peek_at(pAudioFifo, data, nbSamples, Offset).ThrowExceptionIfError();
        }

        /// <summary>
        /// auto realloc if space less than nbSamples
        /// </summary>
        /// <param name="data"></param>
        /// <param name="nbSamples"></param>
        /// <exception cref="FFmpegException"/>
        public int Add(void** data, int nbSamples)
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
        public int Read(void** data, int nbSamples)
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
            ffmpeg.av_audio_fifo_drain(pAudioFifo, nbSamples).ThrowExceptionIfError();
        }

        public void Clear()
        {
            ffmpeg.av_audio_fifo_reset(pAudioFifo);
        }

        #region IDisposable Support

        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                ffmpeg.av_audio_fifo_free(pAudioFifo);
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