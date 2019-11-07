using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmguFFmpeg.Example
{
    public class ReadDevice : IExample
    {
        public void Start()
        {
            MediaDevice.InitializeDevice();
            var dshowInput = new InFormat("dshow");
            // this func will list all dshow device at console output
            MediaDevice.GetDeviceInfos(dshowInput, MediaDevice.ListDevicesOptions);

            // 'change your auidio name' from console output
            using (MediaReader reader = new MediaReader("audio=change your audio name", dshowInput))
            {
                var stream = reader.Where(_ => _.Codec.Type == AVMediaType.AVMEDIA_TYPE_AUDIO).FirstOrDefault();

                foreach (var packet in reader.ReadPacket())
                {
                    foreach (var frame in stream.ReadFrame(packet))
                    {
                        // TODO： add converter save to MediaWriter
                    }
                }
            }
        }
    }
}