EmguFFmpeg
=====================
[![NuGet version (EmguFFmpeg)](https://img.shields.io/nuget/v/EmguFFmpeg.svg)](https://www.nuget.org/packages/EmguFFmpeg/)
[![NuGet downloads (EmguFFmpeg)](https://img.shields.io/nuget/dt/EmguFFmpeg.svg)](https://www.nuget.org/packages/EmguFFmpeg/)
[![Build status](https://img.shields.io/appveyor/ci/IOL0ol1/emguffmpeg)](https://ci.appveyor.com/project/IOL0ol1/emguffmpeg)     

A [FFmpeg.AutoGen](https://github.com/Ruslan-B/FFmpeg.AutoGen) warpper library, make ffmpeg api easier to use.    
    
**NOTE** not recommended in production environments,
- api is unstable.
    
## Features

1. compatible with **Bitmap**, **EmguCV** and **OpenCvSharp4**.
2. easy decoding and encoding of data through **IEnumerable** interface.
3. video/audio frame format converter.
4. filter supported.

## Example

**Decode** 
```csharp
// create media reader by file
using(MediaReader reader = new MediaReader("input.mp4"))
{
    // get packet from reader (demultiplexing to packet)
    foreach(var packet in reader.ReadPacket())
    {
        // get frame from different stream (decode to frame).
        foreach (var frame in reader[packet.StreamIndex].ReadFrame(packet))
        {
            IntPtr[] data = frame.Data; // FFmpeg AVFrame.data
	    byte[][] managed = frame.GetData(); // copy AVFrame.data to managed data
	    if(frame is VideoFrame)
	        Bitmap bitmap = frame.ToBitmap(); // add EmguFFmpeg.Bitmap, only for video frame
	    Mat mat = frame.ToMat(); // add EmguFFmpeg.OpenCvSharp or EmguFFmpeg.EmguCV (video and audio)
        }
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
using(MediaWriter writer = new MediaWriter("output.mp4"))
{
    // create media encoder
    MediaEncode videoEncode = MediaEncode.CreateVideoEncode(writer.Format.VideoCodec, writer.Format.Flags, width, height, fps);
    // add stream by encoder
    writer.AddStream(videoEncode);
    // init writer
    writer.Initialize();
    // create video frame, default video frame is green image 
    VideoFrame videoFrame = new VideoFrame(AVPixelFormat.AV_PIX_FMT_YUV420P, width, height);

    // write frame by timespan see EmguFFmpeg.Example/Mp4VideoWriter.cs#L95
    // write 60s duration video
    long lastpts = -1;
    Stopwatch timer = Stopwatch.StartNew();
    for(stopwatch.Elapsed <= TimeSpan.FromSeconds(duration)) 
    {
        long curpts = (long)(timeSpan.TotalSeconds * fps);
        if(curpts > lastpts)
        {
            lastpts = curpts;
            
            /* fill video frame data, see more code in example*/
            
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
}
```

## Index

1. [EmguFFmpeg](/EmguFFmpeg)    
	FFmpeg.AutoGen warpper
	It's dependent on FFmpeg.AutoGen.    
	for windows:
	- nuget install EmguFFmpeg
	- download the dll of ffmpeg with different licenses on your own.
2. [EmguFFmpeg.EmguCV](/EmguFFmpeg.EmguCV)    
	Some extension methods for data exchange between EmguFFmpeg and [**EmguCV**](https://github.com/emgucv/emgucv).     
	It's dependent on EmguFFmpeg and EmguCV.    
	for windows: 
	- nuget install EmguFFmpeg.EmguCV
	- nuget install Emgu.CV.runtime.windows
	- download the dll of ffmpeg with different licenses on your own.
3. [EmguFFmpeg.OpenCvSharp](/EmguFFmpeg.OpenCvSharp)    
	Some extension methods for data exchange between EmguFFmpeg and [**OpenCvSharp**](https://github.com/shimat/opencvsharp).     
	please read [**here**](https://github.com/shimat/opencvsharp) before use.    
	It's dependent on EmguFFmpeg and OpenCvSharp.    
	for windows: 
	- nuget install EmguFFmpeg.OpenCvSharp
	- nuget install OpenCvSharp4.runtime.win
	- download the dll of ffmpeg with different licenses on your own.
4. [EmguFFmpeg.Bitmap](/EmguFFmpeg.Bitmap)    
        Some extension methods for Bitmap. only supported video frame.
5. [EmguFFmpeg.Example](/EmguFFmpeg.Example)    
	EmguFFmpeg example.     

## TODO
   
- [x] Convert MediaFrame data easy with EmguCV etc.
- [x] Add MedaiFilter support.
- [ ] Add Subtitle support.
- [ ] Data exchange with NAudio and SharpAVI.
