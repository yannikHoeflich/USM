using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using System;
using USM.Devices.Packaging;
using USM.Devices.UdpNetworking;

namespace USM.Devices {
    public abstract partial class Device : IDevice {
        public IPEndPoint EndPoint { get; }
        public int ProtocolVersion { get; protected set; }
        public int Id { get; protected set; }
        public DeviceType DeviceType { get; protected set; }

        protected Device(IPEndPoint endPoint) {
            this.EndPoint = endPoint;
        }

        protected abstract Task SpecificInitAsync();

        internal abstract void SpecificInitWithPackage(ResponseStream response);

        public void Init() {
            InitAsync().Wait();
        }

        public async Task InitAsync() => await InitAsync(true);
        public async Task InitAsync(bool includeSpecificInit) {
            var answerStart = GenerateMessageStart(2);
            await CachingUdpClient.SendAsync(new Package(2, this).Build(), this.EndPoint);
            var endpoint = this.EndPoint;
            byte[] buffer = (await CachingUdpClient.ReceiveAsync(endpoint, answerStart)).Buffer;
            var response = new ResponseStream(buffer);
            InitWithPackage(response);

            if(includeSpecificInit) {
                await SpecificInitAsync();
            }
        }

        internal void InitWithPackage(ResponseStream response) {
            response.DropDefaultValues(Id != 0);
            ProtocolVersion = response.GetInt32();
            Id = response.GetInt32();
            DeviceType = (DeviceType)response.GetInt8();
        }


        protected static long[] ParsePackage(byte[] package, params int[] byteLengths) {
            LinkedList<long> numbers = new LinkedList<long>();
            int i = 0;
            foreach (var length in byteLengths) {
                numbers.AddLast(length switch {
                    1 => package[i],
                    2 => BitConverter.ToInt16(package.Skip(i).Take(length).ToArray()),
                    4 => BitConverter.ToInt32(package.Skip(i).Take(length).ToArray()),
                    8 => BitConverter.ToInt64(package.Skip(i).Take(length).ToArray()),
                    _ => 0
                });
                i += length;
            }
            return numbers.ToArray();
        }
        public override string ToString() {
            return $"{this.DeviceType} {this.EndPoint}";
        }


        protected byte[] GenerateMessageStart(byte requestType)
                            => this.ProtocolVersion > 1
                            ? new byte[] { 0 }.Concat(BitConverter.GetBytes(this.Id)).Add(requestType).ToArray()
                            : new byte[] { 0, requestType };
    }
    public enum DeviceType {
        Light,
        Sensor,
        Matrix
    }
}