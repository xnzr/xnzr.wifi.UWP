using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace WiFiCircles.Devices
{
    class UWPDevice : IDevice, IDisposable
    {
        public UWPDevice(SerialDevice device)
        {
            _device = device;
            _device.BaudRate = 115200;
            _device.DataBits = 8;
            _device.Parity = SerialParity.None;
            _device.StopBits = SerialStopBitCount.One;
            _dataReader = new DataReader(_device.InputStream);
            _dataWriter = new DataWriter(_device.OutputStream);
        }

        private SerialDevice _device;
        private DataReader _dataReader;
        private DataWriter _dataWriter;

        #region IDevice implementation
        bool IDevice.HasData
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        private string dataString = string.Empty;
        private byte _terminator1 = 13;
        private byte _terminator2 = 10;
        private char[] trimChars = { (char)10, (char)13, (char)0 };

        async Task<string> IDevice.ReadLineAsync(CancellationToken token)
        {
            if (_device == null)
                throw new Exception();

            int termPos1 = dataString.IndexOf((char)_terminator1);
            int termPos2 = dataString.IndexOf((char)_terminator2);
            if (termPos2 > -1 && termPos2 > -1)
            {
                string workingString = dataString.Substring(0, termPos2);
                dataString = dataString.Substring(termPos2 + 1);
                workingString = workingString.Trim(trimChars);

                return workingString;
            }
            else
            {
                do
                {
                    uint bytesRead = 0;
                    bytesRead = await _dataReader.LoadAsync(20).AsTask(token);
                    var bytes = new byte[bytesRead];
                    _dataReader.ReadBytes(bytes);
                    dataString += Encoding.ASCII.GetString(bytes, 0, (int)bytesRead);
                } while (dataString.IndexOf((char)_terminator1) < 0 || dataString.IndexOf((char)_terminator2) < 0);

                termPos1 = dataString.IndexOf((char)_terminator1);
                termPos2 = dataString.IndexOf((char)_terminator2);

                if (termPos2 > -1 && termPos2 > -1)
                {
                    string workingString = dataString.Substring(0, termPos2);
                    dataString = dataString.Substring(termPos2 + 1);
                    workingString = workingString.Trim(trimChars);

                    return workingString;
                }

                return null;
            }
        }

        async Task IDevice.WriteAsync(byte data)
        {
            _dataWriter.WriteByte(data);
            await _dataWriter.StoreAsync();
        }

        void IDevice.Reset()
        {
            dataString = string.Empty;
        }
        #endregion IDevice implementation

        #region IDisposable implementation
        public void Dispose()
        {
            if (_dataReader != null)
            {
                _dataReader.Dispose();
                _dataReader = null;
            }
            if (_dataWriter != null)
            {
                _dataWriter.Dispose();
                _dataWriter = null;
            }
            if (_device != null)
            {
                _device.Dispose();
                _device = null;
            }
        }
        #endregion IDisposeble implementation
    }
}
