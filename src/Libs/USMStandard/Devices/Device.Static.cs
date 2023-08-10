using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using System;
using USM;
using USM.Devices;
using USM.Devices.Packaging;
using USM.Devices.UdpNetworking;

namespace USM.Devices {
    public partial class Device {

        protected static byte[] CreatePackage(out int length, IDevice? device, byte packageType, params object[] parts) {
            LinkedList<byte> package = new LinkedList<byte>();
            package.AddLast(packageType);
            if (device != null && device.ProtocolVersion > 1) 
                package.AddRange(BitConverter.GetBytes(device.Id));
            foreach (var part in parts) {
                if (part is byte b)
                    package.AddLast(b);
                else if (part is short s)
                    package.AddRange(BitConverter.GetBytes(s));
                else if (part is int i)
                    package.AddRange(BitConverter.GetBytes(i));
                else if (part is long l)
                    package.AddRange(BitConverter.GetBytes(l));
                else if (part is byte[] a)
                    package.AddRange(a);
                else if (part is Enum e)
                    package.AddRange(BitConverter.GetBytes((int)part));
            }
            length = package.Count;
            return package.ToArray();
        }

        public static async Task<Device> CreateDeviceAsync(IPEndPoint endPoint, int id = 0) {
            try {
                byte[] package = id != 0
                    ? CreatePackage(out int length, new DataDevice() { Id = id, ProtocolVersion = 2 }, 2)
                    : CreatePackage(out length, null, 2);

                await CachingUdpClient.SendAsync(package, endPoint);
                byte[] buffer;

                byte[] messageStart = id != 0
                                ? new byte[] { 0 }.Concat(BitConverter.GetBytes(id)).Add((byte)2).ToArray()
                                : new byte[] { 0, 2 };

                var timeout = TimeSpan.FromSeconds(2);
                var result = await Task.WhenAny(CachingUdpClient.ReceiveAsync(endPoint, messageStart, timeout));
                buffer = result.Result.Buffer;

                var parsed = id != 0
                    ? ParsePackage(buffer, 1, 4, 1, 4, 4, 1)[3..]
                    : ParsePackage(buffer, 1, 1, 4, 4, 1)[2..];
                int protocolVersion = (int)parsed[0];
                var deviceType = (DeviceType)parsed[2];
                endPoint = endPoint.Clone();
                return deviceType switch {
                    DeviceType.Light => new LightDevice(endPoint) { ProtocolVersion = protocolVersion, Id = id },
                    DeviceType.Sensor => new SensorDevice(endPoint) { ProtocolVersion = protocolVersion, Id = id },
                    DeviceType.Matrix => new MatrixDevice(endPoint) { ProtocolVersion = protocolVersion, Id = id},
                    _ => throw new NotSupportedException("The DeviceType that the device responded is invalid.")
                };
            } catch (Exception ex) {
                return null;
            }
            }

        public static async IAsyncEnumerable<IDevice> ScanAsync(int port = USMProtocol.DefaultPort, IEnumerable<IPEndPoint>? endPoints = null) {
            byte[] request = new Package(1, new DataDevice() { Id = 0, ProtocolVersion = 2 })
                                        .Add(USMProtocol.ScanRequestInt)
                                        .Build();

            byte[] oldResponseStart = new byte[] { 0, 1 }.Concat(BitConverter.GetBytes(USMProtocol.ScanResponseInt)).ToArray();
            byte[] newResponseStart = new byte[] { 0 }.Concat(BitConverter.GetBytes(0)).Add((byte)1).Concat(BitConverter.GetBytes(USMProtocol.ScanResponseInt)).ToArray();


            IPAddress[] ips = GetLocalIPs();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                var broadcastIps = ips.Select(x => string.Join('.', x.ToString().Split('.')[..^1].Append("255")));
                await Task.WhenAll(broadcastIps.Select(x => CachingUdpClient.SendAsync(request, new IPEndPoint(IPAddress.Parse(x), port))));
            } else {
                foreach (var ip in ips) {
                    if (ip is null)
                        yield break;
                    IPAddress localIP = ip.MapToIPv4();

                    string ipStart = string.Join(".", localIP.ToString().Split('.')[..^1]);
                    await Task.WhenAll(Enumerable.Range(1, 254).Select(x => CachingUdpClient.SendAsync(request, new IPEndPoint(IPAddress.Parse(ipStart + $".{x}"), port))));
                }
            }

            Queue<IDevice> devices = new Queue<IDevice>();
            var task = Task.Run(async () => {
                while (true) {
                    UdpReceiveResult scanResponse;
                    try {
                        var timeout = TimeSpan.FromSeconds(3);
                        var task = await Task.WhenAny(CachingUdpClient.ReceiveAsync(oldResponseStart, timeout), CachingUdpClient.ReceiveAsync(newResponseStart, timeout));
                        if(!(task.Exception is null)) {
                            break;
                        }
                        scanResponse = task.Result;
                    } catch (TimeoutException) {
                        break;
                    }


                    if (scanResponse.Buffer.StartWith(oldResponseStart) || scanResponse.Buffer.StartWith(newResponseStart)) {
                        if (scanResponse.Buffer.Length == oldResponseStart.Length) {
                            Task.Run(async () => {
                                var newDevice = await CreateDeviceAsync(scanResponse.RemoteEndPoint);
                                lock (devices) {
                                    devices.Enqueue(newDevice);
                                }
                            });
                            continue;
                        }

                        byte[] idBuffer = scanResponse.Buffer[newResponseStart.Length..];
                        int[] ids = new int[idBuffer.Length / 4];

                        for (int i = 0; i < ids.Length; i++) {
                            ids[i] = BitConverter.ToInt32(idBuffer, i * 4);
                        }

                        var newDevices = await Task.WhenAll(ids.Select(id => CreateDeviceAsync(scanResponse.RemoteEndPoint, id)));

                        devices.EnqueueAll(newDevices);
                    }
                }
            });

            while (!task.IsCompleted) {
                if (devices.Any()) {
                    while (devices.Count > 0) {
                        yield return devices.Dequeue();
                    }
                }
                await Task.Delay(1);
            }

            lock (devices) {
                while (devices.Count > 0) {
                    yield return devices.Dequeue();
                }
            }
        }


        private static IPAddress[] GetLocalIPs() {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                return Dns.GetHostAddresses(Dns.GetHostName()).Where(x => x.GetAddressBytes().Length == 4).ToArray();
            } else {
                IPAddress localIP;
                using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                if (!(socket.LocalEndPoint is IPEndPoint endPoint))
                    return Array.Empty<IPAddress>();
                localIP = endPoint.Address;
                return new IPAddress[] { localIP };
            }
        }

        public static async Task InitAllDevicesSave(IEnumerable<IDevice> devices) {

        }
    }
}
