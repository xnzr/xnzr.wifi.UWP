using System;
using Android.Content;
using Android.Hardware.Usb;
using Android.OS;
using System.Linq;
using Android.App;
using Java.IO;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace WiFiCircles
{
    public class USBCommunicator
    {
        public const string ACTION_USB_PERMISSION = "com.android.example.USB_PERMISSION";

        public USBCommunicator(Context context, Handler handler)
        {
            _context = context;
            _handler = handler;
            _usbManager = (UsbManager)context.GetSystemService(Context.UsbService);

            _permissionIntent = PendingIntent.GetBroadcast(_context, 0, new Intent(ACTION_USB_PERMISSION), 0);
            _usbReceiver = new USBBroadcastReceiver(this);
            _context.RegisterReceiver(_usbReceiver, new IntentFilter(ACTION_USB_PERMISSION));
            _context.RegisterReceiver(_usbReceiver, new IntentFilter(UsbManager.ActionUsbDeviceAttached));
            _context.RegisterReceiver(_usbReceiver, new IntentFilter(UsbManager.ActionUsbDeviceDetached));
        }

        private Context _context = null;
        private Handler _handler = null;
        private UsbManager _usbManager = null;
        private UsbDevice _usbDevice = null;
        public UsbDevice UsbDevice { get { return _usbDevice; } }
        private UsbDeviceConnection _usbConnection;
        private UsbEndpoint _usbReadEndpoint;
        private UsbEndpoint _usbWriteEndpoint;
        private UsbInterface _usbInterface;

        public void Connect()
        {
            IEnumerable<UsbDevice> devices = _usbManager.DeviceList.Values.Where(x => x.VendorId == 0x0483 && x.ProductId == 0x5740);
            if (!devices.Any())
            {
                _handler.SendEmptyMessage(0);
                return;
            }

            _usbDevice = devices.First();
            _usbManager.RequestPermission(_usbDevice, _permissionIntent);
        }

        public void Start()
        {
            _usbConnection = _usbManager.OpenDevice(_usbDevice);
            if (_usbConnection == null)
                throw new NullReferenceException("connection");

            _usbInterface = _usbDevice.GetInterface(1);

            if (!_usbConnection.ClaimInterface(_usbInterface, true))
                throw new Exception("claim interface");

            _usbWriteEndpoint = _usbInterface.GetEndpoint(0);
            _usbReadEndpoint = _usbInterface.GetEndpoint(1);

            ScanNetworks();
        }

        public void ScanNetworks()
        {
            if (_cts != null)
                _cts.Cancel();
            _thread = null;

            _cts = new CancellationTokenSource();

            _run = new ReadingRun(_usbConnection, _usbReadEndpoint, _usbWriteEndpoint, _handler, _cts.Token);
            _thread = new Java.Lang.Thread(_run);
            _thread.Start();
        }

        public void ScanChannel(int channel)
        {
            //if (_cts != null)
            //    _cts.Cancel();
            //_thread = null;

            //_cts = new CancellationTokenSource();

            //_run = new ReadingRun(_usbConnection, _usbReadEndpoint, _usbWriteEndpoint, _handler, _cts.Token, channel);
            //_thread = new Java.Lang.Thread(_run);
            //_thread.Start();
            _run.SetChannel(channel);
        }

        public void Stop()
        {
            _cts.Cancel();
            _cts = null;
            _handler.SendMessage(_handler.ObtainMessage((int)USBDeviceStatus.DeviceConnectionClosed));
            _usbReadEndpoint = null;
            _usbWriteEndpoint = null;
            _usbConnection.Close();
            _usbInterface = null;
            _usbConnection = null;
            _thread = null;

            //Device.DataReceived -= Device_DataReceived;
            //Device.Dispose();
            //Device = null;
        }

        private PendingIntent _permissionIntent;
        private USBBroadcastReceiver _usbReceiver;

        public event EventHandler<BeaconInfoEventArgs> DataReceived;

        #region Device implementation
        private Java.Lang.Thread _thread;
        private ReadingRun _run;

        private CancellationTokenSource _cts;

        private class ReadingRun : Java.Lang.Object, Java.Lang.IRunnable
        {
            public ReadingRun(UsbDeviceConnection connection, UsbEndpoint readEndpoint, UsbEndpoint writeEndpoint, Handler handler, CancellationToken token, int channel = -1)
            {
                _connection = connection;
                _readEndpoint = readEndpoint;
                _writeEndpoint = writeEndpoint;
                _handler = handler;
                _token = token;
                _scanMode = channel < 0;
                _channel = !_scanMode ? channel : 0;
            }

            private UsbDeviceConnection _connection;
            private UsbEndpoint _readEndpoint;
            private UsbEndpoint _writeEndpoint;
            private Handler _handler;
            private CancellationToken _token;
            private Stopwatch _sw = new Stopwatch();
            private int _channel;
            private bool _scanMode;

            private string dataString = string.Empty;
            private byte _terminator1 = 13;
            private byte _terminator2 = 10;
            private char[] trimChars = { (char)10, (char)13, (char)0 };

            public void SetChannel(int channel)
            {
                if (channel >= 0)
                {
                    _channel = channel;
                    _scanMode = false;
                }
                else
                {
                    _channel = 0;
                    _scanMode = true;
                }
            }

            public void Run()
            {
                if (!_scanMode)
                {
                    System.Diagnostics.Debug.WriteLine($"Setting channel to {_channel + 1}");
                    Write(GetChannelByte(0));
                    Write(GetChannelByte((byte)(_channel + 1)));
                }

                byte[] buffer = new byte[100];
                while (!_token.IsCancellationRequested)
                {
                    //Changing channel
                    if (_scanMode && (!_sw.IsRunning || _sw.ElapsedMilliseconds > 2500))
                    {
                        System.Diagnostics.Debug.WriteLine($"Setting channel to {_channel + 1}");
                        Write(GetChannelByte(0));
                        Write(GetChannelByte((byte)(_channel + 1)));
                        _channel = ++_channel % 14;
                        _sw.Restart();
                    }

                    //Reading data
                    int count = _connection.BulkTransfer(_readEndpoint, buffer, buffer.Length, 1000);
                    if (count > 0 && !_token.IsCancellationRequested)
                    {
                        dataString += Encoding.ASCII.GetString(buffer, 0, count);

                        int termPos1, termPos2;

                        do
                        {
                            //Check if string contains the terminator
                            termPos1 = dataString.IndexOf((char)_terminator1);
                            termPos2 = dataString.IndexOf((char)_terminator2);
                            //Console.WriteLine("RAW: '" + tString + "'" +
                            //    " termPoses=" + termPos1 + " " + termPos2 +
                            //    " len " + tString.Length );

                            if (termPos2 > -1 && termPos2 > -1)
                            {

                                string workingString = dataString.Substring(0, termPos2);

                                dataString = dataString.Substring(termPos2 + 1);
                                //Console.WriteLine("NEXT: '" + tString + "'" + " len " + tString.Length);

                                workingString = workingString.Trim(trimChars);
                                //Console.WriteLine("RAW: '" + workingString + "'");

                                //System.Diagnostics.Debug.WriteLine(workingString);

                                try
                                {
                                    BeaconInfoData info = BeaconInfoData.FromString(workingString);

                                    System.Diagnostics.Debug.WriteLine(info);

                                    var msg = _handler.ObtainMessage(111);
                                    msg.Data.PutString("raw", workingString);
                                    _handler.SendMessage(msg);

                                    if (!_scanMode && info.wifiChan != _channel)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Setting channel to {_channel}");
                                        Write(GetChannelByte(0));
                                        Write(GetChannelByte((byte)_channel));
                                    }
                                }
                                catch { }
                            }

                        } while (termPos1 > -1 && termPos2 > -1);
                    }

                }
            }

            private byte GetChannelByte(byte channel)
            {
                return channel < 10 ? (byte)(48 + channel) : (byte)(65 + channel - 10);
            }

            private void Write(byte data)
            {
                _connection.BulkTransfer(_writeEndpoint, new byte[] { data }, 1, 1000);
            }
        }
        #endregion Device implementation
    }
}

