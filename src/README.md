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
using FFmpeg.Sharp;
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
    using (var encoder = MediaEncoder.Video().OutputFormat(muxer.Format).Size(width, heith).Fps(fps).Configure(_ => _.Ref.thread_count = 0 /* auto */).Build())
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
### Demux and decode
```csharp
/// Video to BGR images
var input = "path-to-your-input-file.mp4";
var output = "path-to-your-output-dir";
using (var demuxer = MediaDemuxer.Open(input))
using (var convert = new Swscale())
using (var bgrFrame = new MediaFrame())
{
    var decoders = demuxer.Select(_ => MediaDecoder.CreateDecoder(_.CodecparRef)).ToList();
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
More see **[Example](../example/FFmpeg.Sharp.Example)**
## ROADMAP

- Easy api to cut/seek/mute audio clip.
- Easy api to cut/seek video clip.
- More example and test.
- Filter support.
- Data exchange with NAudio and SharpAVI.
- Subtitle support.