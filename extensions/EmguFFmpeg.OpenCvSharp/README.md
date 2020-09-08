EmguFFmpeg.OpenCvSharp
=====================

[OpenCvSharp](https://github.com/shimat/opencvsharp) Extension of [EmguFFmpeg](/../EmguFFmpeg)  

    
Add some extension methods for data exchange between [OpenCvSharp](https://github.com/shimat/opencvsharp) and [EmguFFmpeg](../EmguFFmpeg/README.md)

[![NuGet version (EmguFFmpeg.OpenCvSharp)](https://img.shields.io/nuget/v/EmguFFmpeg.OpenCvSharp.svg)](https://www.nuget.org/packages/EmguFFmpeg.OpenCvSharp/)
[![NuGet downloads (EmguFFmpeg.OpenCvSharp)](https://img.shields.io/nuget/dt/EmguFFmpeg.OpenCvSharp.svg)](https://www.nuget.org/packages/EmguFFmpeg.OpenCvSharp/)
[![Build status](https://img.shields.io/appveyor/ci/IOL0ol1/emguffmpeg)](https://ci.appveyor.com/project/IOL0ol1/emguffmpeg)

# Note

Platform related packages need to be installed before use.
nuget like:
```
OpenCvSharp4.runtime.*
```


# Extension

**Mat to VideoFrame**
```csharp
using (Mat image = new Mat(800, 600, MatType.CV_8UC3, new Scalar(100, 100, 100)))
{
    VideoFrame bgr = mat.ToVideoFrame(); // BGR24, width 800, height 600
    VideoFrame yuv420 = image.ToVideoFrame(AVPixelFormat.AV_PIX_FMT_YUV420P); // YUV420P, width 800, height 600
}
```
**Mat to AudioFrame**
```csharp
// frame nbsamples is mat width, frame format is auto set by mat depth type
using (Mat mat = new Mat(2, 1024, MatType.CV_16SC1)) // if mat channels == 1 frame is planar, frame channels is mat height.
{
    AudioFrame s16pc2s0 = mat.ToAudioFrame(); // S16P, channels 2, sample rate 0. if use this frame in ffmpeg, need set sample rate later.
    AudioFrame s16pc2s44100 = mat.ToAudioFrame(dstSampleRate: 44100); // S16P, channels 2, sample rate 44100
    AudioFrame fltpc2s48000 = mat.ToAudioFrame(AVSampleFormat.AV_SAMPLE_FMT_FLTP, 48000); // FLTP, channels 2, sample rate 44100
}
using (Mat mat = new Mat(1, 1024, MatType.CV_32SC2)) // if mat channels > 1 frame is packet, frame channels is mat channels, only first line in mat is used.
{
    AudioFrame s32c2s0 = mat.ToAudioFrame(); // S32, channels 2, sample rate 0. if use this frame in ffmpeg, need set sample rate later.
    AudioFrame s32c2s44100 = mat.ToAudioFrame(dstSampleRate: 44100); // S32, channels 2, sample rate 44100
    AudioFrame fltpc2s48000 = mat.ToAudioFrame(AVSampleFormat.AV_SAMPLE_FMT_FLTP, 48000); // FLTP, channels 2, sample rate 44100
}
```

**VideoFrame to Mat**
```csharp
VideoFrame videoFrame = new VideoFrame(AVPixelFormat.AV_PIX_FMT_YUV420P, 800, 600);
Mat bgra = videoFrame.ToMat(); // Bgra, channels 4, width 800, height 600
```
**AudioFrame to Mat**
```csharp
// mat does not store sample rate, this will lose the sample rate.
AudioFrame audioPacketFrame = new AudioFrame(AVSampleFormat.AV_SAMPLE_FMT_S16, 3, 1024, 44100);
Mat cv16sH1C3 = audioPacketFrame.ToMat(); // Cv16S, width 1024, height 1, channels 3

AudioFrame audioPlanarFrame = new AudioFrame(AVSampleFormat.AV_SAMPLE_FMT_FLTP, 3, 1024, 44100);
Mat cv32fH3C1 = audioPlanarFrame.ToMat(); // Cv32F, width 1024, height 3, channels 1
```

# Note
1. Mat not hava Cv64S.    
So we can't create a AV_SAMPLE_FMT_S64 or AV_SAMPLE_FMT_S64P AudioFrame directly from Mat.

2. S64 or S64P AudioFrame will convert to Cv64F Mat, but stored inside is long.    
There is a example to get long type output.
```csharp
// if is packet, output is long[1,frame nbsamples,frame channels];

AudioFrame dblpPlanarFrame = new AudioFrame(AVSampleFormat.AV_SAMPLE_FMT_S64, 2, 1024, 44100);
var cv64fH1C1 = dblpPlanarFrame.ToMat(); // Cv64F, width 1024, height 1, number of channel 2
cv64fH1C1.GetRectangularArray<Vec2d>(out var data); // Vec*d, * is number of channel

var r0 = data.GetLength(0);
var r1 = data.GetLength(1);
var r2 = data[0, 0].Count(); // number of channel
long[,,] output = new long[r0, r1, r2];
for (int i = 0; i < r0; i++)
{
    for (int j = 0; j < r1; j++)
    {
        for (int k = 0; k < r2; k++)
        {
            output[i, j, k] = data[i, j].ToInt64Bits(r2)[k];
        }
    }
} // output is long[1,1024,2]
```
```csharp
// if is planar, output is long[frame channels,frame nbsamples];

AudioFrame dblpPacketFrame = new AudioFrame(AVSampleFormat.AV_SAMPLE_FMT_S64P, 2, 1024, 44100);
var cv64fH2C1 = dblpPacketFrame.ToMat(); // Cv64F, width 1024, height 2, number of channel 1
cv64fH2C1.GetRectangularArray<double>(out var data);  // data is double[2,1024]
long[,] output = new long[data.GetLength(0), data.GetLength(1)];
Buffer.BlockCopy(data, 0, output, 0, data.Length * sizeof(long)); // output is long[2,1024]
```

3. **DO NOT** forget to call **Dispose()** for OpenCvSharp data

4. If get pointers using implicit type conversions, please call GC.KeepAlive() at th end.    
e.g.    
```csharp
//MediaCodec decode = ...;
//MediaCodec encode = ...;
//MediaFrame frame = ...;

ffmpeg.avcodec_receive_frame(decode, frame);

// some operation with AVFrame*
AVFrame* pFrame = frame; // implicit type conversion
pFrame->channels = 3;


ffmpeg.avcodec_send_frame(encode, pFrame);


GC.KeepAlive(frame); // Keep the reference and make object alive
// After this, the GC can reclaim objects
```


 

