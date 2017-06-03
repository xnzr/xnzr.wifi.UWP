using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WiFiCircles
{
    public sealed class LevelCalculator
    {
        private string ssid;
        private string mac;
        private int avgCount;
        private double[] avgLevels = new double[2];
        private double avgDiff = 0.0;
        private bool needRecalc = true;
        private LinkedList<double> diffs;
        private LinkedList<double> levels;

        public LevelCalculator(string ssid, string mac)
        {
            this.ssid = ssid;
            this.mac = mac;
            avgCount = 15;
            diffs = new LinkedList<double>();
            levels = new LinkedList<double>();
        }

        public bool HandleInfo(BeaconInfoData data)
        {
            //data.print();
            if (data.ssid == ssid && data.mac == mac)
            {
                levels.AddLast(data.level);
                diffs.AddLast(data.diff);
                while (levels.Count > avgCount)
                {
                    levels.RemoveFirst();
                }
                while (diffs.Count > avgCount)
                {
                    diffs.RemoveFirst();
                }
                needRecalc = true;
                print();
            }
            else
            {
                //Console.WriteLine("LevelCalculator.HandleInfo() skip ssid " + data.ssid);
            }
            return needRecalc;
        }

        public void print()
        {
            return;
            double diff = GetAvg();
            System.Diagnostics.Debug.WriteLine("\r                                                          \rLEVEL: {0,5:n2}       rssi: {1,6:n2}  {2,6:n2}", diff, avgLevels[0], avgLevels[1]);
        }

        public double GetAvg()
        {
            if (needRecalc)
            {
                avgDiff = diffs.Average();
                needRecalc = false;
            }
            return avgDiff;
        }

        public double GetCurrent() => levels.Any() ? levels.Last() : -100;
    }
}
