using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiFiCircles.Data
{
    public class NetworkInfo : ModelBase
    {
        public NetworkInfo(BeaconInfoData info)
        {
            Ssid = info.ssid;
            Mac = info.mac;
        }

        public string Ssid { get; set; }
        public string Mac { get; set; }

        public List<ChannelInfo> Channels { get; } = new List<ChannelInfo>();

        public bool AddChannel(BeaconInfoData info)
        {
            bool result = false;
            var chan = Channels.Where(x => x.Channel == info.wifiChan).FirstOrDefault();
            if (chan == null)
            {
                chan = new ChannelInfo(info.wifiChan);
                Channels.Add(chan);
                result = true;
            }
            if (info.rcvIdx == 0)
                chan.AddRssi1(info.level);
            else if (info.rcvIdx == 1)
                chan.AddRssi2(info.level);
            return result;
        }
    }
}
