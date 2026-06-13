# Architecture

This document describes the layering of the FFmpeg.Sharp wrappers and the conventions they rely on. It is the reference for "how do I add a new wrapper class?" — anything you write should follow these patterns or have a comment explaining why not.

## The two-partial pattern

Every wrapper that holds a native pointer (`MediaFrame`, `MediaPacket`, `MediaCodec`, `MediaCodecContext`, `MediaFormatContext`, `MediaStream`, `MediaInputFormat`, `MediaOutputFormat`, `MediaFilter*`) is split across two files:

- `src/Internal/Xxx.cs` — `partial class Xxx`: holds the raw `AVXxx*` field, the `(AVXxx*)` / `(IntPtr)` ctors, the implicit operator to the raw pointer, and the `Ref` property. Nothing else.
- `src/<Subsystem>/Xxx.cs` — `partial class Xxx`: API surface, `IDisposable` implementation, factories, ergonomics.

Rationale: the unsafe pointer manipulation is small, mechanical, and reviewable as a single concept. The API surface evolves a lot; keeping it visually separated makes the diffs intelligible. The Internal partials are also where `Ref => ref *pXxx` lives, which is the escape hatch into raw struct fields.

### `Ref` is the field-access surface — and it is a `ref` return

There is no typed-property layer mirroring `AVXxx` fields (PascalCase partials were removed in 8.x); `wrapper.Ref.field` is the one way to read or write raw struct fields. Because `Ref` returns `ref AVXxx`, access composes directly onto native memory — but only when the expression stays a `ref`:

```csharp
pkt.Ref.pts = 100;              // OK: writes native memory
ref var r = ref pkt.Ref;        // OK: alias; r.pts = 100 writes native memory
var copy = pkt.Ref;             // WRONG for writes: silently copies the whole struct;
                                //   copy.pts = 100 mutates the copy, not the packet
```

The compiler does not warn about the third form. When you need a local, take it with `ref var x = ref ...`.

## Ownership: `disposedValue` is the source of truth

Every wrapper has a `private bool disposedValue;` field (default `false` = owned). The pointer-only ctor (the one declared in `Internal/`) does NOT touch it, so the wrapper takes ownership by default. The `(ptr, bool leaveOpen)` ctor explicitly assigns `disposedValue = leaveOpen` — when true, dispose is a no-op and the caller retains ownership.

This is the inverse of an early version of the library which initialised the field to `true` and relied on every ctor to flip it off. That semantics caused four separate native-resource leaks (one per `Clone()` method, two from raw-pointer factory paths). The current default forbids that class of bug.

## Lifetime of delegates and callbacks

C# delegates passed to FFmpeg via function pointers (`avio_alloc_context`, `avformat_context.interrupt_callback`, `avcodec_context.get_format`) must remain reachable from managed code for the lifetime of the native object that references them. Field references alone are not sufficient — the JIT may elide reads. Wrappers pin via `GCHandle.Alloc(del)` and free the handle in `Dispose`.

## Native exceptions must not cross the boundary

CoreCLR considers a managed exception propagating into a native frame undefined behaviour. Every C# callback installed via FFmpeg's function-pointer hooks wraps its body in `try { ... } catch (Exception ex) { ... return AVERROR_*; }`. The exception is cached on the wrapper and re-thrown from the next managed-side call. See `MediaIOContext` for the canonical implementation.

## `IConverter` vs `IBatchConverter`

- `IConverter.Convert(src, dst) -> int` — single-frame, no allocation. Returns 0 when the converter is buffering and 1 when `dst` contains valid output. Implemented by `Swscale` and `Swresample`.
- `IBatchConverter.Convert(src) -> IEnumerable<MediaFrame>` — fan-out (0..n out per 1 in). Implemented by `AudioResampler` because variable→fixed audio resampling produces a different number of frames per input.

There is no enumerable-yielding shim — the old `ConvertEnumerable(src, dst)` was removed in 8.x; call `Convert(src, dst)` and use `dst` directly.

## Threading

FFmpeg's `AVCodecContext`, `AVFormatContext`, etc. are NOT thread-safe. The wrappers do not add synchronization. The conventional pattern is:

- One thread does demux + decode (sequentially), pushing frames into a `Channel<MediaFrame>`.
- A second thread reads frames out of the channel and encodes / writes.
- Pass frames as **cloned** packets (`MediaPacket.Clone`) — yielded packets share storage with the demuxer iterator and are unrefed on the next pull.

## C# language version constraints

The project pins `LangVersion=latest`. The major constraints that drove past hacks (now obsolete):

- C# 7.3/8.0 disallow unsafe code inside iterator (`yield`) bodies. Workarounds factor the unsafe access into a helper method called from the iterator.
- C# disallows `await` inside `unsafe` methods. Any future async surface must mark only its inner helpers as unsafe and keep the awaiting method outside.

## Where to put what

| Question                                          | Place it in                                                                 |
|---------------------------------------------------|-----------------------------------------------------------------------------|
| New raw FFmpeg field accessor                     | Nowhere — use `wrapper.Ref.field` (see the escape hatch section above)      |
| New convenience factory for an existing wrapper   | The non-internal partial of the wrapper                                     |
| Cross-wrapper helper                              | `src/Utilities/FFmpegUtil.cs`                                               |
| New async / cancellation surface                  | Not in the library — async/threading orchestration is user-land (see Threading) |
| New high-level pipeline class (`AudioResampler`-style) | `src/<subsystem>/` as its own file                                     |
