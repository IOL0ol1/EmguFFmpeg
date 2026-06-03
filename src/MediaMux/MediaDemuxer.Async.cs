using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FFmpeg.AutoGen;

namespace FFmpeg.Sharp
{
    /// <summary>
    /// Async + cancellation extensions on <see cref="MediaDemuxer"/>.
    /// <para>
    /// On netstandard2.1+/net6+, <c>ReadPacketsAsync</c> returns an <c>IAsyncEnumerable&lt;MediaPacket&gt;</c> that
    /// dispatches each <c>av_read_frame</c> call to a worker thread and respects <see cref="CancellationToken"/>.
    /// </para>
    /// <para>
    /// Cancellation cooperates with FFmpeg's <c>AVFormatContext.interrupt_callback</c> so a blocking network
    /// read can be aborted from another thread. The pinned trampoline is freed when the enumeration ends.
    /// </para>
    /// </summary>
    public static class MediaDemuxerAsyncExtensions
    {
        // Delegate matching avio_interrupt_cb_t: int(void*).
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate int InterruptDelegate(void* opaque);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET
        /// <summary>
        /// Read packets asynchronously. Each call hops to the thread pool and is interruptible via <paramref name="ct"/>.
        /// The yielded <see cref="MediaPacket"/> is shared across iterations (call <see cref="MediaPacket.Clone"/> to outlive the next pull).
        /// </summary>
        public static async IAsyncEnumerable<MediaPacket> ReadPacketsAsync(
            this MediaDemuxer demuxer,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            if (demuxer == null) throw new ArgumentNullException(nameof(demuxer));
            var pkt = new MediaPacket();
            using (var _ = InterruptScope.Bind(demuxer, ct))
            {
                try
                {
                    while (true)
                    {
                        ct.ThrowIfCancellationRequested();
                        int ret = await Task.Run(() => ReadOneSync(demuxer, pkt), ct).ConfigureAwait(false);
                        if (ret == ffmpeg.AVERROR_EOF) yield break;
                        if (ret < 0) ret.ThrowIfError();
                        try { yield return pkt; }
                        finally { pkt.Unref(); }
                    }
                }
                finally { pkt.Dispose(); }
            }
        }
#endif

        /// <summary>
        /// Single-shot async read. Returns the FFmpeg return code (0 on success, AVERROR_EOF at end, &lt; 0 on error).
        /// </summary>
        public static Task<int> ReadNextAsync(this MediaDemuxer demuxer, MediaPacket packet, CancellationToken ct = default)
        {
            if (demuxer == null) throw new ArgumentNullException(nameof(demuxer));
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            return Task.Run(() => ReadOneSync(demuxer, packet), ct);
        }

        private static unsafe int ReadOneSync(MediaDemuxer demuxer, MediaPacket pkt)
        {
            AVFormatContext* fmt = demuxer;
            return ffmpeg.av_read_frame(fmt, pkt);
        }

        // Binds a CancellationToken to AVFormatContext.interrupt_callback for as long as the scope is alive.
        // The delegate is pinned via GCHandle.Alloc so the JIT can't elide it across the native call.
        private sealed class InterruptScope : IDisposable
        {
            private readonly MediaDemuxer _demuxer;
            private InterruptDelegate _del;
            private GCHandle _handle;

            public static InterruptScope Bind(MediaDemuxer demuxer, CancellationToken ct)
            {
                if (!ct.CanBeCanceled) return new InterruptScope(demuxer, null, default);
                unsafe
                {
                    InterruptDelegate del = _ => ct.IsCancellationRequested ? 1 : 0;
                    var handle = GCHandle.Alloc(del);
                    AVFormatContext* fmt = demuxer;
                    fmt->interrupt_callback = new AVIOInterruptCB
                    {
                        callback = new AVIOInterruptCB_callback_func
                        {
                            Pointer = Marshal.GetFunctionPointerForDelegate(del),
                        },
                        opaque = null,
                    };
                    return new InterruptScope(demuxer, del, handle);
                }
            }

            private InterruptScope(MediaDemuxer demuxer, InterruptDelegate del, GCHandle handle)
            {
                _demuxer = demuxer;
                _del = del;
                _handle = handle;
            }

            public void Dispose()
            {
                if (_del != null)
                {
                    unsafe
                    {
                        AVFormatContext* fmt = _demuxer;
                        if (fmt != null) fmt->interrupt_callback = default;
                    }
                }
                if (_handle.IsAllocated) _handle.Free();
                _del = null;
            }
        }
    }
}
