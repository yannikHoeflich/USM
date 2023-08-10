using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System;
using USM.Devices.Packaging;
using USM.Devices.UdpNetworking;

namespace USM.Devices {
    public class SensorDevice : Device {
        public UpdateMethod UpdateMethod { get; protected set; }
        public int UpdateFrequency { get; set; }

        public double Value { get; protected set; }

        public int Decimals { get; protected set; }

        public string Suffix { get; set; } = "";

        public SensorDevice(IPEndPoint endPoint) : base(endPoint) {
            this.DeviceType = DeviceType.Sensor;
        }

        protected override async Task SpecificInitAsync() {
            byte[] answerStart = GenerateMessageStart(3);
            await CachingUdpClient.SendAsync(CreatePackage(out _, this, 3), EndPoint);

            ResponseStream responseStream = (await CachingUdpClient.ReceiveAsync(EndPoint, answerStart)).GetResponseStream();
            SpecificInitWithPackage(responseStream);
        }

        internal override void SpecificInitWithPackage(ResponseStream response) {
            response.DropDefaultValues(ProtocolVersion > 1);
            UpdateMethod = (UpdateMethod)response.GetInt8();
            UpdateFrequency = response.GetInt32();
            Decimals = response.GetInt8();
            ReadOnlySpan<byte> suffixBuffer = response.GetBuffer(8);
            Suffix = Encoding.UTF8.GetString(suffixBuffer).TrimEnd('\0');
        }

        public Task<double> GetValueAsync() => GetValueAsync(null);

        public async Task<double> GetValueAsync(TimeSpan? timeout) {
            var answerStart = GenerateMessageStart(16);

            CachingUdpClient.Send(CreatePackage(out int length, this, 16), length, this.EndPoint);
            var endpoint = this.EndPoint;
            var response = await CachingUdpClient.ReceiveAsync(endpoint, answerStart, timeout);
            var buffer = response.Buffer;
            if (buffer == null) {
                this.Value = 0;
                return 0;
            }

            var parsed = ParsePackage(buffer, 1, 4, 1, 8)[3..];
            var value = BitConverter.Int64BitsToDouble( parsed[0]);
            this.Value = value;

            return value;
        }

        public void AddToUpdate() {
            CachingUdpClient.Send(CreatePackage(out int length,this, 17), length, this.EndPoint);
        }
        public void RemoveFromUpdate() {
            CachingUdpClient.Send(CreatePackage(out int length,this, 18), length, this.EndPoint);
        }

        public async Task StartAutoUpdate(CancellationToken? cancellationToken = null) {
            //AddToUpdate();
            try {
                if (UpdateMethod == UpdateMethod.RequestByHost) {
                    while (cancellationToken == null || !cancellationToken.Value.IsCancellationRequested) {
                        try {
                            await GetValueAsync();
                        } catch { }
                        await Task.Delay(UpdateFrequency);
                    }
                } else if (UpdateMethod == UpdateMethod.SendWhenChange) {
                    while (cancellationToken == null || !cancellationToken.Value.IsCancellationRequested) {
                        var response = await CachingUdpClient.ReceiveAsync(this.EndPoint);
                        var parsed = ParsePackage(response.Buffer, 1, 2)[1..];
                        var value = parsed[0] * 1.0 / short.MaxValue;
                    }
                } else {
                    throw new ArgumentException("The UpdateMethod is not supported.", nameof(UpdateMethod));
                }
            } finally {
                //RemoveFromUpdate();
            }
        }

        protected string GetStringFormatted() {
            var valueStr = Value.ToString($"N{Decimals}");

            return string.Concat(valueStr, Suffix);
        }

        public string ValueString => GetStringFormatted();
    }

    public enum UpdateMethod {
        RequestByHost,
        SendWhenChange
    }
}