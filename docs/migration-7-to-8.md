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

If you really want the old shape, there's a `[Obsolete]` `ConvertEnumerable(src, dst)` shim.

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

The old overloads still compile; they're not yet `[Obsolete]`.

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

// HW → HW transcode: share the decoder's device ref
using var hwEnc2 = MediaEncoder.Video()
    .Codec("h264_nvenc").Size(w, h).Fps(30)
    .UseHardware(AVPixelFormat.AV_PIX_FMT_CUDA, AVPixelFormat.AV_PIX_FMT_NV12,
                 AVHWDeviceType.AV_HWDEVICE_TYPE_CUDA)
    .UseHardwareDevice(hwDecoder.GetHWDeviceRef())
    .Build();
```

`MediaCodecContext.InitHWDeviceContext` also gained:
- `fallbackToSw: true` for graceful degradation when the codec doesn't offer the negotiated HW format.
- Recognition of codecs that need `hw_frames_ctx` or `INTERNAL` methods (not just `hw_device_ctx`).
- A real exception when you pass an unknown HW device name (was: silent no-op).

## `MediaFrame` new HW accessors

```csharp
if (frame.IsHardwareFrame)
{
    using var sw = frame.TransferToSoftware(AVPixelFormat.AV_PIX_FMT_NV12);
    // CPU access on `sw`
}

// Allocate an HW surface from an existing frames context (e.g. when feeding a HW encoder):
frame.AllocateOnHWFrames(encoder.GetHWFramesRef());
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
