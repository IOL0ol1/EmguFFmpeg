EmguFFmpeg
=====================
A [FFmpeg.AutoGen](https://github.com/Ruslan-B/FFmpeg.AutoGen) Warpper Library.    
    
**NOTE** not recommended in production environments,
- api is unstable.
- have memory leak issues (about AvFrame/AvPacket unref and filter, fixing...).
- the filter part warpper is very bad (thinking...).
    
[![NuGet version (EmguFFmpeg)](https://img.shields.io/nuget/v/EmguFFmpeg.svg)](https://www.nuget.org/packages/EmguFFmpeg/)
[![NuGet downloads (EmguFFmpeg)](https://img.shields.io/nuget/dt/EmguFFmpeg.svg)](https://www.nuget.org/packages/EmguFFmpeg/)    
[![Build status](https://img.shields.io/appveyor/ci/IOL0ol1/emguffmpeg)](https://ci.appveyor.com/project/IOL0ol1/emguffmpeg)

## Index


1. [EmguFFmpeg](/EmguFFmpeg)    
	FFmpeg.AutoGen warpper
	It's dependent on FFmpeg.AutoGen.    
2. [EmguFFmpeg.EmguCV](/EmguFFmpeg.EmguCV)    
	Some extension methods for data exchange between EmguFFmpeg and [**EmguCV**](https://github.com/emgucv/emgucv).     
	It's dependent on EmguFFmpeg and EmguCV.    
3. [EmguFFmpeg.OpenCvSharp](/EmguFFmpeg.OpenCvSharp)    
	Some extension methods for data exchange between EmguFFmpeg and [**OpenCvSharp**](https://github.com/shimat/opencvsharp).     
	please read [**here**](https://github.com/shimat/opencvsharp) before use.    
	It's dependent on EmguFFmpeg and OpenCvSharp.    
4. [EmguFFmpeg.Example](/EmguFFmpeg.Example)    
	EmguFFmpeg example.    
	It's dependent on EmguFFmpeg.EmguCV.    

## TODO
   
- [x] Convert MediaFrame data easy with EmguCV etc.
- [x] Add MedaiFilter support.
- [ ] Add Subtitle support.
- [ ] Data exchange with NAudio and SharpAVI.
