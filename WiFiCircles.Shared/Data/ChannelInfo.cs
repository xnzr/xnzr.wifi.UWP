using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiFiCircles.Data
{
    public class ChannelInfo : ModelBase
    {
        public ChannelInfo(int channel)
        {
            Channel = channel;
        }

        public int Channel { get; }
        //public int Rssi { get; private set; }

        private double _rssi;
        private double _diff;

        public void AddRssi(double rssi, double diff)
        {
            _rssi = rssi;
            _diff = diff;
            OnPropertyChanged(nameof(AvgRssi));
        }

        public double AvgRssi => _rssi;
        public double Diff => _diff;
    }
}
