FFmpeg4Sharp
=====================
**A [FFmpeg.AutoGen](https://github.com/Ruslan-B/FFmpeg.AutoGen) Warpper Library.**     

[![NuGet version (FFmpeg4Sharp)](https://img.shields.io/nuget/v/FFmpeg4Sharp.svg)](https://www.nuget.org/packages/FFmpeg4Sharp/)
[![NuGet downloads (FFmpeg4Sharp)](https://img.shields.io/nuget/dt/FFmpeg4Sharp.svg)](https://www.nuget.org/packages/FFmpeg4Sharp/)
[![Build status](https://ci.appveyor.com/api/projects/status/rrsd6t3pn1gqurbt?svg=true)](https://ci.appveyor.com/project/IOL0ol1/emguffmpeg-hhiy2)    

This is **NOT** a ffmpeg command-line library.    
dev branch is under construction.    
FFmpeg API are unstable, please use ffmpeg library version > 5 


## Usage
Manually download the *.dll files that comply with the license from [ffmpeg.org](http://www.ffmpeg.org/download.html).    
```
NuGet\Install-Package FFmpeg4Sharp
```
```csharp
using FFmpeg.AutoGen;
using FFmpegSharp;
```
### Mux and encode
```csharp
/// Create a video file
var fps = 29.97d;
var width = 800;
var heith = 600;
var output = "path-to-your-output-file.mp4";
using (var muxer = MediaMuxer.Create(output))
{
    using (var encoder = MediaEncoder.CreateVideoEncoder(muxer.Format, width, heith, fps))
    {
        var stream = muxer.AddStream(encoder);
        muxer.WriteHeader();
        using (var vFrame = MediaFrame.CreateVideoFrame(width, heith, encoder.PixFmt))
        {
            for (var i = 0; i < 300; i++)
            {
                // Your code to fill AVFrame.data
                vFrame.Pts = i;
                foreach (var packet in encoder.EncodeFrame(vFrame))
                {
                    packet.StreamIndex = stream.Index;
                    muxer.WritePacket(packet, encoder.TimeBase);
                }
            }
        }
        muxer.FlushCodecs(new[] { encoder });
        muxer.WriteTrailer();
    }
}
```
### Demux and decode
```csharp
/// Video to BGR images
var input = "path-to-your-input-file.mp4";
var output = "path-to-your-output-dir";
using (var demuxer = MediaDemuxer.Open(input))
using (var convert = new PixelConverter())
{
    var decoders = demuxer.Select(_ => MediaDecoder.CreateDecoder(_.CodecparRef, _ => _.ThreadCount = 10)).ToList();
    foreach (var packet in demuxer.ReadPackets())
    {
        var decoder = decoders[packet.StreamIndex];
        if (decoder != null && decoder.CodecType == FFmpeg.AutoGen.AVMediaType.AVMEDIA_TYPE_VIDEO)
        {
            convert.SetOpts(decoder.Width, decoder.Height, FFmpeg.AutoGen.AVPixelFormat.AV_PIX_FMT_BGR24);
            foreach (var frame in decoder.DecodePacket(packet))
            {
                // frame is YUV AVFrame
                foreach (var bgrframe in convert.Convert(frame))
                {
                    // use opencvsharp save to jpg
                    //using (var mat = new Mat(bgrframe.Height, bgrframe.Width, MatType.CV_8UC3))
                    //{
                    //    var srcLineSize = bgrframe.Linesize[0];
                    //    var dstLineSize = (int)mat.Step();
                    //    FFmpegUtil.CopyPlane((IntPtr)bgrframe.Ref.data[0], srcLineSize,
                    //        mat.Data, dstLineSize, Math.Min(srcLineSize, dstLineSize), mat.Height);
                    //    if (frame.PktDts >= 0)
                    //        mat.SaveImage(Path.Combine(output, $"{demuxer[packet.StreamIndex].ToTimeSpan(frame.PktDts).TotalMilliseconds}ms.jpg"));
                    //}
                }
            }
        }
    }
    decoders.ForEach(_ => _?.Dispose());
}
```
More see **[Example](./example/FFmpegSharp.Example)**
## ROADMAP

- Easy api to cut/seek/mute audio clip.
- Easy api to cut/seek video clip.
- More example and test.
- Filter support.
- Data exchange with NAudio and SharpAVI.
- Subtitle support.