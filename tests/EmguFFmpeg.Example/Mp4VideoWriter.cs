using FFmpeg.AutoGen;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace EmguFFmpeg.Example.Example
{
    public class Mp4VideoWriterExample
    {
        public Mp4VideoWriterExample()
        {
            using (Mp4VideoWriter mp4VideoWriter = new Mp4VideoWriter("output.mp4").AddVideo(800, 600, 30).AddAudio(2, 44100).Init())
            {
                // TODO
            }
        }
    }

    public class Mp4VideoWriter : IDisposable
    {
        private MediaWriter writer;
        private int videoIndex = 0;
        private int audioIndex = 0;
        private PixelConverter pixelConverter;
        private SampleConverter sampleConverter;

        public int Height { get; private set; }
        public int Width { get; private set; }
        public int FPS { get; private set; }
        public int Channels { get; private set; }
        public int SampleRate { get; private set; }

        public Mp4VideoWriter(string output)
        {
            writer = new MediaWriter(output, OutFormat.Get("mp4"));
        }

        public Mp4VideoWriter(Stream output)
        {
            writer = new MediaWriter(output, OutFormat.Get("mp4"));
        }

        public Mp4VideoWriter AddVideo(int width, int height, int fps)
        {
            if (writer.Where(_ => _.Codec.Type == AVMediaType.AVMEDIA_TYPE_VIDEO).Count() == 0)
            {
                Height = height;
                Width = width;
                FPS = fps;
                var st = writer.AddStream(MediaEncoder.CreateVideoEncode(writer.Format, width, height, fps));
                videoIndex = writer.Count() - 1;
                pixelConverter = new PixelConverter(st.Codec);
            }
            return this;
        }

        public Mp4VideoWriter AddAudio(int dstChannels, int dstSampleRate)
        {
            if (writer.Where(_ => _.Codec.Type == AVMediaType.AVMEDIA_TYPE_AUDIO).Count() == 0)
            {
                Channels = dstChannels;
                SampleRate = dstSampleRate;
                var stream = writer.AddStream(MediaEncoder.CreateAudioEncode(writer.Format, dstChannels, dstSampleRate));
                audioIndex = writer.Count - 1;
                sampleConverter = new SampleConverter(stream.Codec);
            }
            return this;
        }

        public Mp4VideoWriter Init()
        {
            writer.Initialize();
            return this;
        }

        public void WriteVideoFrame(VideoFrame videoFrame)
        {
            if (videoIndex < 0)
                throw new NotSupportedException();
            foreach (var dstframe in pixelConverter.Convert(videoFrame))
            {
                dstframe.Pts = videoFrame.Pts;
                foreach (var packet in writer[videoIndex].WriteFrame(dstframe))
                {
                    writer.WritePacket(packet);
                }
            }
        }

        private long lastVideoPts = 0;

        public void WriteBGR24(byte[] data, int stride, TimeSpan timeSpan)
        {
            long curPts = (long)timeSpan.TotalSeconds * FPS;
            if (curPts > lastVideoPts)
            {
                VideoFrame videoFrame = new VideoFrame(Width, Height, AVPixelFormat.AV_PIX_FMT_BGR24);
                if (data.Length == videoFrame.Linesize[0] * Height)
                    Marshal.Copy(data, 0, videoFrame.Data[0], data.Length);
                else
                {
                    int line = Math.Min(stride, videoFrame.Linesize[0]);
                    for (int i = 0; i < Height; i++)
                    {
                        Marshal.Copy(data, i * stride, videoFrame.Data[0] + i * videoFrame.Linesize[0], line);
                    }
                }
                videoFrame.Pts = curPts;
                WriteVideoFrame(videoFrame);
                lastVideoPts = curPts;
            }
        }

        public void WriteS16(byte[] data)
        {
            AudioFrame audioFrame = new AudioFrame(Channels, data.Length / Channels / 2, AVSampleFormat.AV_SAMPLE_FMT_S16, SampleRate);
            Marshal.Copy(data, 0, audioFrame.Data[0], data.Length);
            WriteAudioFrame(audioFrame);
        }

        public void WriteFLTP(byte[][] data)
        {
            AudioFrame audioFrame = new AudioFrame(Channels, data[0].Length / 4, AVSampleFormat.AV_SAMPLE_FMT_FLTP, SampleRate);
            for (int i = 0; i < data.Length; i++)
            {
                Marshal.Copy(data[i], 0, audioFrame.Data[i], data.Length);
            }
            WriteAudioFrame(audioFrame);
        }

        private long lastAudioPts = 0;

        public void WriteAudioFrame(AudioFrame audioFrame)
        {
            if (audioIndex < 0)
                throw new NotSupportedException();

            foreach (var dstframe in sampleConverter.Convert(audioFrame))
            {
                lastAudioPts += audioFrame.NbSamples;
                dstframe.Pts = lastAudioPts;
                foreach (var packet in writer[audioIndex].WriteFrame(dstframe))
                {
                    writer.WritePacket(packet);
                }
            }
        }

        public void Close()
        {
            Dispose();
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {

                writer.FlushMuxer();
                writer.Dispose();

                disposedValue = true;
            }
        }

        ~Mp4VideoWriter()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
