using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    /// <summary>
    /// High-level "open a file, push frames in, get a finished file out" sink.
    /// Owns one or more encoders, a muxer, and a stream-index map. Auto-manages pts increment,
    /// time-base rescale, encoder flushing, and trailer writing.
    /// <para>
    /// Typical usage:
    /// <code>
    /// using (var sink = MediaSink.Create("out.mp4"))
    /// {
    ///     int v = sink.AddVideo(MediaEncoder.Video().Codec(AVCodecID.AV_CODEC_ID_H264).Size(w, h).Fps(30).Build());
    ///     sink.Start();
    ///     for (var frame in frames) sink.WriteVideoFrame(v, frame);
    /// } // Disposal flushes encoders and writes the trailer.
    /// </code>
    /// </para>
    /// </summary>
    public sealed class MediaSink : IDisposable
    {
        private readonly MediaMuxer _muxer;
        private readonly List<TrackInfo> _tracks = new List<TrackInfo>();
        private bool _started;
        private bool _disposed;

        private sealed class TrackInfo
        {
            public MediaEncoder Encoder;
            public MediaStream Stream;
            public long Pts;
            public bool IsVideo;
        }

        private MediaSink(MediaMuxer muxer) { _muxer = muxer; }

        public static MediaSink Create(string fileName, MediaOutputFormat oformat = null, string formatName = null)
            => new MediaSink(MediaMuxer.Create(fileName, oformat, formatName));

        public static MediaSink Create(System.IO.Stream stream, MediaOutputFormat oformat, bool leaveOpen = true)
            => new MediaSink(MediaMuxer.Create(stream, oformat, leaveOpen));

        /// <summary>Underlying muxer (for advanced configuration before <see cref="Start"/>).</summary>
        public MediaMuxer Muxer => _muxer;

        /// <summary>Register a video encoder. Returns the track id (also equal to the stream index).</summary>
        public int AddVideo(MediaEncoder videoEncoder)
        {
            if (videoEncoder == null) throw new ArgumentNullException(nameof(videoEncoder));
            var stream = _muxer.AddStream(videoEncoder);
            _tracks.Add(new TrackInfo { Encoder = videoEncoder, Stream = stream, IsVideo = true });
            return _tracks.Count - 1;
        }

        /// <summary>Register an audio encoder. Returns the track id.</summary>
        public int AddAudio(MediaEncoder audioEncoder)
        {
            if (audioEncoder == null) throw new ArgumentNullException(nameof(audioEncoder));
            var stream = _muxer.AddStream(audioEncoder);
            _tracks.Add(new TrackInfo { Encoder = audioEncoder, Stream = stream, IsVideo = false });
            return _tracks.Count - 1;
        }

        /// <summary>Write the file header. Call this AFTER all tracks have been added and BEFORE writing any frames.</summary>
        public void Start(MediaDictionary options = null)
        {
            _muxer.WriteHeader(options);
            _started = true;
        }

        /// <summary>
        /// Push <paramref name="frame"/> into video track <paramref name="trackId"/>. If <paramref name="frame"/>.pts is &lt; 0,
        /// pts is auto-assigned (monotonic frame index).
        /// </summary>
        public void WriteVideoFrame(int trackId, MediaFrame frame)
        {
            EnsureStarted();
            var t = _tracks[trackId];
            if (!t.IsVideo) throw new InvalidOperationException($"Track {trackId} is audio, not video.");
            WriteFrame(t, frame, samplesAdvance: 1);
        }

        /// <summary>Push <paramref name="frame"/> into audio track <paramref name="trackId"/>. pts is auto-assigned in samples.</summary>
        public void WriteAudioFrame(int trackId, MediaFrame frame)
        {
            EnsureStarted();
            var t = _tracks[trackId];
            if (t.IsVideo) throw new InvalidOperationException($"Track {trackId} is video, not audio.");
            WriteFrame(t, frame, samplesAdvance: frame?.Ref.nb_samples ?? 0);
        }

        private void WriteFrame(TrackInfo t, MediaFrame frame, int samplesAdvance)
        {
            if (frame != null)
            {
                if (frame.Ref.pts < 0)
                {
                    frame.Ref.pts = t.Pts;
                    t.Pts += samplesAdvance;
                }
                else
                {
                    t.Pts = frame.Ref.pts + samplesAdvance;
                }
            }
            foreach (var packet in t.Encoder.EncodeFrame(frame))
            {
                packet.Ref.stream_index = t.Stream.Ref.index;
                _muxer.WritePacket(packet, t.Encoder.Ref.time_base);
            }
        }

        private void EnsureStarted()
        {
            if (!_started) throw new InvalidOperationException("Call Start() before writing frames.");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                if (_started)
                {
                    // Flush each encoder, then write trailer.
                    foreach (var t in _tracks)
                    {
                        try
                        {
                            foreach (var packet in t.Encoder.EncodeFrame(null))
                            {
                                packet.Ref.stream_index = t.Stream.Ref.index;
                                _muxer.WritePacket(packet, t.Encoder.Ref.time_base);
                            }
                        }
                        catch { /* best-effort flush */ }
                    }
                    try { _muxer.WriteTrailer(); } catch { /* best-effort */ }
                }
            }
            finally
            {
                foreach (var t in _tracks) t.Encoder.Dispose();
                _muxer.Dispose();
            }
        }
    }
}
