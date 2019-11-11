using FFmpeg.AutoGen;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace EmguFFmpeg.Example.Example
{
    public class Mp4VideoWriter : IDisposable
    {
        private MediaWriter writer;
        private VideoFrame videoFrame;
        private AudioFrame audioFrame;
        private int videoIndex = 0;
        private int audioIndex = 0;

        public int Height { get; private set; }
        public int Width { get; private set; }
        public int FPS { get; private set; }
        public int Channels { get; private set; }
        public int SampleRate { get; private set; }

        public Mp4VideoWriter(string output)
        {
            writer = new MediaWriter(output, new OutFormat("mp4"));
        }

        public Mp4VideoWriter(Stream output)
        {
            writer = new MediaWriter(output, new OutFormat("mp4"));
        }

        public Mp4VideoWriter AddVideo(int width, int height, int fps)
        {
            if (writer.Where(_ => _.Codec.Type == AVMediaType.AVMEDIA_TYPE_VIDEO).Count() == 0)
            {
                Height = height;
                Width = width;
                FPS = fps;
                writer.AddStream(MediaEncode.CreateVideoEncode(writer.Format, width, height, fps));
                videoIndex = writer.Count() - 1;
                videoFrame = new VideoFrame(AVPixelFormat.AV_PIX_FMT_BGR24, width, height);
            }
            return this;
        }

        public Mp4VideoWriter AddAudio(int channels, int sampleRate)
        {
            if (writer.Where(_ => _.Codec.Type == AVMediaType.AVMEDIA_TYPE_AUDIO).Count() == 0)
            {
                Channels = channels;
                SampleRate = sampleRate;
                var stream = writer.AddStream(MediaEncode.CreateAudioEncode(writer.Format, channels, sampleRate));
                audioIndex = writer.Count - 1;
                audioFrame = new AudioFrame(AVSampleFormat.AV_SAMPLE_FMT_S16, channels, stream.Codec.AVCodecContext.frame_size, sampleRate);
            }
            return this;
        }

        public Mp4VideoWriter Init()
        {
            writer.Initialize();
            return this;
        }

        private long lastpts = -1;

        public void WriteVideoFrame(byte[] data, TimeSpan timeSpan)
        {
            if (videoIndex < 0)
                throw new NotSupportedException();

            Marshal.Copy(data, 0, videoFrame.Data[0], data.Length);

            long curpts = (long)timeSpan.TotalSeconds * FPS;
            if (curpts > lastpts)
            {
                lastpts = curpts;
                videoFrame.Pts = lastpts;
                foreach (var packet in writer[videoIndex].WriteFrame(videoFrame))
                {
                    writer.WritePacket(packet);
                }
            }
        }

        private long lastSamples = 0;

        public void WriteAudioFrame(byte[] data)
        {
            if (audioIndex < 0)
                throw new NotSupportedException();

            Marshal.Copy(data, 0, audioFrame.Data[0], data.Length);
            lastSamples += audioFrame.AVFrame.nb_samples;
            audioFrame.Pts = lastSamples;
            foreach (var packet in writer[videoIndex].WriteFrame(audioFrame))
            {
                writer.WritePacket(packet);
            }
        }

        public void Close()
        {
            Dispose();
        }

        #region IDisposable Support

        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)。
                }

                writer.FlushMuxer();
                writer.Dispose();

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        ~Mp4VideoWriter()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(false);
        }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}