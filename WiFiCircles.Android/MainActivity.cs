using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Collections.ObjectModel;
using System.Linq;

namespace WiFiCircles
{
    [Activity(Label = "WiFiCircles.Android", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape | Android.Content.PM.ScreenOrientation.ReverseLandscape)]
    [IntentFilter(new[] { "android.hardware.usb.action.USB_ACCESSORY_ATTACHED" })]
    [MetaData("android.hardware.usb.action.USB_ACCESSORY_ATTACHED", Resource = "@xml/device_filter")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            _networksFragment = new NetworksFragment(NetworkInfo,
                (ssid, mac) =>
                {
                    _selectedSsid = ssid;
                    _selectedMac = mac;
                    Channels.Clear();
                    var net = NetworkInfo.Where(x => x.Ssid == ssid && x.Mac == mac).FirstOrDefault();
                    if (net != null)
                    {
                        foreach (var chan in net.Channels)
                            Channels.Add(chan);
                    }
                    if (_selectedChannel >= 0)
                        _usb.ScanChannel(-1);//_usb.ScanNetworks();
                    _selectedChannel = -1;
                    _cameraFragment.SetLevel(0);
                });
            _channelsFragment = new ChannelsFragment(Channels,
                (channel) =>
                {
                    //Ssid_Id = _selectedSsid;
                    _selectedChannel = channel;
                    _usb.ScanChannel(channel);
                    _calculator = new LevelCalculator(_selectedSsid, _selectedMac);
                    _cameraFragment.SetLevel(0);
                });
            _cameraFragment = new CameraFragment();
            FragmentManager.BeginTransaction().SetTransition(FragmentTransit.None).Replace(Resource.Id.networks_list_content_frame, _networksFragment).Commit();
            FragmentManager.BeginTransaction().SetTransition(FragmentTransit.None).Replace(Resource.Id.channels_list_content_frame, _channelsFragment).Commit();
            FragmentManager.BeginTransaction().SetTransition(FragmentTransit.None).Replace(Resource.Id.camera_content_frame, _cameraFragment).Commit();
        }

        protected override void OnResume()
        {
            base.OnResume();

            usbHandler = new Handler(
                                 (message) =>
                                 {
                                     //switch ((USBDeviceStatus)message.What)
                                     //{
                                     //    case USBDeviceStatus.UsbReading:
                                     //        break;
                                     //    case USBDeviceStatus.DeviceConnectionClosed:
                                     //        break;
                                     //}
                                     if (message.What == 111)
                                     {
                                         string raw = message.Data.GetString("raw");
                                         _usb_DataReceived(this, new BeaconInfoEventArgs(BeaconInfoData.FromString(raw), raw));
                                     }
                                 });
            _usb = new USBCommunicator(this, usbHandler);
            _usb.DataReceived += _usb_DataReceived;
            _usb.Connect();
        }

        Handler usbHandler;

        private void _usb_DataReceived(object sender, BeaconInfoEventArgs e)
        {
            if (e.Info == null)
                return;

            var net = NetworkInfo.Where(x => x.Ssid == e.Info.ssid && x.Mac == e.Info.mac).FirstOrDefault();
            if (net == null)
            {
                net = new Data.NetworkInfo(e.Info);
                NetworkInfo.Add(net);
            }
            if (net.AddChannel(e.Info) && net.Ssid == _selectedSsid && net.Mac == _selectedMac)
                Channels.Add(net.Channels.Last());
            if (_selectedChannel == e.Info.wifiChan && net.Ssid == _selectedSsid && net.Mac == _selectedMac)
            {
                if (_calculator.HandleInfo(e.Info))
                {
                    var level = _calculator.GetAvg();
                    _cameraFragment.SetLevel(level);
                }
                //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Diff)));
                //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Level)));
            }
        }

        public ObservableCollection<Data.NetworkInfo> NetworkInfo { get; } = new ObservableCollection<Data.NetworkInfo>();
        public ObservableCollection<Data.ChannelInfo> Channels { get; } = new ObservableCollection<Data.ChannelInfo>();

        private string _selectedSsid = string.Empty;
        private string _selectedMac = string.Empty;
        private int _selectedChannel = -1;

        private NetworksFragment _networksFragment;
        private ChannelsFragment _channelsFragment;
        private CameraFragment _cameraFragment;
        private USBCommunicator _usb;
        private LevelCalculator _calculator;
    }
}
