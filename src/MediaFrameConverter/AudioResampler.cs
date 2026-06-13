using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    /// <summary>
    /// High-level audio resampler that bridges variable-size decoded audio frames to fixed-size encoder input.
    /// <para>
    /// Construct with the source and destination audio parameters plus the encoder's required <c>frame_size</c>;
    /// then feed each decoded frame into <see cref="Convert"/> and yield as many fully-sized output frames as
    /// possible. Call <see cref="Flush"/> at end-of-stream to drain residual samples (a final partial frame is
    /// emitted when the encoder accepts variable frame sizes).
    /// </para>
    /// <para>
    /// Internally combines a <see cref="Swresample"/> (format/rate/layout conversion) with an
    /// <see cref="AudioFifo"/> (re-packetization to the target frame size).
    /// </para>
    /// </summary>
    public unsafe class AudioResampler : IBatchConverter, IDisposable
    {
        private readonly Swresample _swr;
        private readonly AudioFifo _fifo;
        private readonly int _outFrameSize;
        private readonly AVChannelLayout _outLayout;
        private readonly AVSampleFormat _outFmt;
        private readonly int _outRate;
        private long _pts; // running pts in output time base (1/sample_rate)
        // Reusable conversion target between Swresample and the FIFO; grown geometrically on demand
        // so the steady state allocates nothing per input frame.
        private MediaFrame _staged;
        private int _stagedCapacity;

        public AudioResampler(
            AVChannelLayout outChLayout, AVSampleFormat outSampleFmt, int outSampleRate, int outFrameSize,
            AVChannelLayout inChLayout, AVSampleFormat inSampleFmt, int inSampleRate)
        {
            if (outFrameSize <= 0) throw new ArgumentOutOfRangeException(nameof(outFrameSize));
            _swr = new Swresample(outChLayout, outSampleFmt, outSampleRate, inChLayout, inSampleFmt, inSampleRate);
            _fifo = new AudioFifo(outSampleFmt, outChLayout.nb_channels, outFrameSize * 4);
            _outFrameSize = outFrameSize;
            _outLayout = outChLayout;
            _outFmt = outSampleFmt;
            _outRate = outSampleRate;
        }

        /// <summary>
        /// Convenience factory that derives I/O parameters from a decoder + encoder pair.
        /// </summary>
        public static AudioResampler For(MediaDecoder decoder, MediaEncoder encoder)
        {
            if (decoder == null) throw new ArgumentNullException(nameof(decoder));
            if (encoder == null) throw new ArgumentNullException(nameof(encoder));
            int frameSize = encoder.Ref.frame_size > 0 ? encoder.Ref.frame_size : 1024;
            return new AudioResampler(
                encoder.Ref.ch_layout, encoder.Ref.sample_fmt, encoder.Ref.sample_rate, frameSize,
                decoder.Ref.ch_layout, decoder.Ref.sample_fmt, decoder.Ref.sample_rate);
        }

        /// <summary>
        /// Push one decoded frame (or <see langword="null"/> to flush) and yield zero or more fixed-size frames ready for an encoder.
        /// </summary>
        public IEnumerable<MediaFrame> Convert(MediaFrame src)
        {
            if (src != null)
                ResampleAndBuffer(src);
            while (_fifo.Size >= _outFrameSize)
                yield return PopFrame(_outFrameSize);
            if (src == null)
            {
                foreach (var tail in DrainSwr())
                    yield return tail;
            }
        }

        /// <summary>End-of-stream drain. Yields any remaining buffered samples (possibly a partial final frame).</summary>
        public IEnumerable<MediaFrame> Flush()
        {
            foreach (var tail in DrainSwr())
                yield return tail;
            while (_fifo.Size > 0)
            {
                int take = Math.Min(_outFrameSize, _fifo.Size);
                yield return PopFrame(take);
            }
        }

        private IEnumerable<MediaFrame> DrainSwr()
        {
            // Pull all pending samples out of Swresample into the FIFO, then pop frames from the FIFO.
            foreach (var partial in _swr.Flush(_outFrameSize))
            {
                _fifo.Add(partial);
                partial.Dispose();
            }
            while (_fifo.Size >= _outFrameSize)
                yield return PopFrame(_outFrameSize);
        }

        // Pointer-handling factored out of iterator bodies for C# 7.3 compatibility.
        private void ResampleAndBuffer(MediaFrame src)
        {
            int outSamples = _swr.GetOutSamples(src.Ref.nb_samples);
            if (outSamples <= 0) return;
            EnsureStaged(outSamples);
            if (_swr.Convert(src, _staged) > 0 && _staged.Ref.nb_samples > 0)
                _fifo.Add(_staged);
        }

        // swr_convert_frame uses dst nb_samples as the capacity and rewrites it with the count produced,
        // so the buffer only needs reallocating when the requirement outgrows it.
        private void EnsureStaged(int samples)
        {
            if (_staged == null || samples > _stagedCapacity)
            {
                _staged?.Dispose();
                int capacity = Math.Max(samples, _stagedCapacity * 2);
                _staged = MediaFrame.CreateAudioFrame(_outLayout, capacity, _outFmt, _outRate);
                _stagedCapacity = capacity;
            }
            _staged.Ref.nb_samples = samples;
        }

        private MediaFrame PopFrame(int samples)
        {
            var dst = MediaFrame.CreateAudioFrame(_outLayout, samples, _outFmt, _outRate);
            int n = _fifo.Read(dst, samples);
            dst.Ref.pts = _pts;
            _pts += n;
            return dst;
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue) return;
            _swr?.Dispose();
            _fifo?.Dispose();
            _staged?.Dispose();
            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
