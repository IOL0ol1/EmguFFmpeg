EmguFFmpeg
=====================

A [FFmpeg.AutoGen](https://github.com/Ruslan-B/FFmpeg.AutoGen) Warpper Library.

[![NuGet version (EmguFFmpeg)](https://img.shields.io/nuget/v/EmguFFmpeg.svg)](https://www.nuget.org/packages/EmguFFmpeg/)
[![NuGet downloads (EmguFFmpeg)](https://img.shields.io/nuget/dt/EmguFFmpeg.svg)](https://www.nuget.org/packages/EmguFFmpeg/)    
[![Build status](https://ci.appveyor.com/api/projects/status/184vgaesdp86jo5p?svg=true)](https://ci.appveyor.com/project/IOL0ol1/emguffmpeg)

## HowTo

- Download ffmpeg binarys file.     
	- Download from [Zeranoe](https://ffmpeg.zeranoe.com/builds/).    
	- **Copy** from nuget [FFmpeg.Nightly](https://www.nuget.org/packages/FFmpeg.Nightly/) / [FFmpeg.Nightly.LGPL](https://www.nuget.org/packages/FFmpeg.Nightly.LGPL/). 
        - **DO NOT INSTALL** them in c# project. valid only for C++ projects.
- Install [EmguFFmpeg](https://www.nuget.org/packages/EmguFFmpeg/) nuget packet.    
- Import namespace:
	```csharp
	using EmguFFmpeg;
	```
- Media codec with c#

## Example

**Decode** 
```csharp
MediaReader reader = new MediaReader("input.mp4");

foreach(var packet in reader.Packets)
{
    foreach (var frame in reader[packet.StreamIndex].ReadFrame(packet))
    {
        // get frame here
    }
}
```

**Encode**
```csharp
int height = 600;
int width = 800;
int fps = 30;
long lastpts = -1;

// create media writer
MediaWriter writer = new MediaWriter("output.mp4");
// create media encode
MediaEncode videoEncode = MediaEncode.CreateVideoEncode(writer.Format.VideoCodec, writer.Format.Flags, width, height, fps);
// add stream by encode
writer.AddStream(videoEncode);
// init writer
writer.Initialize();

// write 60s duration video
Stopwatch timer = Stopwatch.StartNew();
for(stopwatch.Elapsed <= TimeSpan.FromSeconds(60)) 
{
    long curpts = (long)(timeSpan.TotalSeconds * fps);
    if(curpts > lastpts)
    {
        // TODO: create a videoFrame

        videoFrame.PTS = curpts;
        // write frame, encode to packet and write to writer
        foreach (var packet in writer.First().WriteFrame(videoFrame))
        {
            writer.WritePacket(packet);
        }
        lastpts = curpts;
    }
}

// flush encode cache
writer.FlushMuxer();
writer.Dispose();
```

## TODO

- [ ] **Encode audio need to redesign the AudioFrameConverter, maybe also use IEnumable<>.**    
- [ ] **WriteFrame function will add pts optional parameter.**
- [ ] Convert MediaFrame data easy with EmguCV Mat/Image etc.
- [ ] MediaEncode/MediaDecode.Create... function remove flags param.
- [ ] Add MedaiFilter support.
- [ ] Add test code.
