using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using System;
using USM.Devices.Packaging;
using USM.Devices.UdpNetworking;

namespace USM.Devices {
    public class MatrixDevice : Device {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public ColorSpectrum ColorSpectrum { get; private set; }

        public MatrixDevice(IPEndPoint endPoint) : base(endPoint) {
            DeviceType = DeviceType.Matrix;
        }

        protected override async Task SpecificInitAsync() {
            byte[] request = new Package(3, this).Build();


            await CachingUdpClient.SendAsync(request, EndPoint);

            byte[] answerStart = GenerateMessageStart(3);
            ResponseStream response = (await CachingUdpClient.ReceiveAsync(EndPoint, answerStart)).GetResponseStream();

            SpecificInitWithPackage(response);
        }
        
        internal override void SpecificInitWithPackage(ResponseStream response) {
            response.DropDefaultValues(ProtocolVersion > 1);

            Width = response.GetInt32();
            Height = response.GetInt32();
            ColorSpectrum = (ColorSpectrum)response.GetInt8();
        }

        public async Task SendGifAsync(byte[] buffer) {
            byte[] package = new Package(19, this)
                                        .Add(buffer.Length)
                                        .Add(buffer)
                                        .Build();

            await CachingUdpClient.SendAsync(package, EndPoint);
        }

        public async Task SendAnimationAsync(byte[] buffer) {
            byte[] package = new Package(20, this)
                                        .Add(buffer.Length)
                                        .Add(buffer)
                                        .Build();

            await CachingUdpClient.SendAsync(package, EndPoint);
        }

        public Task SendBrightnessAsync(double brightness) {
            if (brightness < 0 || brightness > 1)
                throw new ArgumentException("The brightness needs to be between 0 and 1 (0 and 1 are allowed)", nameof(brightness));
            byte value = (byte)(255 * brightness);

            byte[] package = new Package(21, this)
                                    .Add(value)
                                    .Build();

            return CachingUdpClient.SendAsync(package, EndPoint);
        }

    }
}
