# Migration: 7.x → 8.1.0

This release tightens lifecycle semantics, replaces the converter contract, and adds first-class hardware-acceleration APIs. Most changes have `[Obsolete]` forwarders for one major version; the rest are listed here with before/after snippets.

## Lifecycle: `Clone()` no longer leaks

Symptom in 7.x: looping `frame.Clone()` or `packet.Clone()` grew native memory unboundedly because the single-pointer ctor short-circuited disposal.

```csharp
// 7.x and earlier: every iteration leaked a full AVFrame + AVBufferRef.
for (int i = 0; i < N; i++)
{
    using var copy = frame.Clone();
}
```

In 8.1.0 this just works. No source change needed.

## Stream ownership: `leaveOpen` defaults to true

Before:
```csharp
using var demuxer = MediaDemuxer.Open(stream);
// 7.x silently closed `stream` on demuxer dispose.
```

After:
```csharp
using var demuxer = MediaDemuxer.Open(stream);        // does NOT close `stream`
using var demuxer = MediaDemuxer.Open(stream, leaveOpen: false); // pre-8.1.0 behaviour
```

Same change applies to `MediaMuxer.Create(Stream, ...)`.

## `IConverter.Convert` returns `int`, not `IEnumerable<MediaFrame>`

Before:
```csharp
foreach (var bgr in swscale.Convert(srcFrame, bgrFrame))
{
    // use bgr
}
```

After:
```csharp
swscale.Convert(srcFrame, bgrFrame); // returns int (0 = waiting for more input, 1 = output ready)
// use bgrFrame directly
```

The old enumerable shape has been removed entirely — `Convert(src, dst)` + using `dst` directly is the only form.

## `Swscale` auto-detects dimension changes

7.x cached `SwsContext` for the first input dimensions. If the demuxer switched to a different resolution mid-stream (or an ABR ladder), subsequent frames were silently garbled. 8.1.0 checks `(srcW, srcH, srcFmt, dstW, dstH, dstFmt)` on every call and rebuilds the context if anything moved.

You no longer need to call `Reset(...)` manually.

## `Swresample` requires options up front + `Flush` for tail samples

Before (7.x):
```csharp
using var swr = new Swresample();
// Calling Convert here returned 0 silently — many users never noticed.
```

After:
```csharp
using var swr = new Swresample(outLayout, outFmt, outRate, inLayout, inFmt, inRate);
swr.Convert(srcFrame, dstFrame);

// Drain at end-of-stream:
foreach (var tail in swr.Flush(frameSize: 1024))
{
    // write tail to encoder
    tail.Dispose();
}
```

For the canonical "decoder frames are variable size, encoder needs fixed `frame_size`" pattern, use the new `AudioResampler`:

```csharp
using var resampler = AudioResampler.For(decoder, encoder);
foreach (var sized in resampler.Convert(decodedFrame))
    sink.WriteAudioFrame(track, sized);
foreach (var tail in resampler.Flush())
    sink.WriteAudioFrame(track, tail);
```

## 14 encoder factory overloads → builder

Before:
```csharp
using var enc = MediaEncoder.CreateVideoEncoder(
    muxer.Format, width, height, 30.0,
    AVPixelFormat.AV_PIX_FMT_YUV420P, bitrate: 4_000_000,
    otherSettings: c => c.Ref.gop_size = 60);
```

After:
```csharp
using var enc = MediaEncoder.Video()
    .OutputFormat(muxer.Format)
    .Size(width, height)
    .Fps(30)
    .PixelFormat(AVPixelFormat.AV_PIX_FMT_YUV420P)
    .Bitrate(4_000_000)
    .Configure(c => c.Ref.gop_size = 60)
    .Build();
```

The 14 legacy `CreateVideoEncoder`/`CreateAudioEncoder` overloads (and the public `CreateHWVideoEncoder`) are **removed** in 8.1.0 — the builder is the only configuration-style entry point. Still available for other scenarios:

- `new MediaEncoder(codec)` + instance `Open(opts)` — the low-level path wrapping `avcodec_open2` (see next section).
- `MediaEncoder.CreateEncoder(codecParameters)` — parameter-driven creation for transcode/remux.
- Hardware encoding via `MediaEncoder.Video().UseHardware(...)` (accepts `AVHWDeviceType`, a device type name like `"cuda"`, or a shared `HWDeviceContext`).

## Lambda factories → ctor + `Open()`

The `Action<MediaCodecContext>`-based factories are removed: `MediaDecoder.Create(codec, beforeOpenSetting, opts)`, `MediaEncoder.Create(codec, beforeOpenSetting, opts)`, the static `MediaCodecContext.Open(codec, beforeOpenSetting, opts)`, and the `Action` parameters on `CreateDecoder`/`CreateEncoder`.

`MediaCodecContext` gains an instance `Open(MediaDictionary opts = null)` wrapping `avcodec_open2`; it returns `this` (typed in the subclasses), so the FFmpeg lifecycle maps to straight-line code: ctor (alloc) → configure → `Open()`.

Before:
```csharp
using var dec = MediaDecoder.CreateDecoder(stream.CodecparRef, ctx =>
{
    ctx.Ref.thread_count = 10;
    ctx.InitHWDeviceContext("d3d11va");
});
```

After:
```csharp
using var dec = new MediaDecoder(MediaCodec.FindDecoder(stream.CodecparRef.codec_id));
dec.SetCodecParameters(ref stream.CodecparRef);
dec.Ref.thread_count = 10;
dec.InitHWDeviceContext("d3d11va");
dec.Open();
```

Unchanged / trivial cases:

- `MediaDecoder.CreateDecoder(stream.CodecparRef)` (no configuration) still works — the lambda-free one-liners stay for LINQ/bulk use.
- `MediaDecoder.Create(codec)` → `new MediaDecoder(codec).Open()`.

Pitfall: `CreateDecoder(codecpar, null, opts)` — passing `null` for the old `Action` parameter — now binds to the `(codecpar, MediaCodec codec, opts)` overload and throws `ArgumentNullException`. Call `CreateDecoder(codecpar, opts)` instead.

`Open()` throws `InvalidOperationException` when no codec is bound to the context or when the context is already open (`IsOpen`).

## Hardware encoder support is no longer an afterthought

7.x: there was no API to wire `hw_device_ctx`/`hw_frames_ctx` on an encoder. Hardware encoding required dropping out of the wrapper and calling FFmpeg.AutoGen directly.

8.1.0:
```csharp
using var hwEnc = MediaEncoder.Video()
    .Codec("h264_nvenc")
    .Size(w, h).Fps(30)
    .UseHardware(AVPixelFormat.AV_PIX_FMT_CUDA, AVPixelFormat.AV_PIX_FMT_NV12,
                 AVHWDeviceType.AV_HWDEVICE_TYPE_CUDA)
    .Build();

// HW → HW transcode: create the device once and share it explicitly
using var cuda = HWDeviceContext.Create(AVHWDeviceType.AV_HWDEVICE_TYPE_CUDA);
hwDecoder.InitHWDeviceContext(cuda); // before hwDecoder.Open()
using var hwEnc2 = MediaEncoder.Video()
    .Codec("h264_nvenc").Size(w, h).Fps(30)
    .UseHardware(AVPixelFormat.AV_PIX_FMT_CUDA, AVPixelFormat.AV_PIX_FMT_NV12, cuda)
    .Build();
```

`MediaCodecContext.InitHWDeviceContext` also gained:
- `fallbackToSw: true` for graceful degradation when the codec doesn't offer the negotiated HW format.
- Recognition of codecs that need `hw_frames_ctx` or `INTERNAL` methods (not just `hw_device_ctx`).
- A real exception when you pass an unknown HW device name (was: silent no-op).
- An overload taking a shared `HWDeviceContext` — the only correct way to share an existing device into a *decoder* (plain `AttachHWDevice` does not run the pixel-format negotiation).

## HW device/frames contexts are typed

Raw `AVBufferRef*` is gone from the HW public surface. Two refcount-owning wrappers replace it
(`Dispose` releases the wrapper's reference; consumers attach by taking their own):

| 7.x / early 8.x                                            | 8.1.0                                                        |
|------------------------------------------------------------|--------------------------------------------------------------|
| `ffmpeg.av_hwdevice_ctx_create(&dev, type, ...)`           | `HWDeviceContext.Create(type, device, opts)`                 |
| hand-rolled `av_hwframe_ctx_alloc` + init                  | `HWFramesContext.Create(device, hwFmt, swFmt, w, h)`         |
| `ctx.AttachHWDevice(AVBufferRef*)` (wired get_format)      | `ctx.AttachHWDevice(HWDeviceContext)` (pure attach)          |
| `ctx.AttachHWFramesContext(AVBufferRef*)`                  | `ctx.AttachHWFramesContext(HWFramesContext)`                 |
| `ctx.GetHWDeviceRef()` / `GetHWFramesRef()` (borrowed)     | `ctx.GetHWDevice()` / `GetHWFrames()` (owning, disposable)   |
| `ctx.IsHWDeviceCtxInit()`                                  | `ctx.HasHWDevice`                                            |
| `builder.UseHardwareDevice(AVBufferRef*)`                  | `builder.UseHardware(hwFmt, swFmt, HWDeviceContext)`         |
| `builder.UseHardwareFrames(AVBufferRef*)`                  | `builder.UseHardwareFrames(HWFramesContext)` (formats derived from the ctx) |
| `graph.SetHWDevice(AVBufferRef*)`                          | `graph.SetHWDevice(HWDeviceContext)`                         |
| `MediaFilterGraph.GetSinkHWFramesCtx(sink)` / `sink.BufferSinkGetHwFramesCtx()` | `sink.GetHWFramesContext()` (single API)  |
| `frame.AllocateOnHWFrames(AVBufferRef*)`                   | `frame.AllocateOnHWFrames(HWFramesContext)`                  |

Device-ctx and frames-ctx can no longer be swapped silently (they used to be the same raw type), and
escape hatches remain via the implicit `AVBufferRef*` conversion on both wrappers.

## `MediaFrame` new HW accessors

```csharp
if (frame.IsHardwareFrame)
{
    using var sw = frame.TransferToSoftware(AVPixelFormat.AV_PIX_FMT_NV12);
    // CPU access on `sw`
}

// Allocate an HW surface from an existing frames context (e.g. when feeding a HW encoder):
using var hwFrames = encoder.GetHWFrames();
frame.AllocateOnHWFrames(hwFrames);
```

## `MediaDictionary` indexer returns null on miss

Before:
```csharp
string v = dict["nonexistent"]; // KeyNotFoundException
```

After:
```csharp
string v = dict["nonexistent"]; // null
if (dict.TryGetValue("k", out var hit)) ...
```

## Parser fixes

`MediaCodecParserContext.ParserPackets` was buggy in 7.x — using a wrong variable name caused a NullReferenceException on the typical call path, and the `Parser2(byte[])` overload returned packets whose data pointed into a managed buffer that had already left the `fixed` scope. Both fixed; the renamed method is `ParsePackets` and the byte-buffer overload now copies into a refcounted packet before returning.

## `DecodePacket` / `EncodeFrame` return allocation-free cursors

The two hot-path methods now return `MediaDecoder.Frames` / `MediaEncoder.Packets` — readonly structs
whose `foreach` compiles against a struct enumerator (zero allocation per call, was one iterator state
machine). Both implement `IEnumerable<T>`, so code that stores the result as an interface or applies
LINQ still works (it boxes). Plain `foreach` consumers need no source change.

Semantic change: the packet/frame is now **sent eagerly at call time**. Previously `SendPacket`/`SendFrame`
only ran on the first `MoveNext` — obtaining the result without enumerating silently dropped the input.
Now not enumerating only delays retrieval; nothing is lost, and send errors throw at the call site.

`MediaDemuxer.ReadFrames` and `MediaMuxer.FlushCodecs` also reuse a single receive frame/packet across
the stream instead of allocating one per packet.

## Enumeration internals consolidated

The per-class iteration plumbing is gone: the public `IntPtrRef` helper class and the `protected static xxx_iterate_safe` / `avcodec_get_hw_config_safe`-style helpers were implementation details and have been removed. Public `GetCodecs()`/`GetFormats()`/`GetFilters()`/`GetParsers()`/`GetHWConfigs()`/`GetProfiles()`/device enumerations are unchanged.

Capability queries now return arrays instead of `IEnumerable<T>` (they always materialized arrays internally): `GetPixelFmts()`, `GetSupportedFramerates()`, `GetSupportedSamplerates()`, `GetChLayouts()` — matching `GetSampleFormats()`. Arrays still implement `IEnumerable<T>` (existing LINQ/foreach code compiles as-is) and gain indexing plus implicit `ReadOnlySpan<T>` conversion.

## Typo cleanup

- `MediaCodec.GetSampelFmts` → `GetSampleFormats` (old name kept as `[Obsolete]`).
- `MediaFilter.GetGetFilters` → `GetFilters` (old name kept as `[Obsolete]`).

## Errors are typed

```csharp
try { ... }
catch (FFmpegException ex)
{
    switch (ex.TypedCode)
    {
        case FFmpegErrorCode.EAGAIN: /* retry */ break;
        case FFmpegErrorCode.EOF:    /* done  */ break;
        default: throw;
    }
}
```

## Compatibility matrix

| FFmpeg.Sharp | FFmpeg.AutoGen   | FFmpeg shared libs | .NET targets                            |
|--------------|------------------|--------------------|-----------------------------------------|
| 7.x          | 7.x.x            | 6.x / 7.x          | netstandard2.0 / netstandard2.1          |
| 8.0.x        | 8.1.0            | 7.x / 8.x          | netstandard2.0 / netstandard2.1          |
| **8.1.0**    | **[8.1.0,9.0.0)**| **7.x / 8.x**      | **netstandard2.0/2.1, net6.0, net8.0**   |
