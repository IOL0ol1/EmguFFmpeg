using System;
using System.Collections.Generic;
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
            /// just work in live unit testing
            FFmpegHelper.RegisterBinaries(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg"));
        }


        [Fact]
        public void TestNotSupported()
        {
            MediaDictionary options = new MediaDictionary();
            options.Add("protocol_whitelist", "file");
            options.Add("key", "value"); // not supported
            options.Add("protocol_blacklist", "cache");
            var file = "test.mp4";
            AVFormatContext* pFormatContext = null;
            AVFormatContext** ppFormatContext = &pFormatContext;
            var ret1 = ffmpeg.avformat_alloc_output_context2(ppFormatContext, null, null, file);
            var ret2 = ffmpeg.avio_open2(&pFormatContext->pb, file, ffmpeg.AVIO_FLAG_WRITE, null, options);
            Assert.True(options.Count == 1);
            Assert.Equal("key", options.First().Key);
            Assert.Equal("value", options.First().Value);
        }

        [Fact]
        public void TestDictionary()
        {
            MediaDictionary options = new MediaDictionary();
            options.Add("k1", "v1");
            options.Add("k2", "v2");
            Assert.Equal(2, options.Count);

            options.Add("K1", "v3",AVDictWriteFlags.MultiKey);
            Assert.Equal(3, options.Count);
            Assert.Equal("v3",options["K1"]);
            Assert.Equal("v1",options["k1"]);

            options.Add("k2", "v4",AVDictWriteFlags.DontOverwrite);
            Assert.Equal(3, options.Count);
            Assert.Equal("v2", options["k2"]);

            options.Add("k1", "v5",AVDictWriteFlags.Append);
            Assert.Equal(3, options.Count);
            Assert.Equal("v1v5", options["k1"]);
        }
    }
}
