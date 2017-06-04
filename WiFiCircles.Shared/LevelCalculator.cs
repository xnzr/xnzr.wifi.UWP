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
        private LinkedList<double>[] levels;

        public LevelCalculator(string ssid, string mac)
        {
            this.ssid = ssid;
            this.mac = mac;
            avgCount = 15;
            levels = new LinkedList<double>[2];
            levels[0] = new LinkedList<double>();
            levels[1] = new LinkedList<double>();
        }

        public bool HandleInfo(BeaconInfoData data)
        {
            //data.print();
            if (data.ssid == ssid && data.mac == mac)
            {
                int idx = data.rcvIdx;
                if (0 <= idx && idx <= 1)
                {
                    levels[idx].AddLast(data.level);
                    while (levels[idx].Count > avgCount)
                    {
                        levels[idx].RemoveFirst();
                    }
                    needRecalc = true;
                    print();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("LevelCalculator.HandleInfo() Bad rcvIdx: " + data.rcvIdx);
                }
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
                for (int idx = 0; idx < 2; idx++)
                {
                    double sum = 0.0;
                    foreach (double x in levels[idx])
                    {
                        sum += x;
                    }
                    int count = levels[idx].Count;
                    if (count != 0)
                    {
                        sum /= count;
                    }
                    avgLevels[idx] = sum;
                }
                //copy-pasted from beacon radar
                //tfDiff = Math.Pow(10.0, ((double)(ch1rssi - ch0rssi) * 0.1 + 4) * Services.SettingsServices.SettingsService.Instance.PowerAmplifier);
                //
                //avgDiff = Math.Abs(avgLevels[0] - avgLevels[1]);
                //avgDiff = 100 - 4*(avgLevels[0] - avgLevels[1]);
                avgDiff = Math.Pow(10.0, ((double)(avgLevels[1] - avgLevels[0]) * 0.1 + 2.5) * 1);
                //avgDiff = 2000 / 100 * avgDiff;

                needRecalc = false;
            }
            return avgDiff;
        }

        public double GetCurrent() => levels[0].Any() ? levels[0].Last() : -100;
    }
}
