using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WiFiCircles
{
    public sealed class WiFiScannerDevice : IDisposable
    {
        public WiFiScannerDevice(IDevice device)
        {
            _device = device;
        }

        private IDevice _device;

        #region IDisposable implementation
        public void Dispose()
        {
            Stop();
        }
        #endregion IDisposable implementation

        #region Public methods
        public void ScanNetworks()
        {
            Stop();
            _cts = new CancellationTokenSource();
            DoScanNetworks(_cts.Token);
        }

        public void ScanChannel(byte channel)
        {
            Stop();
            _cts = new CancellationTokenSource();
            DoScanChannel(channel, _cts.Token);
        }

        public bool IsWorking => _cts != null;

        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts = null;
            }
        }
        #endregion Public methods

        #region Private methods
        private CancellationTokenSource _cts = null;
        private async void DoScanNetworks(CancellationToken token)
        {
            bool reading = false;
            int channel = 0;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    //System.Diagnostics.Debug.WriteLine($"Setting channel to {channel + 1}");
                    await _device.WriteAsync(GetChannelByte(0));
                    await _device.WriteAsync(GetChannelByte((byte)(channel + 1)));
                    if (!reading)
                    {
                        ReadingData(token);
                        reading = true;
                    }
                    await Task.Delay(2500, token);
                    channel = ++channel % 14;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async void DoScanChannel(byte channel, CancellationToken token)
        {
            await _device.WriteAsync(GetChannelByte(0));
            await _device.WriteAsync(GetChannelByte(channel));

            ReadingData(token);
        }

        private async void ReadingData(CancellationToken token)
        {
            while (_device != null && !token.IsCancellationRequested)
            {
                try
                {
                    string workingString = await _device.ReadLineAsync(token);
                    //System.Diagnostics.Debug.WriteLine(workingString);

                    BeaconInfoData info = BeaconInfoData.FromString(workingString);

                    //System.Diagnostics.Debug.WriteLine(info);

                    DataReceived?.Invoke(this, new BeaconInfoEventArgs(info, workingString));
                }
                catch(TaskCanceledException)
                {
                    _device.Reset();
                }
            }
            _device.Reset();
        }

        private byte GetChannelByte(byte channel)
        {
            return channel < 10 ? (byte)(48 + channel) : (byte)(65 + channel - 10);
        }
        #endregion Private methods

        public List<Data.NetworkInfo> Networks { get; } = new List<Data.NetworkInfo>();

        public event EventHandler<BeaconInfoEventArgs> DataReceived;
    }

    public class BeaconInfoEventArgs : EventArgs
    {
        public BeaconInfoEventArgs(BeaconInfoData info, string raw)
        {
            Info = info;
            Raw = raw;
        }

        public BeaconInfoData Info { get; private set; }
        public string Raw { get; private set; }
    }
}
