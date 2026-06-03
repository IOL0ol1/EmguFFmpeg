export const meta = {
  name: 'ffmpeg-sharp-deep-audit',
  description: 'Deep multi-dimension audit of FFmpeg.Sharp (EmguFFmpeg) for performance, correctness, and API ergonomics — produces a prioritized rectification list.',
  whenToUse: 'Comprehensive code audit for FFmpeg.Sharp wrapper library, with emphasis on hardware acceleration, format conversion, lifecycle/safety, and API usability.',
  phases: [
    { title: 'Survey', detail: 'parallel dimension-specific finders read source and produce findings' },
    { title: 'Verify', detail: 'adversarial skeptics try to refute each finding' },
    { title: 'Synthesize', detail: 'aggregate, deduplicate, prioritize into rectification list' },
  ],
}

const REPO = 'e:/Projects/EmguFFmpeg'

const FINDING_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  required: ['dimension', 'findings'],
  properties: {
    dimension: { type: 'string' },
    findings: {
      type: 'array',
      items: {
        type: 'object',
        additionalProperties: false,
        required: ['id', 'title', 'category', 'severity', 'file', 'evidence', 'impact', 'recommendation'],
        properties: {
          id: { type: 'string' },
          title: { type: 'string' },
          category: { type: 'string', enum: ['Correctness/Bug', 'MemorySafety', 'Performance', 'API-Ergonomics', 'Resource-Lifecycle', 'Threading', 'Missing-Feature', 'Maintenance/Tech-Debt'] },
          severity: { type: 'string', enum: ['Critical', 'High', 'Medium', 'Low'] },
          file: { type: 'string' },
          evidence: { type: 'string' },
          impact: { type: 'string' },
          recommendation: { type: 'string' },
        },
      },
    },
  },
}

const VERDICT_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  required: ['id', 'isReal', 'confidence', 'reasoning', 'severityAdjustment', 'adjustedSeverity'],
  properties: {
    id: { type: 'string' },
    isReal: { type: 'boolean' },
    confidence: { type: 'string', enum: ['High', 'Medium', 'Low'] },
    reasoning: { type: 'string' },
    severityAdjustment: { type: 'string', enum: ['Keep', 'Raise', 'Lower'] },
    adjustedSeverity: { type: 'string', enum: ['Critical', 'High', 'Medium', 'Low', 'NA'] },
  },
}

phase('Survey')

const DIMENSIONS = [
  {
    key: 'HW-Accel',
    label: 'survey:hw-accel',
    prompt: [
      'You audit hardware-acceleration handling of an FFmpeg C# wrapper library (FFmpeg.Sharp). The owner has flagged HW acceleration as not perfect — be aggressive in finding problems.',
      '',
      'Read these files in ' + REPO + ':',
      '- src/MediaCodec/MediaCodecContext.cs (especially InitHWDeviceContext and GetFormat)',
      '- src/MediaCodec/MediaDecoder.cs (DecodePacket and the HW transfer path)',
      '- src/MediaCodec/MediaCodec.cs (GetHWConfigs, GetPixelFmts)',
      '- example/HWDecode.cs (real-world usage pattern)',
      '- src/MediaCodec/MediaEncoder.cs (HW encoder path — note it does NOT exist; this is itself a finding)',
      '',
      'Look hard for:',
      '1. Closure / GC issues with the get_format callback (GetFormatFunc field — kept alive correctly? what if user replaces codec? leaked delegates?).',
      '2. The hw_device_ctx is set without av_buffer_ref / refcounting — freed correctly?',
      '3. The forced sw download in DecodePacket: any way to keep the GPU frame (zero-copy) for downstream filters / encoder / D3D11 interop? Current API has no way to return the hw frame directly.',
      '4. HW encoding: no API to create hw_frames_ctx for encoders, set hw frame format, or feed GPU surfaces to encoder. CreateVideoEncoder has no hw-aware overload.',
      '5. HW filter graph: filter graph has no helper for hwupload/hwdownload or buffersink_get_hw_frames_ctx.',
      '6. Transfer flags / AV_HWFRAME_TRANSFER_DIRECTION / preferred format negotiation handled?',
      '7. CopyProps overwrites format/width/height of sw_frame — does it defeat user pre-allocation? hwframe_transfer_data also allocates dst buffer itself on first call.',
      '8. Reuse of inFrame/swFrame across iterations — Unref of _frame is gated on ret>0 but ReceiveFrame on success returns 0, so Unref may never run; investigate.',
      '9. Thread-safety: get_format callback fires on decode thread; captured hWConfig.pix_fmt is per-call but delegate field is instance-level — re-init scenarios?',
      '10. InitHWDeviceContext early-return when type==AV_HWDEVICE_TYPE_NONE: comparison gates whole block, so passing NONE silently does nothing.',
      '11. HW codec lookup: GetHWConfigs picks the FIRST matching device_type, ignoring configs that need hw_frames_ctx (frames context) but not hw_device_ctx. Code masks AV_CODEC_HW_CONFIG_METHOD_HW_DEVICE_CTX only — what about codecs needing frames ctx?',
      '12. HWDecode.cs example lacks proper teardown of the device context and Write() leaks the av_malloc-d buffer.',
      '',
      'Return ONLY HW-acceleration findings. Be specific with file:lines and concrete fix. Aim for 6-15 findings.',
    ].join('\n'),
  },
  {
    key: 'Conversion',
    label: 'survey:conversion',
    prompt: [
      'You audit format conversion (Swscale + Swresample + AudioFifo) of FFmpeg.Sharp. The owner said format conversion is not direct enough — find every awkwardness and inefficiency.',
      '',
      'Read in ' + REPO + ':',
      '- src/MediaFrameConverter/Swscale.cs',
      '- src/MediaFrameConverter/Swresample.cs',
      '- src/MediaFrameConverter/AudioFifo.cs',
      '- src/MediaFrameConverter/IConverter.cs',
      '- src/MediaData/MediaFrame.cs (CreateVideoFrame / CreateAudioFrame; user must call these and AllocateBuffer manually)',
      '- example/HWDecode.cs (awkward allocate-dst-frame, then convert pattern)',
      '- README.md (breaking change requiring pre-allocated dst frame)',
      '',
      'Look for:',
      '1. Swscale.Convert returns IEnumerable<MediaFrame> with a single element — misleading; suggests multi-frame output. IConverter forces this awkward shape.',
      '2. Swscale lazy Reset uses src.w/h/fmt + dst.w/h/fmt — if a later frame has different dims, cached SwsContext silently produces wrong output. No sws_getCachedContext-style caching.',
      '3. Swscale.Reset frees pContext then realloc without nulling first — if sws_getContext returns null the field is briefly invalid.',
      '4. No sws_scale per-slice / cropping support, no SwsContext options beyond bilinear default.',
      '5. Swresample default ctor allocates context via swr_alloc but does NOT set options — calling Convert returns immediately or errors. Misleading.',
      '6. Swresample.Convert ignores 0-output case and does not drain residual samples on EOF (no flush API).',
      '7. Swresample lacks AudioFifo helper that the standard sample-conversion workflow needs (variable frame_size to fixed encoder frame_size). Manual wiring required.',
      '8. AudioFifo.Add reallocs via av_audio_fifo_realloc with Size+nbSamples — verify call shape (it is correct, but API is confusing).',
      '9. AudioFifo exposes void** raw pointers — no managed-friendly overload to feed a MediaFrame.extended_data.',
      '10. No high-level VideoConverter that takes input MediaFrame + target (w,h,pixfmt) and returns a fresh allocated MediaFrame.',
      '11. No high-level Resampler that takes input frames and yields fixed-size output frames (encoder needs constant nb_samples pattern).',
      '12. Swscale ctor that takes only SwsContext* does not validate non-null; null context NREs on first Convert.',
      '13. Performance: Swscale.Convert always calls av_frame_copy_props per call.',
      '14. No async / IAsyncEnumerable conversion overloads.',
      '',
      'Return 6-15 findings. Be very specific about API shape changes.',
    ].join('\n'),
  },
  {
    key: 'Codec-Lifecycle',
    label: 'survey:codec',
    prompt: [
      'Audit codec lifecycle, encode/decode loop correctness, parser of FFmpeg.Sharp.',
      '',
      'Read in ' + REPO + ':',
      '- src/MediaCodec/MediaCodecContext.cs',
      '- src/MediaCodec/MediaDecoder.cs (DecodePacket — examine yield/unref logic)',
      '- src/MediaCodec/MediaEncoder.cs (EncodeFrame — MakeWritable timing, FlushCodecs in Muxer)',
      '- src/MediaCodec/MediaCodecParserContext.cs (Parser2 — note packet.Ref.size referenced when variable name is pkt)',
      '- src/MediaMux/MediaMuxer.cs (FlushCodecs)',
      '',
      'Look for:',
      '1. MediaDecoder.DecodePacket: finally if (ret > 0) _frame.Unref(); — avcodec_receive_frame returns 0 on success, never >0. So _frame is never unrefed between iterations. Misleading.',
      '2. MediaDecoder hw transfer path returns _swframe — verify metadata copy semantics.',
      '3. MediaDecoder.DecodePacket: if (ret < 0 && ret != EAGAIN && ret != EOF) ret.ThrowIfError(); — but EAGAIN on send means decoder buffer full; the contract for SendPacket(EAGAIN) requires a drain loop. Currently it falls through into receive — silent corruption potential.',
      '4. MediaEncoder.EncodeFrame: MakeWritable(frame) is called in finally — av_frame_make_writable AFTER encoding wastes a deep copy and is wrong placement.',
      '5. MediaEncoder.EncodeFrame: SendFrame returns EAGAIN -> yield break, but caller cannot tell whether the frame was consumed.',
      '6. MediaCodecContext.Dispose: disposedValue starts as true (line 11). It is set to false only via the (codec) ctor that chains. But the (pAVCodecContext, leaveOpen) ctor sets disposedValue=leaveOpen — leaveOpen=true means do not dispose, but the field initial true makes the logic invert. Audit carefully.',
      '7. MediaCodecParserContext.ParserPackets: if (packet.Ref.size > 0) uses parameter packet (which may be null when caller passes null) instead of pkt local. NullReferenceException bug.',
      '8. MediaCodecParserContext.Parser2 byte[] overload: pbuf fixed inside the function but av_parser_parse2 stores a pointer into the input buffer in packet->data — pointer dangling when fixed scope exits.',
      '9. AVCodecContext_get_format delegate (GetFormatFunc): assigned only once per InitHWDeviceContext call. If called twice the new delegate replaces the old; GC of old delegate while still referenced by pCodecContext->get_format risks UAF.',
      '10. Encoder factories duplicate ~150 lines of boilerplate (8 video, 6 audio overloads); a builder pattern would replace.',
      '11. CreateVideoEncoder uses format.Ref.video_codec but if format is null this NREs.',
      '12. Decoder enumerator on flush: hw transfer path still runs after EOF; hw_frames_ctx may be torn down already.',
      '13. AVCodecContext.thread_count is set via callback but thread_type (FRAME vs SLICE) is not exposed — most users want auto.',
      '14. No way to check encoder/decoder open state (avcodec_is_open). Re-open accidents possible.',
      '',
      'Return 6-15 findings. Specific recommendations.',
    ].join('\n'),
  },
  {
    key: 'MuxDemux-IO',
    label: 'survey:muxdemux',
    prompt: [
      'Audit muxer/demuxer/IO layer of FFmpeg.Sharp.',
      '',
      'Read in ' + REPO + ':',
      '- src/MediaMux/MediaFormatContext.cs',
      '- src/MediaMux/MediaDemuxer.cs',
      '- src/MediaMux/MediaMuxer.cs',
      '- src/MediaMux/MediaIOContext.cs',
      '- src/MediaMux/MediaStream.cs',
      '',
      'Look for:',
      '1. MediaDemuxer.ReadPackets: do { ret = ReadPacketSafe(packet); ... yield return packet; } while (ret >= 0) — when ret == AVERROR_EOF the packet is still yielded WITH empty/invalid data. Caller cannot tell EOF from a real packet.',
      '2. yield return inside try/finally with Unref — Unref happens on next MoveNext; but on ret<0 fall-through to ThrowIfError throws AFTER yielding — consumer already saw the broken packet.',
      '3. MediaDemuxer.FindBestStream: codec parameter logic is buggy — if (codec != null) return ret — but codec is null on entry typically, so always falls through. Intent unclear.',
      '4. MediaDictionary leak pattern in Open / FindStreamInfo / WriteHeader / Codec.Open / Decoder.Create / Encoder.Create — tmp = options ?? new MediaDictionary() and the new MediaDictionary() never gets disposed -> leaks AVDictionary*.',
      '5. MediaIOContext: read/write/seek delegates kept alive only while MediaIOContext alive — good. But _buffer (av_malloc) is owned by avio_alloc_context per FFmpeg docs; if avio_alloc_context fails after av_malloc succeeds, _buffer leaks.',
      '6. MediaIOContext.Read returns AVERROR_EOF when count==0 — good. Write does not return AVERROR if stream.Write throws — exception will propagate through native frame, undefined behavior.',
      '7. MediaIOContext: avio_closep frees the internal buffer; if user wrote and the internal buffer was reallocated, the original pointer may differ — verify.',
      '8. MediaMuxer dispose: writes trailer in dispose if (hasWriteHeader && !hasWriteTrailer) silently — if trailer write fails the exception is swallowed.',
      '9. MediaMuxer.Create(Stream, MediaOutputFormat) requires MediaOutputFormat — but more common entry points are by filename or by format name string. Add overloads.',
      '10. MediaMuxer.AddStream(MediaEncoder) copies time_base from encoder, but encoder time_base for audio is often 1/sample_rate while stream may need explicit time_base — verify.',
      '11. MediaMuxer.WritePacket: calls av_packet_rescale_ts only if codecTimeBase != null. Most users will not pass it. Document or default.',
      '12. MediaDemuxer: no async API — av_read_frame blocks IO thread.',
      '13. No ReadFrames convenience that yields decoded MediaFrame per stream.',
      '14. MediaStream has only CodecparRef and timespan conversions — missing: side data, attached pic, disposition, language tag accessors.',
      '15. Disposing the demuxer _ioContext disposes the user-passed Stream — closed-stream surprise. Add leaveOpen flag.',
      '',
      'Return 6-15 findings.',
    ].join('\n'),
  },
  {
    key: 'Frame-Packet',
    label: 'survey:frame-packet',
    prompt: [
      'Audit MediaFrame and MediaPacket — hot path of any media app.',
      '',
      'Read in ' + REPO + ':',
      '- src/MediaData/MediaFrame.cs (GetBytes, GetData, GetVideoData, GetAudioData)',
      '- src/MediaData/MediaPacket.cs',
      '- src/Internal/MediaFrame.cs',
      '- src/Internal/MediaPacket.cs',
      '- src/Utilities/FFmpegUtil.cs',
      '',
      'Look for:',
      '1. MediaFrame.GetBytes(bool padding=true) — returns new byte[] every call.',
      '2. MediaFrame.GetData() returns byte[][] per call (allocates planes-many arrays).',
      '3. ComputeVideoSize and CopyVideoBytes: when (height % chroma_h) != 0, use of av_image_fill_plane_sizes / av_image_copy_to_buffer would be safer than hand-rolling.',
      '4. CopyVideoBytes / CopyAudioBytes throw ArgumentException after partial writes if buffer is too small. Validate size upfront.',
      '5. GetBytesSize for HW frames throws — fine, but no helper to download HW frame to RAM and call GetBytes.',
      '6. MediaFrame.Clone calls av_frame_clone returning a new AVFrame*. The wrapping MediaFrame goes through MediaFrame(AVFrame*) which does NOT set disposedValue, so disposedValue stays at initial true -> av_frame_free never called -> leak per Clone.',
      '7. Same lifecycle hazard for MediaPacket: new MediaPacket(av_packet_clone(this)) -> MediaPacket(AVPacket*) does NOT update disposedValue (initial true) -> never frees.',
      '8. MediaFrame.AllocateBuffer does not validate width/height/format set first — calling on empty frame returns nonsense or errors.',
      '9. MediaFrame.CreateAudioFrame does not set sample_rate by default (sampleRate=0). Downstream may break.',
      '10. MediaFrame has no Ref-based property to read AVFrame.opaque, side_data, etc.',
      '11. MediaPacket has no factory like MediaPacket.FromBuffer(Span<byte>) — common need (feed externally-sourced compressed data).',
      '12. FFmpegUtil.CopyPlane is the only utility; missing helpers for av_image_alloc, av_image_fill_arrays, av_image_copy_to_buffer.',
      '',
      'Return 6-15 findings. Clone leak should be Critical severity.',
    ].join('\n'),
  },
  {
    key: 'API-Ergonomics',
    label: 'survey:ergonomics',
    prompt: [
      'Audit API ergonomics. Step into a new user shoes — what is awkward, surprising, or requires too much code?',
      '',
      'Read in ' + REPO + ':',
      '- README.md (canonical examples)',
      '- example/Program.cs and example/*.cs (real usage samples)',
      '- src/MediaCodec/MediaEncoder.cs (count overload explosion)',
      '- src/MediaCodec/MediaDecoder.cs',
      '- src/MediaMux/MediaMuxer.cs',
      '- src/MediaFrameConverter/*.cs',
      '',
      'Look for:',
      '1. The Ref.snake_case idiom: zero-copy is great, but every property leaks snake_case FFmpeg naming into idiomatic C#. Provide curated wrapper properties for top-20 fields with PascalCase + XML doc, keep Ref as escape hatch.',
      '2. CreateVideoEncoder has 8 overloads, audio has 6 — combinatorial explosion. Replace with builder.',
      '3. The demux to decode pattern in README requires manual decoder list, packet-to-decoder match by stream_index, null-decoder handling, and FlushCodecs at end. Provide TranscodePipeline / DecodedFrameReader.',
      '4. Swscale/Swresample setup verbose: size dst MediaFrame, AllocateBuffer, remember Reset on dim change.',
      '5. MediaDictionary appears to be options bag — verify it is user-friendly and Disposable in conventional patterns.',
      '6. Examples use base classes (ExampleBase) inconsistently — Stream-based vs url-based. Document.',
      '7. No async APIs anywhere.',
      '8. No IAsyncEnumerable<MediaFrame>/IAsyncEnumerable<MediaPacket> for streaming scenarios.',
      '9. No Span<T>-based audio/video read API exposed.',
      '10. Error model: FFmpegException carries int code. Provide enum for common errors and TryXxx pattern.',
      '11. Naming inconsistencies: GetSampelFmts (typo of Sample), GetGetFilters (double Get), Initialize vs Init (filter graph).',
      '12. Implicit operators (Swscale->SwsContext*, MediaCodec->AVCodec*) make compile-time misuse silently work.',
      '13. No PrintFormat / DumpFormat helper convenience.',
      '14. README quick-start is a 30-line snippet with manual pts assignment, packet flushing, codec time_base passing — should be a 2-line job in a modern wrapper.',
      '',
      'Return 6-15 findings, especially focused on what surprises a new user.',
    ].join('\n'),
  },
  {
    key: 'Memory-Lifecycle',
    label: 'survey:memory',
    prompt: [
      'Audit memory safety, dispose patterns, native resource lifecycles. C# wraps unsafe pointers — get this wrong and you crash or leak.',
      '',
      'Read in ' + REPO + ':',
      '- src/MediaCodec/MediaCodecContext.cs (Dispose with disposedValue=true initial)',
      '- src/MediaData/MediaFrame.cs (Dispose, disposedValue=true initial)',
      '- src/MediaData/MediaPacket.cs (Dispose, disposedValue=true initial)',
      '- src/MediaMux/MediaFormatContext.cs (Dispose)',
      '- src/MediaMux/MediaIOContext.cs (Dispose, owned buffer, delegate lifetime)',
      '- src/MediaMux/MediaDemuxer.cs (Dispose override)',
      '- src/MediaMux/MediaMuxer.cs (Dispose override — writes trailer in finalizer-reachable path)',
      '- src/MediaFrameConverter/Swscale.cs',
      '- src/MediaFrameConverter/Swresample.cs',
      '- src/MediaFilter/MediaFilterGraph.cs (Dispose method possibly not override)',
      '',
      'Look for:',
      '1. disposedValue=true initial in MediaCodecContext / MediaFrame / MediaPacket / MediaFormatContext — any ctor that does NOT set it false -> never freed. Trace every ctor.',
      '2. MediaFrame(AVFrame*) ctor (Internal partial) does not touch disposedValue. Public ctor chain uses (AVFrame*, leaveOpen) which does -> OK only via that path. Other paths (Clone) leak.',
      '3. Same for MediaCodecContext, MediaPacket, MediaFormatContext.',
      '4. MediaIOContext: stream field stored; on Dispose stream?.Dispose() closes user stream — surprising. Need leaveOpen flag.',
      '5. MediaFilterGraph.Dispose declared but not override — parent class likely IDisposable separately. Review.',
      '6. MediaDemuxer.Dispose ordering: base.Dispose inside avformat_close_input expects pb still set in some FFmpeg versions — verify ordering.',
      '7. MediaMuxer.Dispose: writes trailer even on error path; if WriteHeader succeeded but encoding failed, writing trailer can crash. Wrap try/catch.',
      '8. AVCodecContext_get_format delegate (GetFormatFunc field): function pointer derived from .NET delegate; must outlive codec context. If user reassigns or it is freed by avcodec_free_context first, delegate could be GC-d.',
      '9. MediaCodecParserContext.Dispose: av_parser_close without nulling. Double-Dispose double-free risk.',
      '10. MediaDictionary leak across many entry points (already noted) — collect all sites.',
      '11. Implicit operators returning raw pointers hide pointer aliasing -> use-after-free across managed/native boundary. Consider SafeHandle.',
      '12. Lack of SafeHandle (CriticalFinalizerObject) for FFmpeg pointers — finalizer races with dispose.',
      '13. AudioFifo.Dispose does not null pAudioFifo; subsequent ops would NRE.',
      '14. Swresample.Dispose: fixed (SwrContext** ppSwrContext = &pSwrContext) inside Dispose — swr_free already nulls it; harmless but verify.',
      '15. Swscale.Reset: frees pContext then immediately overwrites — if sws_getContext returns null the field is null; next Dispose double-frees? sws_freeContext(NULL) is safe but verify.',
      '',
      'Return 6-15 findings. Memory-safety bugs tagged Critical.',
    ].join('\n'),
  },
  {
    key: 'Performance',
    label: 'survey:perf',
    prompt: [
      'Audit performance hotpaths. Look for unnecessary allocations, copies, P/Invoke overhead, threading model gaps.',
      '',
      'Read in ' + REPO + ':',
      '- src/MediaData/MediaFrame.cs (GetVideoData, GetBytes — allocations)',
      '- src/MediaCodec/MediaDecoder.cs',
      '- src/MediaCodec/MediaEncoder.cs',
      '- src/MediaMux/MediaDemuxer.cs',
      '- src/MediaFrameConverter/Swscale.cs',
      '- src/MediaMux/MediaIOContext.cs (per-Read marshaling allocation under NETSTANDARD2_0)',
      '- src/MediaCodec/MediaCodec.cs',
      '- src/MediaCodec/MediaCodecParserContext.cs (per-call 20KB+64 byte[] allocation)',
      '',
      'Look for:',
      '1. MediaCodecParserContext.ParserPackets allocates 20544 bytes EVERY call — should be stackalloc or pooled.',
      '2. MediaIOContext NETSTANDARD2_0 path: per-Read/Write byte[] allocation (often called every 32KB) — massive GC pressure for streaming. Use ArrayPool.',
      '3. MediaFrame.GetData / GetBytes allocate per call.',
      '4. yield return iterators capture state machine — minor allocation per ReadPackets / DecodePacket. Provide a TryGetNext low-allocation API.',
      '5. New MediaCodec(pCodec) allocates per call in GetCodecs / GetCodec / FindBestStream — minor GC pressure.',
      '6. MediaCodec.GetSupportedConfig<T> array allocation per call — cache static codec metadata.',
      '7. Swscale.Convert: ffmpeg.av_frame_copy_props per call — fast but unnecessary if src/dst already share props.',
      '8. No multi-threading guidance / examples.',
      '9. No batching primitives for transcode pipelines.',
      '10. Span<T> usage absent in conversion APIs — high-perf interop wants Span<byte> overloads for plane data.',
      '11. IConverter.Convert returns IEnumerable<MediaFrame> with single yield — IEnumerable<T> state machine plus static array allocates 16+ bytes per call.',
      '12. P/Invoke: per-property string conversion (Name => PtrToStringUTF8()) allocates per call. Cache static codec metadata.',
      '',
      'Return 6-15 findings, with quantified estimates where possible.',
    ].join('\n'),
  },
  {
    key: 'Threading-Async',
    label: 'survey:threading',
    prompt: [
      'Audit threading model, concurrency guarantees, async support.',
      '',
      'Read in ' + REPO + ':',
      '- src/MediaCodec/MediaCodecContext.cs',
      '- src/MediaCodec/MediaDecoder.cs',
      '- src/MediaCodec/MediaEncoder.cs',
      '- src/MediaMux/MediaDemuxer.cs',
      '- src/MediaMux/MediaMuxer.cs',
      '- src/MediaMux/MediaIOContext.cs',
      '- src/MediaFilter/MediaFilterGraph.cs',
      '- src/Utilities/FFmpegLog.cs',
      '',
      'Look for:',
      '1. No documented thread-safety contract anywhere. FFmpeg structures are NOT thread-safe; users may try to feed packets from one thread and receive frames from another (FFmpeg legal pattern, undocumented in this wrapper).',
      '2. No async / Task / IAsyncEnumerable overloads.',
      '3. MediaIOContext Read/Write run on FFmpeg thread (calling thread). NetworkStream blocking reads block codec thread; no AVIOInterruptCB-based cancellation surface.',
      '4. CancellationToken integration absent.',
      '5. AVIOInterruptCB exposed in MediaIOContext.Open but not in MediaDemuxer/MediaMuxer ctors. Plumb through.',
      '6. Multi-thread decoding (codec.thread_count) exposed via Ref but thread_type (FRAME/SLICE) not exposed clearly.',
      '7. Logging callbacks fire on FFmpeg internal threads — verify FFmpegLog marshals properly.',
      '8. MediaFilterGraph runs single-threaded by default.',
      '',
      'Return 4-8 findings.',
    ].join('\n'),
  },
  {
    key: 'Build-Dependencies',
    label: 'survey:build',
    prompt: [
      'Audit build configuration, packaging, target frameworks, NuGet hygiene.',
      '',
      'Use Glob to find csproj files first, then Read them. Also read ' + REPO + '/README.md and ' + REPO + '/FFmpegSharp.sln.',
      '',
      'Look for:',
      '1. Target frameworks: netstandard2.0 + netstandard2.1 — Span<T> on netstandard2.0 needs System.Memory NuGet. Verify reference; consider targeting net6/net8 for SDK improvements.',
      '2. AllowUnsafeBlocks — verify.',
      '3. Nullable reference types — likely disabled; modern C# benefits from nullable annotations on public API.',
      '4. Symbol packages (.snupkg).',
      '5. SourceLink.',
      '6. Version numbering: README says Breaking changes in 8.0.0 — csproj version match.',
      '7. FFmpeg.AutoGen 8.1.0 dependency pinning.',
      '8. tool/CodeGenerator purpose — auto-regen wrappers? Document.',
      '9. example project mixes OpenCvSharp4 — transitive cost.',
      '10. No unit tests project found. Critical gap.',
      '11. CI configuration beyond AppVeyor badge?',
      '',
      'Return 4-10 findings.',
    ].join('\n'),
  },
  {
    key: 'Docs-Examples',
    label: 'survey:docs',
    prompt: [
      'Audit documentation, XML doc comments, examples.',
      '',
      'Read in ' + REPO + ':',
      '- README.md',
      '- example/*.cs (all examples)',
      '- Glob for .md anywhere',
      '',
      'Look for:',
      '1. README sparse — only 2 quick-start snippets. No HW acceleration example. No filter graph example. No audio resample example.',
      '2. XML doc completeness across public APIs — many methods have no <summary>. Filter classes have placeholders (TODO).',
      '3. Examples lack comments explaining FFmpeg API rationale.',
      '4. No architecture/design doc explaining wrapper layering (Internal partial vs public).',
      '5. No migration guide for 7.x to 8.0.0 beyond bullet list.',
      '6. No troubleshooting section (ffmpeg.dll not found is the most common new-user issue).',
      '7. README does not link to FFmpeg.AutoGen docs.',
      '8. No CONTRIBUTING.md.',
      '',
      'Return 3-8 findings.',
    ].join('\n'),
  },
]

const surveyResults = await parallel(
  DIMENSIONS.map(d => () =>
    agent(d.prompt, { label: d.label, phase: 'Survey', schema: FINDING_SCHEMA })
  )
)

const allFindings = surveyResults
  .filter(Boolean)
  .flatMap(r => (r.findings || []).map(f => Object.assign({}, f, { dimension: r.dimension || 'unknown' })))

log('Survey: ' + allFindings.length + ' raw findings across ' + surveyResults.filter(Boolean).length + ' dimensions')

phase('Verify')

const LENSES = [
  'correctness — does the cited code/line actually exhibit the claimed bug? Read the file again and trace control flow.',
  'severity-realism — would a real user hit this? If it requires a contrived call sequence, lower severity.',
]

const verified = await parallel(
  allFindings.map(f => () =>
    parallel(
      LENSES.map((lens, idx) => () =>
        agent(
          [
            'You are an adversarial reviewer. Try hard to REFUTE this finding. Default to isReal=false if you cannot independently confirm it from the source.',
            '',
            'Lens: ' + lens,
            '',
            'Finding ID: ' + f.id,
            'Title: ' + f.title,
            'File: ' + f.file,
            'Evidence: ' + f.evidence,
            'Impact: ' + f.impact,
            'Recommendation: ' + f.recommendation,
            '',
            'To verify: open the cited file in ' + REPO + ' and read the surrounding code. Check whether the evidence actually proves what is claimed.',
            '',
            'Return your verdict.',
          ].join('\n'),
          { label: 'verify:' + f.id + ':' + idx, phase: 'Verify', schema: VERDICT_SCHEMA }
        )
      )
    ).then(verdicts => {
      const valid = verdicts.filter(Boolean)
      const realVotes = valid.filter(v => v.isReal && v.confidence !== 'Low').length
      const survives = realVotes >= 1
      const lowerings = valid.filter(v => v.severityAdjustment === 'Lower' && v.adjustedSeverity !== 'NA').map(v => v.adjustedSeverity)
      const raisings = valid.filter(v => v.severityAdjustment === 'Raise' && v.adjustedSeverity !== 'NA').map(v => v.adjustedSeverity)
      let finalSeverity = f.severity
      if (lowerings.length > 0) finalSeverity = lowerings[0]
      if (raisings.length > 0) finalSeverity = raisings[0]
      return Object.assign({}, f, { survives: survives, finalSeverity: finalSeverity, verdicts: valid })
    })
  )
)

const confirmed = verified.filter(Boolean).filter(v => v.survives)
log('Verify: ' + confirmed.length + '/' + allFindings.length + ' findings survived adversarial review')

phase('Synthesize')

const SYNTH_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  required: ['executiveSummary', 'criticalFindings', 'categorized', 'recommendedRoadmap'],
  properties: {
    executiveSummary: { type: 'string' },
    criticalFindings: {
      type: 'array',
      items: {
        type: 'object',
        required: ['id', 'title', 'why', 'fix'],
        properties: {
          id: { type: 'string' },
          title: { type: 'string' },
          why: { type: 'string' },
          fix: { type: 'string' },
        },
      },
    },
    categorized: {
      type: 'object',
      additionalProperties: {
        type: 'array',
        items: {
          type: 'object',
          required: ['id', 'title', 'severity', 'fix'],
          properties: {
            id: { type: 'string' },
            title: { type: 'string' },
            severity: { type: 'string' },
            file: { type: 'string' },
            fix: { type: 'string' },
          },
        },
      },
    },
    recommendedRoadmap: {
      type: 'array',
      items: {
        type: 'object',
        required: ['milestone', 'rationale', 'includes'],
        properties: {
          milestone: { type: 'string' },
          rationale: { type: 'string' },
          includes: { type: 'array', items: { type: 'string' } },
        },
      },
    },
  },
}

const synthInput = confirmed.map(c => ({
  id: c.id,
  dimension: c.dimension,
  title: c.title,
  category: c.category,
  severity: c.finalSeverity,
  file: c.file,
  evidence: c.evidence,
  impact: c.impact,
  recommendation: c.recommendation,
}))

const synthPrompt = [
  'You are the lead architect synthesizing audit findings into a prioritized rectification list (整改清单) for an FFmpeg C# wrapper library called FFmpeg.Sharp / EmguFFmpeg.',
  '',
  'The owner explicitly flagged TWO areas as most-problematic:',
  '1. Hardware acceleration handling is not perfect.',
  '2. Format conversion is not direct enough.',
  'and asked to find everything else.',
  '',
  'Here are ' + synthInput.length + ' verified findings from a multi-dimension audit:',
  '',
  JSON.stringify(synthInput, null, 2),
  '',
  'Produce a polished rectification list:',
  '- executiveSummary: 3-5 sentences. Reference the owner two flagged areas first.',
  '- criticalFindings: ONLY items at Critical or High that are real bugs (memory leaks, NREs, use-after-free, double-free, incorrect output). 5-10 items.',
  '- categorized: group findings by theme. Suggested themes: Hardware Acceleration, Format Conversion, Memory Safety & Lifecycle, Codec Lifecycle, Mux/Demux/IO, API Ergonomics, Performance, Threading & Async, Build & Packaging, Documentation. Each finding under its best-fitting theme. Include severity, file, and one-sentence fix.',
  '- recommendedRoadmap: 4-6 ordered milestones. M1 must address Critical bugs. M2 should rebuild HW acceleration. M3 should rebuild format conversion API. Subsequent milestones for ergonomics, perf, docs.',
  '',
  'Use Chinese (中文) for executiveSummary, criticalFindings.why, criticalFindings.fix, categorized fix descriptions, and recommendedRoadmap rationale and milestone names — the owner wrote in Chinese. Keep ids, file paths, and code identifiers in English/original.',
  '',
  'Be ruthless about prioritization. The owner will confirm this list before any code changes.',
].join('\n')

const synthesis = await agent(synthPrompt, { label: 'synthesize', phase: 'Synthesize', schema: SYNTH_SCHEMA })

return { rawFindingCount: allFindings.length, confirmedCount: confirmed.length, rectificationList: synthesis }
