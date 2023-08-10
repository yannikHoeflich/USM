using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using USM;
using USM.Devices;
using USM.Lal.Compiler;

namespace ScreenColor;
internal class DeviceManager {
    public LightDevice[]? Devices { get; private set; }

    public async Task InitAsync() {

        var endpoints = await new USMProtocol().ScanAsync().ToLinkedListAsync();
        var rawDevices = await Task.WhenAll(endpoints.Select(x => Device.CreateDeviceAsync(x)));
        await Task.WhenAll(rawDevices.Select(x => x.InitAsync()));
        Devices = rawDevices.Where(x => x is LightDevice).Select(x => (LightDevice)x).Where(x => x.ColorSpectrum == ColorSpectrum.Rgb).ToArray();
    }

    public async Task SendColorAsync(Color color) {
        if (Devices == null)
            throw new InvalidOperationException("The DeviceManager has to call InitAsync first");

        byte[] colorByteCode = DynamicCompiler.OneColor(color.R, color.G, color.B);
        await Task.WhenAll(Devices.Select(x => x.SendAnimationAsync(colorByteCode)));
    }

}
