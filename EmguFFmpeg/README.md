EmguFFmpeg
=====================

A [FFmpeg.AutoGen](https://github.com/Ruslan-B/FFmpeg.AutoGen) Warpper Library.    
Cross-platform support.    
    
**This is **NOT** a ffmpeg command-line warpper library**    

[![NuGet version (EmguFFmpeg)](https://img.shields.io/nuget/v/EmguFFmpeg.svg)](https://www.nuget.org/packages/EmguFFmpeg/)
[![NuGet downloads (EmguFFmpeg)](https://img.shields.io/nuget/dt/EmguFFmpeg.svg)](https://www.nuget.org/packages/EmguFFmpeg/)    
[![Build status](https://img.shields.io/appveyor/ci/IOL0ol1/emguffmpeg)](https://ci.appveyor.com/project/IOL0ol1/emguffmpeg)

## Usage

1. Download ffmpeg binarys file:     
	1. Windows:    
        - Download from [Zeranoe](https://ffmpeg.zeranoe.com/builds/).    
        - Download from [FFmpeg.Nightly](https://www.nuget.org/packages/FFmpeg.Nightly/) / [FFmpeg.Nightly.LGPL](https://www.nuget.org/packages/FFmpeg.Nightly.LGPL/) (.nupkg is zip file).    
          **DO NOT INSTALL** them in C# project. valid only for C++ projects.
	2. Other platforms see [this](https://github.com/Ruslan-B/FFmpeg.AutoGen#usage)
2. Install [EmguFFmpeg](https://www.nuget.org/packages/EmguFFmpeg/) nuget packet.    
3. Import namespace:    
```csharp
using FFmpeg.AutoGen; // use some struct
using EmguFFmpeg;
```
4. For windows, Emgucv extension is recommended, see [EmguFFmpeg.EmguCV](/../EmguFFmpeg.EmguCV)

## Example

**Decode** 
```csharp
MediaReader reader = new MediaReader("input.mp4");

foreach(var packet in reader.ReadPacket())
{
    foreach (var frame in reader[packet.StreamIndex].ReadFrame(packet))
    {
        var data = frame.GetData();
    }
}
```

**Encode**
```csharp
/* create a 60s duration video */

int height = 600;
int width = 800;
int fps = 30;
int duration = 60;

// create media writer
MediaWriter writer = new MediaWriter("output.mp4");
// create media encode
MediaEncode videoEncode = MediaEncode.CreateVideoEncode(writer.Format.VideoCodec, writer.Format.Flags, width, height, fps);
// add stream by encode
writer.AddStream(videoEncode);
// init writer
writer.Initialize();
// create video frame
VideoFrame videoFrame = new VideoFrame(AVPixelFormat.AV_PIX_FMT_YUV420P, width, height);

// write 60s duration video
long lastpts = -1;
Stopwatch timer = Stopwatch.StartNew();
for(stopwatch.Elapsed <= TimeSpan.FromSeconds(duration)) 
{
    long curpts = (long)(timeSpan.TotalSeconds * fps);
    if(curpts > lastpts)
    {
        lastpts = curpts;
        // TODO: add converter to fill video frame yuv data from bgr24 data
        // TODO: change WriteFrame interface, add timespan parame

        videoFrame.PTS = curpts;
        // write frame, encode to packet and write to writer
        foreach (var packet in writer.First().WriteFrame(videoFrame))
        {
            writer.WritePacket(packet);
        }
    }
}

// flush encode cache
writer.FlushMuxer();
writer.Dispose();
```
