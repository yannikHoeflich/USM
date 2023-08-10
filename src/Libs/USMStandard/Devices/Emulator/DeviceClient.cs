using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace USM.Devices.Emulator {
    public class DeviceClient {
        private const int ScanInt = 0x6DDFBB37;
        private const int UsmVersion = 1;

        private UdpClient _listener;
        private int _port;
        
        public int Id { get; }
        public int LedCount { get; }

        public event Func<NewAnimationEventArgs, Task>? NewAnimationReceived;
        public event Func<NewDimmingEventArgs, Task>? NewDimmingReceived;

        public DeviceClient(int id, int ledCount, int port = USMProtocol.DefaultPort) {
            _listener = new UdpClient(port);
            _port = port;
            Id = id;
            LedCount = ledCount;
        }

        public async Task StartListening(CancellationToken cancellationToken) {
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, _port);
            while (!cancellationToken.IsCancellationRequested) {
                var result = await Task.Run(_listener.ReceiveAsync, cancellationToken);
                var response = await ProcessPackage(result.Buffer);
                if (response == null)
                    continue;
                await _listener.SendAsync(response, response.Length, result.RemoteEndPoint);
            }
        }
        
        public async Task<byte[]?> ProcessPackage(byte[] buffer) {
            if (buffer.Length == 0)
                return null;
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            writer.Write((byte)0);
            writer.Write(buffer[0]);
            switch (buffer[0]) {
                case 1:
                    writer.Write(ScanInt);
                    break;
                case 2:
                    writer.Write(UsmVersion);
                    writer.Write(Id);
                    writer.Write((byte)0);
                    break;
                case 3:
                    writer.Write(LedCount);
                    writer.Write((byte)2);
                    break;
                case 16:
                    if (NewAnimationReceived != null)
                        await NewAnimationReceived.Invoke(new NewAnimationEventArgs(buffer[5..]));
                    break;
                case 17:
                    if(NewDimmingReceived!= null)
                        await NewDimmingReceived.Invoke(new NewDimmingEventArgs((double)buffer[1] / 255));
                    break;
            }

            var response = stream.ToArray();
            return response.Length > 2 ? response : null;
        }
    }
}
