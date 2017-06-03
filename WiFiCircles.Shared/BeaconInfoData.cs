using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiFiCircles
{
    public class BeaconInfoData
    {
        public byte wifiChan;
        public string mac;
        public Int64 time;
        public double diff;
        public double level;
        public string ssid;

        public override string ToString()
        {
            return
                "chan " + wifiChan + "  " +
                "mac " + mac + "  " +
                "time " + time + "  " +
                "diff " + diff + "  " +
                "lev " + level + "  " +
                "ssid " + ssid;
        }

        static int MIN_COUNT = 5;
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
                info.wifiChan = Convert.ToByte(subStrings[1], 10);
                info.mac = subStrings[0];
                info.diff = Convert.ToDouble(subStrings[2]);
                info.level = Convert.ToDouble(subStrings[3]);
                info.ssid = subStrings[4];
                for (int i = 5; i < subStrings.Length; i++)
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
