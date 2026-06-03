using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    /// <summary>
    /// <see cref="AVAudioFifo"/> wrapper. Append samples with <see cref="Add(MediaFrame)"/>, drain with
    /// <see cref="Read(MediaFrame, int)"/>. Backing storage doubles on demand to amortize re-allocations.
    /// </summary>
    public unsafe class AudioFifo : IDisposable
    {
        protected AVAudioFifo* pAudioFifo;
        private readonly AVSampleFormat _format;
        private readonly int _channels;
        private bool disposedValue;

        public AudioFifo(AVAudioFifo* pAVAudioFifo, bool isDisposeByOwner = true)
        {
            if (pAVAudioFifo == null) throw new ArgumentNullException(nameof(pAVAudioFifo));
            pAudioFifo = pAVAudioFifo;
            disposedValue = !isDisposeByOwner;
        }

        public AudioFifo(AVSampleFormat format, int channels, int nbSamples = 1)
            : this(ffmpeg.av_audio_fifo_alloc(format, channels, nbSamples <= 0 ? 1 : nbSamples), true)
        {
            _format = format;
            _channels = channels;
        }

        /// <summary>Samples currently buffered.</summary>
        public int Size => ffmpeg.av_audio_fifo_size(pAudioFifo);

        /// <summary>Samples that fit without re-allocation.</summary>
        public int Space => ffmpeg.av_audio_fifo_space(pAudioFifo);

        /// <summary>Ensure at least <paramref name="totalCapacity"/> samples of total capacity. Uses geometric growth.</summary>
        public void EnsureCapacity(int totalCapacity)
        {
            int current = Size + Space;
            if (current >= totalCapacity) return;
            int target = Math.Max(totalCapacity, current * 2);
            ffmpeg.av_audio_fifo_realloc(pAudioFifo, target).ThrowIfError();
        }

        public int Peek(void** data, int nbSamples)
            => ffmpeg.av_audio_fifo_peek(pAudioFifo, data, nbSamples).ThrowIfError();

        public int PeekAt(void** data, int nbSamples, int offset)
            => ffmpeg.av_audio_fifo_peek_at(pAudioFifo, data, nbSamples, offset).ThrowIfError();

        /// <summary>Auto-grows the buffer with a 2× growth factor if there's not enough room.</summary>
        public int Add(void** data, int nbSamples)
        {
            EnsureCapacity(Size + nbSamples);
            return ffmpeg.av_audio_fifo_write(pAudioFifo, data, nbSamples).ThrowIfError();
        }

        /// <summary>Append all samples from <paramref name="frame"/> (managed-friendly overload).</summary>
        public int Add(MediaFrame frame)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            return Add((void**)frame.Ref.extended_data, frame.Ref.nb_samples);
        }

        public int Read(void** data, int nbSamples)
            => ffmpeg.av_audio_fifo_read(pAudioFifo, data, nbSamples).ThrowIfError();

        /// <summary>
        /// Read up to <paramref name="nbSamples"/> samples into <paramref name="dstFrame"/>'s extended_data planes.
        /// The frame must already be sized (see <see cref="MediaFrame.CreateAudioFrame(AVChannelLayout, int, AVSampleFormat, int, int)"/>).
        /// </summary>
        /// <returns>Number of samples actually read (may be less than requested at end-of-stream).</returns>
        public int Read(MediaFrame dstFrame, int nbSamples)
        {
            if (dstFrame == null) throw new ArgumentNullException(nameof(dstFrame));
            int actual = ffmpeg.av_audio_fifo_read(pAudioFifo, (void**)dstFrame.Ref.extended_data, nbSamples).ThrowIfError();
            dstFrame.Ref.nb_samples = actual;
            return actual;
        }

        public int Drain(int nbSamples)
            => ffmpeg.av_audio_fifo_drain(pAudioFifo, nbSamples).ThrowIfError();

        /// <summary>Clear the buffer.</summary>
        public void Reset()
            => ffmpeg.av_audio_fifo_reset(pAudioFifo);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (pAudioFifo != null)
                {
                    ffmpeg.av_audio_fifo_free(pAudioFifo);
                    pAudioFifo = null;
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
    }
}
