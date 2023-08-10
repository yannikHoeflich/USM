using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace USM.Devices {
    internal class DataDevice : IDevice {
        public DeviceType DeviceType {get;set;}

        public IPEndPoint EndPoint { get; set; }

        public int Id { get; set; }

        public int ProtocolVersion { get; set; }

        public Task InitAsync() => Task.CompletedTask;
    }
}
