using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System;

namespace USM.Devices.UdpNetworking
{
    public class CachingUdpClient
    {
        private static bool _running = false;
        private static readonly UdpClient _client = new();
        private static readonly List<Message> _receiveResults = new();


        public static async Task Start() {
            new Thread(async () => {
                if (_running)
                    return;
                _running = true;
                while (_running) {
                    try {
                        var newRequest = await _client.ReceiveAsync();
                        lock (_receiveResults) {
                            _receiveResults.Add(new Message(newRequest));
                            if (_receiveResults.Count > 1000) {
                                DateTime now = DateTime.Now;
                                var OneMin = TimeSpan.FromMinutes(1);
                                _receiveResults.RemoveAll(x => now - x.TimeStamp > OneMin);
                            }
                        }
                    } catch {

                    }
                }
            }).Start();
        }

        public static Task<int> SendAsync(byte[] data, IPEndPoint endPoint)
            => SendAsync(data, data.Length, endPoint);

        public static async Task<int> SendAsync(byte[] data, int length, IPEndPoint endPoint) {
            if (!_running) {
                _ = Start();
            }

            return await _client.SendAsync(data, length, endPoint);
        }

        public static int Send(byte[] data, IPEndPoint endPoint)
            => Send(data, data.Length, endPoint);

        public static int Send(byte[] data, int length, IPEndPoint endPoint) {
            if (!_running) {
                _ = Start();
            }

            return _client.Send(data, length, endPoint);
        }

        public static Task<UdpReceiveResult> ReceiveAsync(IPEndPoint remoteEndPoint, TimeSpan? timeout = null) 
            => ReceiveAsync(remoteEndPoint, null, timeout);

        public static Task<UdpReceiveResult> ReceiveAsync(byte[] startBytes, TimeSpan? timeout = null) 
            => ReceiveAsync(null, startBytes, timeout);

        public static async Task<UdpReceiveResult> ReceiveAsync(IPEndPoint? remoteEndPoint, byte[]? startBytes, TimeSpan? timeout = null)
        {
            if (!_running) {
                _ = Start();
            }

            UdpReceiveResult? result = await InternReceiveAsync(remoteEndPoint, startBytes, timeout);

            return result is not null
                ? result.Value
                : throw new TimeoutException();
        }

        public static async Task<UdpReceiveResult?> InternReceiveAsync(IPEndPoint? remoteEndPoint, byte[]? startBytes, TimeSpan? timeout) {
            Predicate<Message> predicate;

            if (remoteEndPoint != null && startBytes != null)
                predicate = x => x.Result.RemoteEndPoint.Equals(remoteEndPoint) && x.Result.Buffer.StartWith(startBytes);
            else if (remoteEndPoint != null)
                predicate = x => x.Result.RemoteEndPoint.Equals(remoteEndPoint);
            else if (startBytes != null)
                predicate = x => x.Result.Buffer.StartWith(startBytes);
            else predicate = x => true;


            long startTicks = Environment.TickCount64;
            long timeoutTicks = long.MaxValue;
            if (timeout is not null)
                timeoutTicks = (long)timeout.Value.TotalMilliseconds;

            while (Environment.TickCount64 - startTicks < timeoutTicks) {
                Message? result;
                lock (_receiveResults) {
                    result = _receiveResults.Find(predicate);
                }

                if (result is not null) {
                    lock (_receiveResults) {
                        _receiveResults.Remove(result);
                    }

                    return result.Result;
                }

                await Task.Delay(1);
            }

            return null;
        }

        public static void Stop()
        {
            _running = false;
        }
    }
}
