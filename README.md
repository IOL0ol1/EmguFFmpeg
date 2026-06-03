FFmpeg.Sharp
=====================
**A [FFmpeg.AutoGen](https://github.com/Ruslan-B/FFmpeg.AutoGen) Warpper Library.**     

[![NuGet version (FFmpeg4Sharp)](https://img.shields.io/nuget/v/FFmpeg4Sharp.svg)](https://www.nuget.org/packages/FFmpeg4Sharp/)
[![NuGet downloads (FFmpeg4Sharp)](https://img.shields.io/nuget/dt/FFmpeg4Sharp.svg)](https://www.nuget.org/packages/FFmpeg4Sharp/)
[![Build status](https://ci.appveyor.com/api/projects/status/rrsd6t3pn1gqurbt?svg=true)](https://ci.appveyor.com/project/IOL0ol1/emguffmpeg-hhiy2)    

This is **NOT** a ffmpeg command-line library.    
dev branch is under construction.    
FFmpeg API are unstable, please use ffmpeg library version > 5 


## Usage
### Get ffmpeg *.dll    
Manually download the *.dll files that comply with the license from [ffmpeg.org](http://www.ffmpeg.org/download.html).   
You can get the nightly version on Nuget    
```
NuGet\Install-Package FFmpeg.GPL
NuGet\Install-Package FFmpeg.LGPL 
```

### Install FFmpeg4Sharp 
```
NuGet\Install-Package FFmpeg4Sharp
```
add namespace 
```csharp
using FFmpeg.AutoGen;
using FFmpeg.Sharp;
```
### Quick start
#### Mux and encode
```csharp
/// Create a video file
var fps = 29.97d;
var width = 800;
var heith = 600;
var output = "path-to-your-output-file.mp4";
using (var muxer = MediaMuxer.Create(output))
{
    using (var encoder = MediaEncoder.CreateVideoEncoder(muxer.Format, width, heith, fps, otherSettings: _ => _.Ref.thread_count = 10))
    {
        var stream = muxer.AddStream(encoder);
        muxer.WriteHeader();
        using (var vFrame = MediaFrame.CreateVideoFrame(width, heith, encoder.Ref.pix_fmt))
        {
            for (var i = 0; i < 300; i++)
            {
                // Your code to fill AVFrame.data
                vFrame.Ref.pts = i;
                foreach (var packet in encoder.EncodeFrame(vFrame))
                {
                    packet.Ref.stream_index = stream.Ref.index;
                    muxer.WritePacket(packet, encoder.Ref.time_base);
                }
            }
        }
        muxer.FlushCodecs(new[] { encoder });
        muxer.WriteTrailer();
    }
}
```
#### Demux and decode
```csharp
/// Video to BGR images
var input = "path-to-your-input-file.mp4";
var output = "path-to-your-output-dir";
using (var demuxer = MediaDemuxer.Open(input))
using (var convert = new Swscale())
using (var bgrFrame = new MediaFrame())
{
    var decoders = demuxer.Select(_ => MediaDecoder.CreateDecoder(_.CodecparRef, _ => _.Ref.thread_count = 10)).ToList();
    foreach (var packet in demuxer.ReadPackets())
    {
        var decoder = decoders[packet.Ref.stream_index];
        if (decoder != null && decoder.Ref.codec_type == FFmpeg.AutoGen.AVMediaType.AVMEDIA_TYPE_VIDEO)
        {
            // pre-allocate dst frame once; Swscale.Convert auto-resets on first call from frame metadata.
            if (bgrFrame.Ref.width == 0)
            {
                bgrFrame.Ref.width = decoder.Ref.width;
                bgrFrame.Ref.height = decoder.Ref.height;
                bgrFrame.Ref.format = (int)FFmpeg.AutoGen.AVPixelFormat.AV_PIX_FMT_BGR24;
                bgrFrame.AllocateBuffer();
            }
            foreach (var frame in decoder.DecodePacket(packet))
            {
                // frame is YUV AVFrame
                foreach (var bgrframe in convert.Convert(frame, bgrFrame))
                {
                    // use opencvsharp save to jpg
                    //using (var mat = new Mat(bgrframe.Ref.height, bgrframe.Ref.width, MatType.CV_8UC3))
                    //{
                    //    var srcLineSize = bgrframe.Ref.linesize[0];
                    //    var dstLineSize = (int)mat.Step();
                    //    FFmpegUtil.CopyPlane((IntPtr)bgrframe.Ref.data[0], srcLineSize,
                    //        mat.Data, dstLineSize, Math.Min(srcLineSize, dstLineSize), mat.Height);
                    //    if (frame.Ref.pkt_dts >= 0)
                    //        mat.SaveImage(Path.Combine(output, $"{demuxer[packet.Ref.stream_index].ToTimeSpan(frame.Ref.pkt_dts).TotalMilliseconds}ms.jpg"));
                    //}
                }
            }
        }
    }
    decoders.ForEach(_ => _?.Dispose());
}
```
More see **[Example](./example/FFmpegSharp.Example)**

## Breaking changes in 8.0.0
- Tracks **FFmpeg.AutoGen 8.1.0** (was 7.x). Drops the `FFmpeg.AutoGen.Abstractions` shim namespace; types are now under `FFmpeg.AutoGen` directly.
- **Renamed wrappers**: `OutputFormat → MediaOutputFormat`, `InputFormat → MediaInputFormat`, `PixelConverter → Swscale`, `SampleConverter → Swresample`, `IFrameConverter → IConverter`. The old types are gone.
- **Property access**: the per-field PascalCase property mirrors on `MediaFrame / MediaPacket / MediaCodecContext / MediaFormatContext / MediaStream / MediaCodec / MediaFilter*` were deleted (~600 LOC of boilerplate). Use `instance.Ref.snake_case_field` (returns `ref AVStruct` — readable and writable, zero-copy) for all field access. Example: `frame.Width = 1920` → `frame.Ref.width = 1920`; `packet.Pts` → `packet.Ref.pts`. The previous `Const` snapshot accessor is also removed — `Ref` covers both read and write needs.
- **Swscale** now requires a pre-allocated destination `MediaFrame` (with `Width`/`Height`/`Format` set + `AllocateBuffer()`). The old `PixelConverter.Convert(src)` single-arg overload that allocated internally is gone. Default `new Swscale()` leaves the context null and lazily configures on first `Convert(src, dst)` call from frame metadata.
- **Swresample** constructor requires full in/out parameters (`new Swresample(outCh, outFmt, outRate, inCh, inFmt, inRate)`). The old `SampleConverter.SetOpts(...)` deferred-config API is gone.
- **`MediaFrame.GetBytes`** now has a zero-allocation `Span<byte>` overload: `int GetBytes(Span<byte> dst, bool padding = true)`. Use `int GetBytesSize(bool padding = true)` to size the buffer. The `byte[] GetBytes()` convenience overload is preserved.

## ROADMAP

- Easy api to cut/seek/mute audio clip.
- Easy api to cut/seek video clip.
- More example and test.
- Filter support.
- Data exchange with NAudio and SharpAVI.
- Subtitle support.

## License
This project is licensed under the MIT license.    

But if you use the part of FFmpeg licensed under the GPL,    
the whole project will be contagious by the GPL.