using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    /// <summary>
    /// <see cref="SwrContext"/> wrapper. Construct with explicit in/out parameters, then call
    /// <see cref="Convert(MediaFrame, MediaFrame)"/> on each input frame, and finally <see cref="Flush"/> to drain any tail samples.
    /// <para>
    /// For variable input frame size → fixed encoder.frame_size scenarios, use <see cref="AudioResampler"/> which
    /// combines this with an <see cref="AudioFifo"/>.
    /// </para>
    /// </summary>
    public unsafe class Swresample : IConverter, IDisposable
    {
        protected SwrContext* pSwrContext;
        private AVSampleFormat _outFmt;
        private AVChannelLayout _outLayout;
        private int _outRate;

        public Swresample(SwrContext* pSwrContext, bool isDisposeByOwner = true)
        {
            if (pSwrContext == null) throw new ArgumentNullException(nameof(pSwrContext));
            this.pSwrContext = pSwrContext;
            disposedValue = !isDisposeByOwner;
        }

        /// <summary>
        /// Construct and initialise a fully-specified resampler. swr_init is called immediately so the first
        /// <see cref="Convert(MediaFrame, MediaFrame)"/> doesn't EINVAL like the historical empty-ctor pitfall.
        /// </summary>
        public Swresample(
            AVChannelLayout outChLayout, AVSampleFormat outSampleFmt, int outSampleRate,
            AVChannelLayout inChLayout, AVSampleFormat inSampleFmt, int inSampleRate,
            int logOffset = 0, void* logCtx = null)
        {
            fixed (SwrContext** ppSwrContext = &pSwrContext)
            {
                ffmpeg.swr_alloc_set_opts2(ppSwrContext, &outChLayout, outSampleFmt, outSampleRate,
                                                       &inChLayout, inSampleFmt, inSampleRate,
                                                       logOffset, logCtx).ThrowIfError();
            }
            ffmpeg.swr_init(pSwrContext).ThrowIfError();
            _outFmt = outSampleFmt;
            _outLayout = outChLayout;
            _outRate = outSampleRate;
        }

        public AVSampleFormat OutputSampleFormat => _outFmt;
        public AVChannelLayout OutputChannelLayout => _outLayout;
        public int OutputSampleRate => _outRate;

        /// <summary>
        /// Convert one input frame.
        /// </summary>
        /// <param name="src">Input frame, or <see langword="null"/> to flush (equivalent to calling <see cref="Flush"/>).</param>
        /// <param name="dst">Pre-allocated destination frame. Must have nb_samples set to a capacity ≥ <see cref="GetOutSamples(int)"/>.</param>
        /// <returns>1 if output was written into <paramref name="dst"/>, 0 if the resampler is buffering more samples before it can produce.</returns>
        public int Convert(MediaFrame src, MediaFrame dst)
        {
            if (dst == null) throw new ArgumentNullException(nameof(dst));
            int ret = ffmpeg.swr_convert_frame(pSwrContext, dst, src).ThrowIfError();
            return dst.Ref.nb_samples > 0 ? 1 : 0;
        }

        /// <summary>
        /// Drain any pending samples. Yields zero or more frames each of size <paramref name="frameSize"/> (or
        /// whatever is left for the final frame). Call after the last <see cref="Convert(MediaFrame, MediaFrame)"/>.
        /// </summary>
        public IEnumerable<MediaFrame> Flush(int frameSize)
        {
            if (frameSize <= 0) throw new ArgumentOutOfRangeException(nameof(frameSize));
            while (true)
            {
                long delay = GetDelay();
                if (delay <= 0) yield break;
                int want = Math.Min(frameSize, (int)delay);
                var dst = MediaFrame.CreateAudioFrame(_outLayout, want, _outFmt, _outRate);
                int produced = DrainInto(dst, want);
                if (produced <= 0)
                {
                    dst.Dispose();
                    yield break;
                }
                yield return dst;
            }
        }

        // C# disallows unsafe in iterator bodies; factor the pointer work out so Flush() stays managed.
        private int DrainInto(MediaFrame dst, int samples)
        {
            int produced = ffmpeg.swr_convert(pSwrContext, dst.Ref.extended_data, samples, null, 0).ThrowIfError();
            if (produced > 0) dst.Ref.nb_samples = produced;
            return produced;
        }

        /// <summary>Number of samples that would be produced for the given input sample count, including internal delay.</summary>
        public int GetOutSamples(int inSamples) => ffmpeg.swr_get_out_samples(pSwrContext, inSamples).ThrowIfError();

        /// <summary>Number of samples currently buffered, expressed in the output sample rate.</summary>
        public long GetDelay() => ffmpeg.swr_get_delay(pSwrContext, _outRate);

        public static implicit operator SwrContext*(Swresample value)
        {
            if (value is null) return null;
            return value.pSwrContext;
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                fixed (SwrContext** ppSwrContext = &pSwrContext)
                {
                    ffmpeg.swr_free(ppSwrContext);
                }
                disposedValue = true;
            }
        }

        ~Swresample()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
