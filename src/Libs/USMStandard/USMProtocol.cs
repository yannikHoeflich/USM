using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Threading.Tasks;

namespace USM {

    public class USMProtocol {
        public const int VersionId = 2;
        public const int ScanResponseInt = 0x6DDFBB37;
        public const int ScanRequestInt = 0x20DFB272;
        public const int DefaultPort = 4210;

        [Obsolete]
        public IAsyncEnumerable<IPEndPoint> ScanSaveAsync(int port = DefaultPort) {
            IPAddress localIP;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address;
            }

            return ScanSaveAsync(localIP, port);
        }

        [Obsolete]
        public async IAsyncEnumerable<IPEndPoint> ScanSaveAsync(IPAddress address, int port = DefaultPort) {
            string ipStart = string.Join(".", address.ToString().Split('.')[..^1]);

            var client = new UdpClient();
            byte[] request = new byte[] { 1 };
            byte[] response = new byte[] { 0, 1 }.Concat(BitConverter.GetBytes(ScanResponseInt)).ToArray();
            await Task.WhenAll(Enumerable.Range(1, 254).Select(x => client.SendAsync(request, request.Length, new IPEndPoint(IPAddress.Parse(ipStart + $".{x}"), port))));

            await foreach(var ip in GetAllResponsesAsync(port, response, client)) {
                yield return ip;
            }
        }

        [Obsolete]
        public IAsyncEnumerable<IPEndPoint> ScanAsync(int port = DefaultPort) {
            var addresses = Dns.GetHostAddresses(Dns.GetHostName()).Where(x => x.GetAddressBytes().Length == 4).ToArray();
            return ScanAsync(port, addresses);
        }

        [Obsolete]
        public IAsyncEnumerable<IPEndPoint> ScanAsync(int port = DefaultPort, params IPAddress[] addresses) {
            var client = new UdpClient();
            byte[] request = new byte[] { 1 };
            byte[] response = new byte[] { 0, 1 }.Concat(BitConverter.GetBytes(ScanResponseInt)).ToArray();
            foreach (var ip in addresses) {
                var ipParts = ip.ToString().Split('.');
                var broadcastIp = string.Join(".", ipParts[..^1]) + ".255";
                var endpoint = new IPEndPoint(IPAddress.Parse(broadcastIp), port);
                client.Send(request, request.Length, endpoint);
            }
            return GetAllResponsesAsync(port, response, client);
        }
        private async IAsyncEnumerable<IPEndPoint> GetAllResponsesAsync(int port, byte[] response, UdpClient client) {
            while (true) {
                IPEndPoint endpoint;
                byte[] buffer;
                try {
                    var receiveTask = client.ReceiveAsync();
                    await Task.WhenAny(receiveTask, Task.Delay(5000));
                    if (!receiveTask.IsCompleted)
                        break;
                    var udpResponse = await receiveTask;
                    buffer = udpResponse.Buffer;
                    if (!Enumerable.SequenceEqual(buffer, response))
                        continue;
                    endpoint = udpResponse.RemoteEndPoint;
                } catch {
                    break;
                }
                yield return endpoint;
            }
        }

        [Obsolete]
        public IEnumerable<IPEndPoint> Scan(int port = DefaultPort) {
            var addresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(x => x.GetAddressBytes().Length == 4).ToArray();
            return Scan(port, addresses);
        }

        [Obsolete]
        public IEnumerable<IPEndPoint> ScanSave( IPAddress address, int port = DefaultPort) {
            string ipStart = string.Join(".", address.ToString().Split('.')[..^1]);

            var client = new UdpClient();
            byte[] request = new byte[] { 1 };
            byte[] response = new byte[] { 0, 1 }.Concat(BitConverter.GetBytes(ScanResponseInt)).ToArray();
            for(int i = 1; i < 255; i++) { 
                var ip = ipStart + $".{i}";
                var endpoint = new IPEndPoint(IPAddress.Parse(ip), port);
                client.Send(request, request.Length, endpoint);
            }

            return GetAllResponses(port, response, client);
        }

        [Obsolete]
        public IEnumerable<IPEndPoint> Scan(int port = DefaultPort, params IPAddress[] addresses) {
            var client = new UdpClient();
            byte[] request = new byte[] { 1 };
            byte[] response = new byte[] { 0, 1 }.Concat(BitConverter.GetBytes(ScanResponseInt)).ToArray();
            foreach (var ip in addresses) {
                var ipParts = ip.ToString().Split('.');
                var broadcastIp = string.Join(".", ipParts[..^1]) + ".255";
                var endpoint = new IPEndPoint(IPAddress.Parse(broadcastIp), port);
                client.Send(request, request.Length, endpoint);
            }
            return GetAllResponses(port, response, client);
        }
        private IEnumerable<IPEndPoint> GetAllResponses(int port, byte[] response, UdpClient client) {
            client.Client.ReceiveTimeout = 3000;
            LinkedList<IPEndPoint> endpoints = new LinkedList<IPEndPoint>();
            while (true) {
                var endpoint = new IPEndPoint(IPAddress.Any, port);
                byte[] buffer;
                try {
                    buffer = client.Receive(ref endpoint);
                    Console.WriteLine("responded: " + endpoint);
                } catch {
                    break;
                }
                if (Enumerable.SequenceEqual(buffer, response)) {
                    endpoints.AddLast(endpoint);
                    Console.WriteLine($"added: {endpoint}");
                } else {
                    Console.WriteLine($"wrong: {endpoint}  - {buffer.Length} => {new BigInteger(buffer)} != {new BigInteger(response)}");
                }
            }
            return endpoints.ToArray();
        }
    }
}