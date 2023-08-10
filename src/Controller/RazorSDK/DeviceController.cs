using Colore;
using Colore.Data;
using Colore.Effects.Keyboard;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using USM.Lal.Runtime;

namespace RazorSDK;
internal class DeviceController {
    private IChroma? _provider;
    private AsyncLalRuntime? _runtime;

    private bool _waitForAnimationEnd = false;

    private Color[] _colors;

    private System.Drawing.Size _keyboardSize;

    public int LedCount => _colors.Length;

    public async Task InitAsync() {
        _provider = await ColoreProvider.CreateNativeAsync();
        _colors = new Color[ await GetTotalLength()];
    }

    public async Task RunAnimation(byte[] animation) {
        _waitForAnimationEnd = true;
        while (_runtime != null) {
            await Task.Delay(10);
        }
        _waitForAnimationEnd = false;
        var rt = new AsyncLalRuntime(animation);
        rt.LedCount = (short)LedCount;
        rt.SetLed = (index, r, g, b) => {
            return SetColor(index, r, g, b);
        };
        rt.UpdateLeds = () => UpdateLeds();
        rt.ShouldStop = () => {
            if (_waitForAnimationEnd) {
                return true;
            }
            return false;
        };
        _runtime = rt;
        Task.Run(async () => {
            await rt.Run();
            _runtime = null;
        });
    }

    private Task SetColor(short index, short r, short g, short b) {
        _colors[index] = new Color((byte) r, (byte) g, (byte) b);

        return Task.CompletedTask;
    }

    private async Task UpdateLeds() {

        var customEffect = CustomKeyboardEffect.Create();
        
        short index = 0;
        for (int x = 0; x < _keyboardSize.Width; x++) {
            for (int y = 0; y < _keyboardSize.Height; y++) {
                customEffect[y, x] = _colors[index];
                index++;
            }
        }
        await _provider.Keyboard.SetCustomAsync(customEffect);
    }

    #region GetSizes
    private async Task<int> GetTotalLength() {
        var keyboardSize = await GetKeyboardSize();
        return (keyboardSize.Width * keyboardSize.Height);
    }

    private async Task<System.Drawing.Size> GetKeyboardSize() {
        var keyboard = _provider.Keyboard;
        int width = 0;
        while (true) {
            try {
                await keyboard.SetPositionAsync(0, width, Color.Black);
            } catch { break; }
            Thread.Sleep(100);
            width++;
        }

        int height = 0;
        while (true) {
            try {
                await keyboard.SetPositionAsync(height, 0, Color.Black);
            } catch { break; }
            Thread.Sleep(100);
            height++;
        }
        await keyboard.ClearAsync();
        var size = new System.Drawing.Size(width, height);
        _keyboardSize = size;
        return size;
    }
    #endregion
}
