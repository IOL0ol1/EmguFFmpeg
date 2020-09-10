using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using EmguFFmpeg;

using FFmpeg.AutoGen;

using Xunit;
using Xunit.Abstractions;

namespace EmugFFmpeg.WapperTest
{
    public unsafe class MediaDictionaryTest
    {

        protected readonly ITestOutputHelper Output;

        public MediaDictionaryTest(ITestOutputHelper testOutput)
        {
            Output = testOutput;
            FFmpegHelper.RegisterBinaries(@"E:\Projects\EmguFFmpeg\tests\EmugFFmpeg.Test\bin\Debug\netcoreapp3.1");
        }


        [Fact]
        public void Test1()
        {

            MediaDictionary options = new MediaDictionary();
            options.Add("protocol_whitelist", "file");
            options.Add("key", "value");
            options.Add("protocol_blacklist", "cache");
            var file = "222.mp4";
            AVFormatContext* pFormatContext = null;
            AVFormatContext** ppFormatContext = &pFormatContext;
            var ret1 = ffmpeg.avformat_alloc_output_context2(ppFormatContext, OutFormat.Get("mpeg"), null, file);
            var ret2 = ffmpeg.avio_open2(&pFormatContext->pb, file, ffmpeg.AVIO_FLAG_WRITE, null, options);
            Assert.True(options.Count == 1);
            Assert.Equal("key", options.First().Key);
            Assert.Equal("value", options.First().Value);
        }
    }
}
