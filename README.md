EmguFFmpeg
=====================
[![NuGet version (EmguFFmpeg)](https://img.shields.io/nuget/v/EmguFFmpeg.svg)](https://www.nuget.org/packages/EmguFFmpeg/)
[![NuGet downloads (EmguFFmpeg)](https://img.shields.io/nuget/dt/EmguFFmpeg.svg)](https://www.nuget.org/packages/EmguFFmpeg/)
[![Build status](https://img.shields.io/appveyor/ci/IOL0ol1/emguffmpeg)](https://ci.appveyor.com/project/IOL0ol1/emguffmpeg)     

A [FFmpeg.AutoGen](https://github.com/Ruslan-B/FFmpeg.AutoGen) warpper library, make ffmpeg api easier to use.    
    
**NOTE** not recommended in production environments,
- api is unstable.


**This branch is under construction**
    
## Features

1. compatible with **Bitmap**, **EmguCV** and **OpenCvSharp4**.
2. easy decoding and encoding of data through **IEnumerable** interface.
3. video/audio frame format converter.
4. switch to ffmpeg.autogen or reverse. 

 
## Index

1. [EmguFFmpeg](/src)    
	FFmpeg.AutoGen warpper
	It's dependent on FFmpeg.AutoGen.    
	for windows:
	- nuget install EmguFFmpeg
	- download the dll of ffmpeg with different licenses on your own.
 

## TODO
    
- [ ] Add Subtitle support.
- [ ] Data exchange with NAudio and SharpAVI.
