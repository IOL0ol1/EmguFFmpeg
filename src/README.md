FFmpeg4Sharp
=====================
**A [FFmpeg.AutoGen](https://github.com/Ruslan-B/FFmpeg.AutoGen) Warpper Library.**    
[![NuGet version (FFmpeg4Sharp)](https://img.shields.io/nuget/v/FFmpeg4Sharp.svg)](https://www.nuget.org/packages/FFmpeg4Sharp/)
[![NuGet downloads (FFmpeg4Sharp)](https://img.shields.io/nuget/dt/FFmpeg4Sharp.svg)](https://www.nuget.org/packages/FFmpeg4Sharp/)
[![Build status](https://ci.appveyor.com/api/projects/status/rrsd6t3pn1gqurbt?svg=true)](https://ci.appveyor.com/project/IOL0ol1/emguffmpeg-hhiy2)    

This's NOT a ffmpeg command-line library.    
NOTE: FFmpeg's APIs are unstable.    
Must use the corresponding version of the ffmpeg's dll file.

This branch is under construction, or use old version    
**"NuGet\Install-Package EmguFFmpeg"**.

## Usage

Manually download the *.dll files that comply with the license from [ffmpeg.org](http://www.ffmpeg.org/download.html).    
```csharp
using FFmpeg.AutoGen;
using FFmpeg4Sharp;

///////////// Create a mp4 file ///////////
var fps = 29.97d;
var width = 800;
var heith = 600;
var outputFile = "path-to-your-output-file.mp4"; 
using (var muxer = MediaMuxer.Create(outputFile))
{
    using (var vEncoder = MediaEncoder.CreateVideoEncoder(muxer.Format, width, heith, fps))
    {
        var vStream = muxer.AddStream(vEncoder);

        muxer.WriteHeader();

        using (var vFrame = MediaFrame.CreateVideoFrame(width, heith, vEncoder.PixFmt))
        {
            for (var i = 0; i < 30; i++)
            {
                // your code to fill YUV AVFrame data, 
                // default is green frame. 
                // you can use PixelConverter: RGB AVFrame->YUV AVFrame
                //FillYuv420P(vFrame, i); 
                vFrame.Pts = i;
                foreach (var packet in vEncoder.EncodeFrame(vFrame))
                {
                    packet.StreamIndex = vStream.Index;
                    muxer.WritePacket(packet, vEncoder.TimeBase);
                }
            }
        }
        muxer.FlushCodecs(new[] { vEncoder });
        muxer.WriteTrailer();
    }
}
//////////// Video to images ///////////
var input = "path-to-your-input-file.mp4";
using (var mediaReader = MediaDemuxer.Open(input))
using (var srcPacket = new MediaPacket())
using (var srcFrame = new MediaFrame())
using (var convert = new PixelConverter())
{
    var decoders = mediaReader.Select(_ => MediaDecoder.CreateDecoder(_.CodecparRef)).ToList(); // create decoder for each AVStream
    MediaFrame dstFrame = null;
    foreach (var inPacket in mediaReader.ReadPackets(srcPacket))
    {
        var decoder = decoders[inPacket.StreamIndex];
        if (decoder != null)
        {
            dstFrame = dstFrame == null 
            ? MediaFrame.CreateVideoFrame(decoder.Width, decoder.Height, FFmpeg.AutoGen.AVPixelFormat.AV_PIX_FMT_BGR24) 
            : dstFrame;
            foreach (var inFrame in decoder.DecodePacket(inPacket, srcFrame))
            {
                if (decoder.CodecType == FFmpeg.AutoGen.AVMediaType.AVMEDIA_TYPE_VIDEO) // Only Video AVStream
                {
                    foreach (var outFrame in convert.Convert(inFrame, dstFrame)) // Convert to rgb frame(mp4 frame is yuv frame)
                    {
                        using (var bitmap = new Bitmap(outFrame.Width, outFrame.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                        {
                            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);
                            var srcLineSize = outFrame.Linesize[0];
                            var dstLineSize = bitmapData.Stride;
                            FFmpegUtil.CopyPlane((IntPtr)outFrame.Ref.data[0], srcLineSize,
                                bitmapData.Scan0, bitmapData.Stride, Math.Min(srcLineSize, dstLineSize), bitmap.Height); // rgb frame to bitmap
                            bitmap.UnlockBits(bitmapData);
                            bitmap.Save(Path.Combine(output, $"{mediaReader[inPacket.StreamIndex].ToTimeSpan(inPacket.Pts).TotalMilliseconds}ms.jpg"));
                        }
                    }
                }
            }
        }
    }
    dstFrame?.Dispose();
    decoders.ForEach(_ => _?.Dispose()); // Dispose all decoder
}
```
More see **example/FFmpegSharp.Example**

## ROADMAP

- Easy api to cut/seek/mute audio clip.
- Easy api to cut/seek video clip.
- More example and test.
- Provides a queue for multiplexing/demultiplexing and encoding/decoding.
- Data exchange with NAudio and SharpAVI.
- Subtitle support.
- Split the filter section into optional standalone nuget packages.