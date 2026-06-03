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
The shortest possible path uses the new `MediaSink` one-stop API — it owns the muxer + encoder(s), auto-assigns pts, flushes encoders, and writes the trailer on dispose.

```csharp
const string output = "out.mp4";
using var sink = MediaSink.Create(output);
int v = sink.AddVideo(MediaEncoder.Video()
    .OutputFormat(sink.Muxer.Format)
    .Size(800, 600)
    .Fps(29.97)
    .Configure(c => c.Ref.thread_count = 10)
    .Build());
sink.Start();

using var frame = MediaFrame.CreateVideoFrame(800, 600, AVPixelFormat.AV_PIX_FMT_YUV420P);
for (int i = 0; i < 300; i++)
{
    // ... fill frame.Ref.data[plane] ...
    sink.WriteVideoFrame(v, frame); // pts auto-assigned
}
// sink.Dispose() flushes the encoder and writes the trailer.
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

using var hwDecoder = MediaDecoder.CreateDecoder(demuxer[vi].CodecparRef, ctx =>
{
    ctx.Ref.thread_count = 10;
    ctx.InitHWDeviceContext("d3d11va"); // or "cuda", "qsv", "vaapi", ...
});

using var pkt = new MediaPacket();
using var recv = new MediaFrame();
using var sw   = new MediaFrame(); // optional — omit to keep zero-copy GPU surface

foreach (var p in demuxer.ReadPackets(pkt))
{
    if (p.StreamIndex != vi) continue;
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

using var hwDecoder = MediaDecoder.CreateDecoder(demuxer[vi].CodecparRef,
    ctx => ctx.InitHWDeviceContext(AVHWDeviceType.AV_HWDEVICE_TYPE_CUDA));

using var hwEncoder = MediaEncoder.Video()
    .Codec("h264_nvenc")
    .Size(demuxer[vi].CodecparRef.width, demuxer[vi].CodecparRef.height)
    .Fps(30)
    .UseHardware(AVPixelFormat.AV_PIX_FMT_CUDA, AVPixelFormat.AV_PIX_FMT_NV12,
                 AVHWDeviceType.AV_HWDEVICE_TYPE_CUDA)
    .UseHardwareDevice(hwDecoder.GetHWDeviceRef()) // share the same CUDA context
    .Bitrate(4_000_000)
    .Build();
```

### Audio resample to encoder.frame_size
```csharp
using var resampler = AudioResampler.For(audioDecoder, audioEncoder);

foreach (var (_, decoded) in demuxer.ReadFrames(audioDecoders, AVMediaType.AVMEDIA_TYPE_AUDIO))
{
    foreach (var fixedFrame in resampler.Convert(decoded))
    {
        sink.WriteAudioFrame(audioTrack, fixedFrame);
        fixedFrame.Dispose();
    }
}
foreach (var tail in resampler.Flush()) // drain
{
    sink.WriteAudioFrame(audioTrack, tail);
    tail.Dispose();
}
```

More: **[example/](./example)**.

## Breaking changes in 8.1.0

This release is a heavyweight cleanup driven by an audit (see `docs/migration-7-to-8.md` for full details and before/after snippets).

Highlights:
- `MediaFrame.Clone()` / `MediaPacket.Clone()` no longer leak (`disposedValue` default flipped).
- `MediaDemuxer.Open(Stream)` / `MediaMuxer.Create(Stream)` no longer close your stream by default — pass `leaveOpen: false` to opt in.
- `MediaIOContext` callbacks catch managed exceptions and surface them as `IOException` on the next managed call (no more crashes from network blips).
- `MediaDemuxer.ReadPackets` no longer yields a ghost packet at EOF; pair with `ReadPacketsCloned()` for safe enqueuing.
- `MediaCodecParserContext.ParserPackets` — fixed NRE and dangling-pointer-on-byte[] bug.
- `MediaEncoder.EncodeFrame` no longer calls `av_frame_make_writable` in `finally` (you can call `MediaEncoder.MakeWritable(frame)` yourself before reuse).
- New builder: `MediaEncoder.Video()` / `MediaEncoder.Audio()` replaces the 14 legacy CreateXxxEncoder overloads.
- New hardware encoder path: `MediaEncoder.CreateHWVideoEncoder(...)` + `MediaCodecContext.AttachHWDevice/AttachHWFramesContext`.
- New `MediaFrame.IsHardwareFrame` / `TransferToSoftware` / `AllocateOnHWFrames`.
- New `MediaSink`, `AudioResampler`, `Swscale.Options`, `Swresample.Flush(...)`.
- `IConverter.Convert` now returns `int` (frames written), not `IEnumerable<MediaFrame>`. The old enumerable behaviour is available via `Swscale.ConvertEnumerable` marked `[Obsolete]`.
- Typo fixes: `MediaCodec.GetSampelFmts` → `GetSampleFormats`, `MediaFilter.GetGetFilters` → `GetFilters` (old names kept as `[Obsolete]` forwarders).
- `MediaDictionary` indexer returns `null` on miss instead of throwing.
- PascalCase shortcuts on `MediaFrame` / `MediaPacket` / `MediaStream` (`Width`, `Height`, `Pts`, `StreamIndex`, `Format`, ...). The `.Ref.snake_case` escape hatch is still available.

## Troubleshooting

**`DllNotFoundException: avformat-XX.dll`** — FFmpeg.AutoGen does not ship native binaries. Either install `FFmpeg.GPL` / `FFmpeg.LGPL` NuGets, or set the loader's search root before any FFmpeg call:
```csharp
ffmpeg.RootPath = @"C:\path\to\ffmpeg\bin";
// or AppDomain.CurrentDomain.BaseDirectory, or any folder that contains avcodec/avformat/avutil/swscale/swresample DLLs
```

**Version mismatch** — this library tracks FFmpeg shared libraries 7.x / 8.x (corresponding FFmpeg.AutoGen versions 7.x / 8.x). FFmpeg 6.x or earlier are not supported.

**`AV_DICT_DONT_STRDUP_KEY` / `AV_DICT_DONT_STRDUP_VAL`** — these flags transfer ownership of an av_malloc'd buffer to the dictionary, which the managed wrapper cannot do safely. They are marked `[Obsolete(error=true)]`.

**HW decode falls back to software silently** — pass `fallbackToSw: false` (the default) to `InitHWDeviceContext` to make this fail instead. Use `fallbackToSw: true` to opt into the graceful fallback.

**HW encode `EINVAL` on first frame** — feed frames whose `format` matches the encoder's `hwPixelFormat`, allocated with `MediaFrame.AllocateOnHWFrames(encoder.GetHWFramesRef())` instead of `AllocateBuffer()`.

**Stream gets unexpectedly closed** — `MediaDemuxer.Open(Stream)` / `MediaMuxer.Create(Stream)` default to `leaveOpen: true` since 8.1.0, but if you upgraded from 7.x your old call sites may still be wiring the wrapper's lifecycle to your stream. Inspect the third (boolean) argument.

## Roadmap
- Easy API for cut/seek/mute audio clip.
- Easy API for cut/seek video clip.
- More examples and tests.
- Filter graph parser (`avfilter_graph_parse2`).
- Subtitle support.
- Async/IAsyncEnumerable surface for encode/mux (read side is done).

## Related
- [FFmpeg.AutoGen](https://github.com/Ruslan-B/FFmpeg.AutoGen) — the underlying P/Invoke bindings.
- [FFmpeg API documentation](https://ffmpeg.org/doxygen/trunk/index.html).

## License
This project is licensed under the MIT license.

If you use FFmpeg builds licensed under the GPL, that license is contagious.
