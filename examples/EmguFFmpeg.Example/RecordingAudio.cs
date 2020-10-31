using System;
using System.Linq;
using FFmpeg.AutoGen;

namespace EmguFFmpeg.Example
{
    public class RecordingAudio
    {
        /// <summary>
        /// recording audio.
        /// <para>
        /// first set inputDeviceName = null, you will get inputDeviceName list in vs output,
        /// </para>
        /// <para>
        /// then set inputDeviceName to your real device name and run again,you will get a audio output.
        /// </para>
        /// <para>
        /// if you want stop record, exit console;
        /// </para>
        /// <para>ffmpeg </para>
        /// </summary>
        /// <param name="outputFile"></param>
        /// <param name="inputDeviceName"></param>
        public RecordingAudio(string outputFile, string inputDeviceName = null)
        {
            // console output
            FFmpegHelper.SetupLogging(logWrite: _ => Console.Write(_));
            // register all device
            FFmpegHelper.RegisterDevice();

            var dshowInput = InFormat.Get("dshow");
            // list all "dshow" device at console output, ffmpeg does not support direct reading of device names
            MediaDevice.PrintDeviceInfos(dshowInput, "list", MediaDevice.ListDevicesOptions);

            if (string.IsNullOrWhiteSpace(inputDeviceName)) return;
            // get your audio input device name from console output
            // NOTE: DO NOT delete "audio="
            using (MediaReader reader = new MediaReader($"audio={inputDeviceName}", dshowInput))
            using (MediaWriter writer = new MediaWriter(outputFile))
            {
                var stream = reader.Where(_ => _.Codec.Type == AVMediaType.AVMEDIA_TYPE_AUDIO).First();

                writer.AddStream(MediaEncoder.CreateAudioEncode(writer.Format, stream.Codec.AVCodecContext.channels, stream.Codec.AVCodecContext.sample_rate));
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
                            pts += dstFrame.AVFrame.nb_samples;
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