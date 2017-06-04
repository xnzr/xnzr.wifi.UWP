using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WiFiCircles
{
    public interface IDevice
    {
        bool HasData { get; }
        Task<string> ReadLineAsync(CancellationToken token);
        Task WriteAsync(byte data);
        Task WriteAsync(string data);
        void Reset();
    }
}
