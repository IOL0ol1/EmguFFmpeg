FFmpeg4Sharp
=====================
**A [FFmpeg.AutoGen](https://github.com/Ruslan-B/FFmpeg.AutoGen) Warpper Library.**


This's NOT a ffmpeg command-line library.    
FFmpeg's APIs are unstable.    
Must use the corresponding version of the ffmpeg's dll file.

[![NuGet version (FFmpeg4Sharp)](https://img.shields.io/nuget/v/FFmpeg4Sharp.svg)](https://www.nuget.org/packages/FFmpeg4Sharp/)
[![NuGet downloads (FFmpeg4Sharp)](https://img.shields.io/nuget/dt/FFmpeg4Sharp.svg)](https://www.nuget.org/packages/FFmpeg4Sharp/)
[![Build status](https://ci.appveyor.com/api/projects/status/rrsd6t3pn1gqurbt?svg=true)](https://ci.appveyor.com/project/IOL0ol1/emguffmpeg-hhiy2)

## Usage

Manually download the files that comply with the license from [ffmpeg.org](http://www.ffmpeg.org/download.html).    
```csharp
using FFmpeg.AutoGen; // use some struct
using FFmpeg4Sharp;

// Create a mp4 file
var fps = 29.97d;
var width = 800;
var heith = 600;
var outputFile = "path-to-your-file.mp4"; 
using (var muxer = MediaMuxer.Create(outputFile))
{
    using (var vEncoder = MediaEncoder.CreateVideoEncoder(muxer.Format, width, heith, fps))
    {
        MediaStream vStream = muxer.AddStream(vEncoder);

        muxer.WriteHeader();

        using (var vFrame = MediaFrame.CreateVideoFrame(width, heith, vEncoder.Context.PixFmt))
        {
            for (int i = 0; i < 30; i++)
            {
                //Add your code to fill vFrame.Data, default is fill green color.
                //FillYuv420P(vFrame, i); 
                vFrame.Pts = i;
                foreach (var packet in vEncoder.EncodeFrame(vFrame))
                {
                    packet.StreamIndex = vStream.Index;
                    muxer.WritePacket(packet, vEncoder.Context.TimeBase);
                }
            }
        }
        muxer.FlushCodecs(new[] { vEncoder });
    }
}
```
 