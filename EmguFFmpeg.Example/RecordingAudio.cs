using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmguFFmpeg.Example
{
    public class RecordingAudio : IExample
    {
        /// <summary>
        /// recording audio
        /// </summary>
        /// <param name="outputFile"></param>
        public RecordingAudio(string outputFile)
        {
            // register all device
            MediaDevice.InitializeDevice();
            var dshowInput = new InFormat("dshow");
            // list all "dshow" device at console output, ffmpeg does not support direct reading of device names
            MediaDevice.GetDeviceInfos(dshowInput, MediaDevice.ListDevicesOptions);

            // get your audio input device name from console output
            // NOTE: DO NOT delete "audio="
            using (MediaReader reader = new MediaReader("audio=change to your audio input device name", dshowInput))
            using (MediaWriter writer = new MediaWriter(outputFile))
            {
                var stream = reader.Where(_ => _.Codec.Type == AVMediaType.AVMEDIA_TYPE_AUDIO).First();

                writer.AddStream(MediaEncode.CreateAudioEncode(writer.Format, (AVChannelLayout)stream.Codec.AVCodecContext.channel_layout, stream.Codec.AVCodecContext.sample_rate));
                writer.Initialize();

                AudioFrame dstFrame = AudioFrame.CreateFrameByCodec(writer[0].Codec);
                SampleConverter converter = new SampleConverter(dstFrame);
                long pts = 0;
                foreach (var packet in reader.ReadPacket())
                {
                    foreach (var frame in stream.ReadFrame(packet))
                    {
                        foreach (var dstframe in converter.Convert(frame))
                        {
                            pts += dstFrame.NbSamples;
                            dstFrame.Pts = pts;
                            foreach (var dstpacket in writer[0].WriteFrame(dstFrame))
                            {
                                writer.WritePacket(dstpacket);
                            }
                        }
                    }
                }
                writer.FlushMuxer();
            }
        }
    }
}