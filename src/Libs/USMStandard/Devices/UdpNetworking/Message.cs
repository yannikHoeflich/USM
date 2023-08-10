using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace USM.Devices.UdpNetworking {
    internal class Message {
        public UdpReceiveResult Result { get; set; }
        public DateTime TimeStamp { get; set; }

        public Message(UdpReceiveResult result) {
            Result = result;
            TimeStamp = DateTime.Now;
        }
    }
}
