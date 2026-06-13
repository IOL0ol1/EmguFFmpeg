FFmpeg.Sharp
=====================
**A [FFmpeg.AutoGen](https://github.com/Ruslan-B/FFmpeg.AutoGen) wrapper library.**

[![NuGet version (FFmpeg4Sharp)](https://img.shields.io/nuget/v/FFmpeg4Sharp.svg)](https://www.nuget.org/packages/FFmpeg4Sharp/)
[![NuGet downloads (FFmpeg4Sharp)](https://img.shields.io/nuget/dt/FFmpeg4Sharp.svg)](https://www.nuget.org/packages/FFmpeg4Sharp/)
[![Build status](https://ci.appveyor.com/api/projects/status/rrsd6t3pn1gqurbt?svg=true)](https://ci.appveyor.com/project/IOL0ol1/emguffmpeg-hhiy2)

This is **NOT** a ffmpeg command-line library.
Please use FFmpeg shared libraries version >= 5.

## Install

### Get ffmpeg DLLs
Either download from [ffmpeg.org](http://www.ffmpeg.org/download.html) according to the license you need, or pull a NuGet redistributable:

```
NuGet\Install-Package FFmpeg.GPL
NuGet\Install-Package FFmpeg.LGPL
```

### Install FFmpeg4Sharp
```
NuGet\Install-Package FFmpeg4Sharp
```

Namespaces:
```csharp
using FFmpeg.AutoGen;
using FFmpeg.Sharp;
```

## Quick start

### Encode and mux
Create a muxer, build an encoder with the fluent builder, then feed frames to `EncodeFrame` and write the resulting packets.

```csharp
const string output = "out.mp4";
using var muxer = MediaMuxer.Create(output);
using var encoder = MediaEncoder.Video()
    .OutputFormat(muxer.Format)
    .Size(800, 600)
    .Fps(29.97)
    .Configure(c => c.Ref.thread_count = 0) // 0 = auto (one per core)
    .Build();
var stream = muxer.AddStream(encoder);
muxer.WriteHeader();

using var frame = MediaFrame.CreateVideoFrame(800, 600, encoder.Ref.pix_fmt);
for (int i = 0; i < 300; i++)
{
    frame.MakeWritable();          // the encoder may still hold a reference to the frame
    // ... fill frame.Ref.data[plane] ...
    frame.Ref.pts = i;             // pts in encoder time_base (1/fps)
    foreach (var packet in encoder.EncodeFrame(frame))
    {
        packet.Ref.stream_index = stream.Ref.index;
        muxer.WritePacket(packet, encoder.Ref.time_base); // rescales encoder time_base → stream time_base
    }
}
muxer.FlushCodecs(new[] { encoder });      // drain the encoder
muxer.WriteTrailer();
```

### Demux and decode
Use `MediaDemuxer.ReadFrames` for the common case: route packets to per-stream decoders and yield decoded frames in one call.

```csharp
var input  = "input.mp4";
var output = "frames-out";

using var demuxer = MediaDemuxer.Open(input);
var decoders = demuxer
    .Select(s => (s.Index, decoder: MediaDecoder.CreateDecoder(s.CodecparRef)))
    .Where(p => p.decoder != null)
    .ToDictionary(p => p.Index, p => p.decoder);

using var convert    = new Swscale();
using var bgrFrame   = MediaFrame.CreateVideoFrame(decoders.First().Value.Ref.width,
                                                   decoders.First().Value.Ref.height,
                                                   AVPixelFormat.AV_PIX_FMT_BGR24);
try
{
    foreach (var (streamIndex, frame) in demuxer.ReadFrames(decoders, AVMediaType.AVMEDIA_TYPE_VIDEO))
    {
        convert.Convert(frame, bgrFrame); // single-frame, no array allocation; auto-resets on size change
        // save bgrFrame somewhere
    }
}
finally
{
    foreach (var d in decoders.Values) d.Dispose();
}
```

### Hardware-accelerated decode (zero-copy GPU frames)
```csharp
using var demuxer = MediaDemuxer.Open("input.mp4");
MediaCodec dec = null;
var vi = demuxer.FindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, ref dec);

using var hwDecoder = new MediaDecoder(dec);
hwDecoder.SetCodecParameters(ref demuxer[vi].CodecparRef);
hwDecoder.Ref.thread_count = 0; // 0 = auto
hwDecoder.InitHWDeviceContext("d3d11va"); // or "cuda", "qsv", "vaapi", ...
hwDecoder.Open();

using var pkt = new MediaPacket();
using var recv = new MediaFrame();
using var sw   = new MediaFrame(); // optional — omit to keep zero-copy GPU surface

foreach (var p in demuxer.ReadPackets(pkt))
{
    if (p.Ref.stream_index != vi) continue;
    foreach (var frame in hwDecoder.DecodePacket(p, recv, sw))
    {
        // `frame` is the SW download. Pass null for swFrame above to receive the raw HW surface instead.
    }
}
```

### Hardware-accelerated transcode (HW → HW, no GPU↔CPU bounce)
```csharp
using var demuxer = MediaDemuxer.Open("input.mp4");
MediaCodec dec = null;
int vi = demuxer.FindBestStream(AVMediaType.AVMEDIA_TYPE_VIDEO, ref dec);

// One device, shared explicitly across decoder and encoder.
using var cuda = HWDeviceContext.Create(AVHWDeviceType.AV_HWDEVICE_TYPE_CUDA);

using var hwDecoder = new MediaDecoder(dec);
hwDecoder.SetCodecParameters(ref demuxer[vi].CodecparRef);
hwDecoder.InitHWDeviceContext(cuda);
hwDecoder.Open();

using var hwEncoder = MediaEncoder.Video()
    .Codec("h264_nvenc")
    .Size(demuxer[vi].CodecparRef.width, demuxer[vi].CodecparRef.height)
    .Fps(30)
    .UseHardware(AVPixelFormat.AV_PIX_FMT_CUDA, AVPixelFormat.AV_PIX_FMT_NV12, cuda)
    .Bitrate(4_000_000)
    .Build();
```

### Audio resample to encoder.frame_size
```csharp
using var resampler = AudioResampler.For(audioDecoder, audioEncoder);

foreach (var (_, decoded) in demuxer.ReadFrames(audioDecoders, AVMediaType.AVMEDIA_TYPE_AUDIO))
{
    foreach (var sized in resampler.Convert(decoded))
        using (sized)
        {
            foreach (var pkt in audioEncoder.EncodeFrame(sized))
                muxer.WritePacket(pkt, audioEncoder.Ref.time_base);
        }
}
foreach (var tail in resampler.Flush()) // drain
    using (tail)
    {
        foreach (var pkt in audioEncoder.EncodeFrame(tail))
            muxer.WritePacket(pkt, audioEncoder.Ref.time_base);
    }
```

More: **[example/](./example)**.

## Performance notes

- **Codec threading** — FFmpeg codecs default to a single thread. Set `thread_count` *before* `Open`: `.Configure(c => c.Ref.thread_count = 0)` on the builder, or `decoder.Ref.thread_count = 0` before `Open()`. `0` means auto (one per core).
- **Reuse frames/packets in hot loops** — `DecodePacket(pkt, recvFrame)`, `EncodeFrame(frame, recvPacket)` and `ReadPackets(pkt)` all accept a reusable receive object; passing one avoids a native alloc/free per call (`ReadFrames` already does this internally). Plain `foreach` over `DecodePacket`/`EncodeFrame` uses the zero-allocation struct enumerator; going through LINQ boxes it.
- **Dispose deterministically** — a single 4K NV12 frame holds ~12 MB of native memory the GC cannot see. Rely on `using`/`Dispose`, not finalizers, or native memory grows far ahead of any GC pressure.
- **Skip unwanted streams at the demuxer** — for streams you never consume, set `demuxer[i].Ref.discard = AVDiscard.AVDISCARD_ALL`: `av_read_frame` then drops their packets internally (mpegts skips parsing them entirely) instead of surfacing them just to be ignored. On inputs with many audio/subtitle tracks this cuts demux work several-fold.
- **Open latency** — `MediaDemuxer.Open(..., findStreamInfo: false)` skips the probing pass (which can pre-read megabytes) for known-format / low-latency inputs; call `FindStreamInfo` later or fill decoder parameters yourself.
- **Single-stream muxing** — `WritePacketDirect` writes via `av_write_frame`, bypassing the interleaving queue; use it when the output has one stream or you interleave yourself (dts must increase monotonically per stream). For many-stream live muxing, bound interleave memory with `muxer.Ref.max_interleave_delta`.
- **Scaling** — `Swscale` is single-threaded by default; set `SwscaleOptions.Threads` (FFmpeg ≥ 5.1), or run heavy scale/overlay chains in a `MediaFilterGraph` with `ThreadCount = 0` (auto).
- **Hardware pipelines** — keep frames on the GPU: pass `swFrame: null` to `DecodePacket` for zero-copy surfaces, and share one `HWDeviceContext`/`HWFramesContext` across decoder → filters → encoder (see the HW sections above). Download (`TransferToSoftware`) only when CPU access is genuinely needed.

## Breaking changes in 8.1.0

This release is a heavyweight cleanup driven by an audit (see `docs/migration-7-to-8.md` for full details and before/after snippets).

Highlights:
- `MediaFrame.Clone()` / `MediaPacket.Clone()` no longer leak (`disposedValue` default flipped).
- `MediaDemuxer.Open(Stream)` / `MediaMuxer.Create(Stream)` no longer close your stream by default — pass `leaveOpen: false` to opt in.
- `MediaIOContext` callbacks catch managed exceptions and surface them as `IOException` on the next managed call (no more crashes from network blips).
- `MediaDemuxer.ReadPackets` no longer yields a ghost packet at EOF; pair with `ReadPacketsCloned()` for safe enqueuing.
- `MediaCodecParserContext.ParserPackets` — fixed NRE and dangling-pointer-on-byte[] bug.
- `MediaEncoder.EncodeFrame` no longer calls `av_frame_make_writable` in `finally` (call `frame.MakeWritable()` yourself before reuse).
- `DecodePacket`/`EncodeFrame` return allocation-free struct cursors (`Frames`/`Packets`) and send their input eagerly at call time — plain `foreach` code is unaffected; see the migration guide.
- New builder: `MediaEncoder.Video()` / `MediaEncoder.Audio()` — the 14 legacy `CreateVideoEncoder`/`CreateAudioEncoder` overloads are removed. The `MediaEncoder.CreateEncoder(codecpar)` one-liner remains.
- New `MediaCodecContext.Open(opts)` instance method. The `Action<MediaCodecContext>`-based lambda factories (`MediaDecoder.Create`, `MediaEncoder.Create`, the `Action` parameters on `CreateDecoder`/`CreateEncoder`) are removed — configure with straight-line code between the ctor and `Open()`.
- New hardware encoder path: `MediaEncoder.Video().UseHardware(...)` + `MediaCodecContext.AttachHWDevice/AttachHWFramesContext`.
- New `MediaFrame.IsHardwareFrame` / `TransferToSoftware` / `AllocateOnHWFrames`.
- New `AudioResampler`, `Swscale.Options`, `Swresample.Flush(...)`.
- `IConverter.Convert` now returns `int` (frames written), not `IEnumerable<MediaFrame>`. The old enumerable shape (`ConvertEnumerable`) has been removed.
- Typo fixes: `MediaCodec.GetSampelFmts` → `GetSampleFormats`, `MediaFilter.GetGetFilters` → `GetFilters` (old names removed).
- `MediaDictionary` indexer returns `null` on miss instead of throwing.
- PascalCase field-mirror shortcuts (`Width`, `Pts`, `StreamIndex`, ...) are removed — raw field access goes through the `.Ref.snake_case` escape hatch (`p.Ref.stream_index`, `frame.Ref.width`, ...). Take locals with `ref var x = ref frame.Ref;` — a plain `var` copies the struct.

## Troubleshooting

**`DllNotFoundException: avformat-XX.dll`** — FFmpeg.AutoGen does not ship native binaries. Either install `FFmpeg.GPL` / `FFmpeg.LGPL` NuGets, or set the loader's search root before any FFmpeg call:
```csharp
ffmpeg.RootPath = @"C:\path\to\ffmpeg\bin";
// or AppDomain.CurrentDomain.BaseDirectory, or any folder that contains avcodec/avformat/avutil/swscale/swresample DLLs
```

**Version mismatch** — this library tracks FFmpeg shared libraries 7.x / 8.x (corresponding FFmpeg.AutoGen versions 7.x / 8.x). FFmpeg 6.x or earlier are not supported.

**`AV_DICT_DONT_STRDUP_KEY` / `AV_DICT_DONT_STRDUP_VAL`** — these flags transfer ownership of an av_malloc'd buffer to the dictionary, which the managed wrapper cannot do safely. They are marked `[Obsolete(error=true)]`.

**HW decode falls back to software silently** — pass `fallbackToSw: false` (the default) to `InitHWDeviceContext` to make this fail instead. Use `fallbackToSw: true` to opt into the graceful fallback.

**HW encode `EINVAL` on first frame** — feed frames whose `format` matches the encoder's `hwPixelFormat`, allocated with `frame.AllocateOnHWFrames(encoder.GetHWFrames())` instead of `AllocateBuffer()`.

**Stream gets unexpectedly closed** — `MediaDemuxer.Open(Stream)` / `MediaMuxer.Create(Stream)` default to `leaveOpen: true` since 8.1.0, but if you upgraded from 7.x your old call sites may still be wiring the wrapper's lifecycle to your stream. Inspect the third (boolean) argument.

## Roadmap
- Easy API for cut/seek/mute audio clip.
- Easy API for cut/seek video clip.
- More examples and tests.
- Filter graph parser (`avfilter_graph_parse2`).
- Subtitle support.
- Async/IAsyncEnumerable surface (demux/encode/mux).

## Related
- [FFmpeg.AutoGen](https://github.com/Ruslan-B/FFmpeg.AutoGen) — the underlying P/Invoke bindings.
- [FFmpeg API documentation](https://ffmpeg.org/doxygen/trunk/index.html).

## License
This project is licensed under the MIT license.

If you use FFmpeg builds licensed under the GPL, that license is contagious.
