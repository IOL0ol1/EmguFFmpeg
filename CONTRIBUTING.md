# Contributing to FFmpeg.Sharp

Thank you for considering a contribution!

## Build

```
dotnet restore
dotnet build src/FFmpegSharp.csproj
```

The library targets `netstandard2.0`, `netstandard2.1`, `net6.0`, and `net8.0`.

To run the examples:

```
dotnet run --project example/FFmpegSharp.Example.csproj
```

Note: `example/` references **FFmpeg.GPL** for convenience. Reuse on LGPL-only deployments by switching the `<PackageReference>` to `FFmpeg.LGPL`. The example project also has an `OpenCvSharp4` dependency for the HW decode → JPEG sample.

## Project layout

```
src/
├── Internal/                  Partial classes that hold raw pointer fields + Ref accessors.
│                              Each public wrapper class has a sibling here — keeps the unsafe
│                              guts isolated and lets the public partial focus on API surface.
├── MediaCodec/                Codec, codec context, decoder, encoder, parser, encoder builder.
├── MediaData/                 MediaFrame, MediaPacket — raw field access via `.Ref.snake_case`
│                              (no typed property mirrors; see docs/architecture.md).
├── MediaFilter/               Filter, filter context, filter graph (with HW helpers).
├── MediaFormat/               AVInputFormat / AVOutputFormat wrappers.
├── MediaFrameConverter/       Swscale, Swresample, AudioFifo, AudioResampler, IConverter.
├── MediaMux/                  Demuxer, Muxer, MediaStream, MediaIOContext.
├── Utilities/                 FFmpegLog, FFmpegUtil (image plane / buffer helpers), extensions.
├── FFmpegException.cs         Typed FFmpegException + FFmpegErrorCode enum.
└── MediaDictionary.cs         AVDictionary wrapper (IDictionary<string,string>).
```

## Conventions

- **Disposed default false (owned), `leaveOpen` flips to true.** Any new wrapper class storing a native pointer must default `disposedValue = false` so the wrapper owns the resource. The `(ptr, leaveOpen)` ctor flips it to `true` when the caller retains ownership. Reviewers will reject `private bool disposedValue = true;` — it is the source of multiple historical leaks.
- **No `IConverter.Convert` returning `IEnumerable<>`** — the contract is `int Convert(src, dst)`. Use `IBatchConverter` for fan-out (e.g. `AudioResampler`).
- **No managed exceptions across native frames.** Any native callback (AVIO read/write/seek, get_format, interrupt) must `try/catch` and translate to an AVERROR negative integer.
- **GCHandle.Alloc for delegates passed to FFmpeg.** Field references are necessary but not sufficient — pin via `GCHandle.Alloc(del)` for the lifetime of the native object.
- **`MediaDictionary` is optional.** Wrapper methods that accept one should branch on `options == null` instead of allocating a throwaway dictionary to satisfy `fixed`.
- **Don't add new top-level `using new MediaDictionary()` patterns** — they leak.
- **Comments**: explain the "why" (a hidden constraint, an FFmpeg-API subtlety, a workaround for a specific bug). Don't restate what the code is doing.

## Public API & docs

- `<summary>` + `<param>` for every public member. CI enforces `GenerateDocumentationFile=true`.
- Mark legacy names `[Obsolete]` with a forwarder rather than deleting outright; bump major version only when the forwarder itself is removed.

## Submitting changes

1. Open an issue first for non-trivial changes.
2. Each PR should have a short description focused on the *why*.
3. Run `dotnet build src/FFmpegSharp.csproj` and ensure no new warnings beyond the `NoWarn` list.
4. If you touched HW or conversion paths, run the relevant examples and attach the produced media's metadata.
