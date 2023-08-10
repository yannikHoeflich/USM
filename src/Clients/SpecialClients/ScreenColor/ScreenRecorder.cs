using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenColor;

[SupportedOSPlatform("windows")]
internal class ScreenRecorder {
    private Bitmap _bitmap;

    public ScreenRecorder() {
        Rectangle bounds = Screen.PrimaryScreen.Bounds;
        _bitmap = new Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
    }
    public async Task StartRecording(FrameTimer frameTime, CancellationToken cancellationToken) {

        using var graphics = Graphics.FromImage(_bitmap);
        while (!cancellationToken.IsCancellationRequested) {
            frameTime.StartFrame();

            lock (_bitmap) {
                graphics.CopyFromScreen(0, 0, 0, 0, _bitmap.Size);
            }

            await frameTime.WaitFrame(cancellationToken);
        }
    }

    public ColorArray GetCurrentColorArray() {
        lock (_bitmap) {
            return ColorArray.CopyFromBitmap(_bitmap);
        }
    }
}
