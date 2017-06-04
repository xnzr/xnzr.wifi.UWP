using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiFiCircles
{
    public class BeaconInfoData
    {
        public int rcvIdx;
        public byte wifiChan;
        public string mac;
        public Int64 time;
        public double level;
        public string ssid;

        public override string ToString()
        {
            return
                "ant " + rcvIdx + "  " +
                "chan " + wifiChan + "  " +
                "mac " + mac + "  " +
                "time " + time + "  " +
                "lev " + level + "  " +
                "ssid " + ssid;
        }

        static int MIN_COUNT = 6;
        public static BeaconInfoData FromString(string str)
        {
            //Console.WriteLine("parseString(" + str + ")");
            string[] subStrings = str.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (subStrings.Length < MIN_COUNT)
            {
                System.Diagnostics.Debug.WriteLine("           parseString() error:'" + str + "'");

                return null;
            }

            //2 06 9027E45EA88D 12E69160 -079 WiFi Alexander

            BeaconInfoData info = new BeaconInfoData();
            try
            {
                info.rcvIdx = Convert.ToInt32(subStrings[0], 10) - 1;
                info.wifiChan = Convert.ToByte(subStrings[1], 10);
                info.mac = subStrings[2];
                info.time = Convert.ToInt64(subStrings[3], 16);
                info.level = (double)Convert.ToInt32(subStrings[4], 10);
                info.ssid = subStrings[5];
                for (int i = 6; i < subStrings.Length; i++)
                {
                    info.ssid += " ";
                    info.ssid += subStrings[i];
                }
            }
            catch (FormatException e)
            {
                System.Diagnostics.Debug.WriteLine("Parse Error: " + e.StackTrace);
                return null;
            }
            //info.print();
            return info;
        }
    }
}
