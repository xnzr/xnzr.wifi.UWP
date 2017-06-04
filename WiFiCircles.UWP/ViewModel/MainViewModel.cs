using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;

namespace WiFiCircles.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public MainViewModel()
        {
        }

        public Windows.UI.Core.CoreDispatcher Dispatcher { get; set; }

        #region Device lookup
        private DeviceWatcher _deviceWatcher;
        private string _deviceId;

        private void StartLookup()
        {
            if (_deviceWatcher == null)
            {
                var aqsFilter = SerialDevice.GetDeviceSelector();
                _deviceWatcher = DeviceInformation.CreateWatcher(aqsFilter);
                _deviceWatcher.Added += DeviceWatcher_Added;
                _deviceWatcher.Removed += DeviceWatcher_Removed;
                _deviceWatcher.Start();
            }
        }

        private void StopLookup()
        {
            if (_deviceWatcher != null)
            {
                _deviceWatcher.Added -= DeviceWatcher_Added;
                _deviceWatcher.Removed -= DeviceWatcher_Removed;
                _deviceWatcher.Stop();
                _deviceWatcher = null;
            }
            if (_device != null)
            {
                _device.DataReceived -= _device_DataReceived;
                _device = null;
                _deviceId = string.Empty;
            }
        }

        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            if (_device == null)
            {
                var device = await SerialDevice.FromIdAsync(args.Id);
                if (device != null && device.UsbVendorId == 0x0483 && device.UsbProductId == 0x5740)
                {
                    await Dispatcher?.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => SelectSsid(string.Empty, string.Empty));
                    _device = new WiFiScannerDevice(new Devices.UWPDevice(device));
                    await _device.SwitchToOldProtocolAsync();
                    _device.DataReceived += _device_DataReceived;
                    _device.ScanNetworks();
                }
            }
        }

        private async void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            var device = await SerialDevice.FromIdAsync(args.Id);
            if (_device != null && args.Id == _deviceId)
            {
                _device.DataReceived -= _device_DataReceived;
                _device.Stop();
                _device = null;
                _deviceId = string.Empty;
            }
        }
        #endregion Device lookup

        public void Start()
        {
            StartLookup();
        }

        public void Scan()
        {
            //_device.DeviceMode = Devices.WiFiDeviceService.Mode.Scan;
        }

        private LevelCalculator _calculator;
        private string _selectedSsid = string.Empty;
        private string _selectedMac = string.Empty;
        private int _selectedChannel = -1;

        public string Ssid_Id { get; protected set; }

        public void SelectSsid(string ssid, string mac)
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
                _device.ScanNetworks();
            _selectedChannel = -1;
        }

        public void SelectChannel(int channel)
        {
            Ssid_Id = _selectedSsid;
            _selectedChannel = channel;
            _device.ScanChannel((byte)channel);
            _calculator = new LevelCalculator(_selectedSsid, _selectedMac);
        }

        private async void _device_DataReceived(object sender, BeaconInfoEventArgs e)
        {
            if (e.Info == null)
                return;

            DataReceived?.Invoke(this, e);

            if (Dispatcher == null)
                return;

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
            () =>
            {
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
                    _calculator.HandleInfo(e.Info);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Diff)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Level)));
                }
            });
        }

        public double Diff => _calculator?.GetAvg() ?? 0;
        public double Level => _calculator?.GetCurrent() ?? 0;

        public ObservableCollection<Data.NetworkInfo> NetworkInfo { get; } = new ObservableCollection<Data.NetworkInfo>();
        public ObservableCollection<Data.ChannelInfo> Channels { get; } = new ObservableCollection<Data.ChannelInfo>();

        private WiFiScannerDevice _device;

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion INotifyPropertyChanged implementation

        public event EventHandler<BeaconInfoEventArgs> DataReceived;
    }
}
