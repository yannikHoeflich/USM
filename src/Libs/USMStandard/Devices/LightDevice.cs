using System.Net;
using System.Net.WebSockets;
using System.Threading.Tasks;

using System;
using USM.Devices.Packaging;
using USM.Devices.UdpNetworking;

namespace USM.Devices {
    public class LightDevice : Device {
        private double brightness = 1;

        public int LedCount { get; protected set; }
        public ColorSpectrum ColorSpectrum { get; protected set; }
        public double Brightness { get => brightness; set => SendBrightness(value); }

        public LightDevice(IPEndPoint endPoint) : base(endPoint) {
            this.DeviceType = DeviceType.Light;
        }

        protected override async Task SpecificInitAsync() {
            byte[] request = new Package(3, this).Build();
            await CachingUdpClient.SendAsync(request, EndPoint);

            byte[] answerStart = GenerateMessageStart(3);
            ResponseStream buffer = (await CachingUdpClient.ReceiveAsync(EndPoint, answerStart)).GetResponseStream();
            SpecificInitWithPackage(buffer);
        }

        internal override void SpecificInitWithPackage(ResponseStream response) {
            response.DropDefaultValues(ProtocolVersion > 1);
            LedCount = response.GetInt32();
            ColorSpectrum = (ColorSpectrum)response.GetInt8();
        }

        public void SendAnimation(byte[] byteCode) {
            var package = new Package(16, this)
                                    .Add(byteCode.Length)
                                    .Add(byteCode)
                                    .Build();

            CachingUdpClient.Send(package, EndPoint);
        }

        public Task SendAnimationAsync(byte[] byteCode) {
            var package = new Package(16, this)
                                    .Add(byteCode.Length)
                                    .Add(byteCode)
                                    .Build();

            return CachingUdpClient.SendAsync(package, EndPoint);
        }

        public Task SendBrightnessAsync(double brightness) {
            if (brightness < 0 || brightness > 1)
                throw new ArgumentException("The brightness needs to be between 0 and 1 (0 and 1 are allowed)", nameof(brightness));
            this.brightness = brightness;
            byte value = (byte)(255 * brightness);

            byte[] package = new Package(17, this)
                                    .Add(value)
                                    .Build();

            return CachingUdpClient.SendAsync(package, this.EndPoint);
        }

        public void SendBrightness(double brightness) {
            if (brightness < 0 || brightness > 1)
                throw new ArgumentException("The brightness needs to be between 0 and 1 (0 and 1 are allowed)", nameof(brightness));
            this.brightness = brightness;
            byte value = (byte)(255 * brightness);

            byte[] package = new Package(17, this)
                                    .Add(value)
                                    .Build();

            CachingUdpClient.Send(package, this.EndPoint);
        }

    }

    public enum ColorSpectrum {
        OnOff,
        Dimmable,
        Rgb
    }
}