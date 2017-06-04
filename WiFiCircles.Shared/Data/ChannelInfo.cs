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
        public int Rssi { get; private set; }

        private double _rssi;

        private List<double> _history1 = new List<double>();
        private List<double> _history2 = new List<double>();

        private const int MAX_HISTORY = 20;

        public void AddRssi1(double rssi)
        {
            _history1.Add(rssi);
            if (_history1.Count > MAX_HISTORY)
                _history1.RemoveAt(0);
            OnPropertyChanged(nameof(AvgRssi1));
        }

        public void AddRssi2(double rssi)
        {
            _history2.Add(rssi);
            if (_history2.Count > MAX_HISTORY)
                _history2.RemoveAt(0);
            OnPropertyChanged(nameof(AvgRssi2));
        }

        public double AvgRssi1 => _history1.Any() ? _history1.Average() : -255d;
        public double AvgRssi2 => _history2.Any() ? _history2.Average() : -255d;
        public double Diff => Math.Abs(AvgRssi1 - AvgRssi2);
    }
}
