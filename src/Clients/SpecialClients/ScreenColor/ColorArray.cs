using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace ScreenColor;

[SupportedOSPlatform("windows")]
internal unsafe class ColorArray : IDisposable {
    private byte* _ptr;
    private bool _disposedValue;
    private readonly nuint _length;

    public PixelFormat PixelFormat { get; }

    public bool HasAlpha => this.PixelFormat != PixelFormat.Format24bppRgb;
    public int Length => (int)(_length / (nuint)(HasAlpha ? 4 : 3));
    private ColorArray(byte* ptr, nuint length, PixelFormat pixelFormat) {
        this._ptr = ptr;
        this._length = length;
        this.PixelFormat = pixelFormat;
    }

    public Color GetAvarageColor() {
        long r = 0;
        long g = 0;
        long b = 0;
        long a = 0;

        bool hasAlpha = this.HasAlpha;
        nuint adder = (nuint)(hasAlpha ? 4 : 3);

        for (nuint i = 0; i < _length; i += adder) {
            b += _ptr[i + 0];
            g += _ptr[i + 1];
            r += _ptr[i + 2];
            if(hasAlpha)
                a += _ptr[i + 3];
        }

        byte avgR = (byte)(r / Length);
        byte avgG = (byte)(g / Length);
        byte avgB = (byte)(b / Length);
        byte avgA = (byte)(HasAlpha ? (a / Length) : 255);

        return Color.FromArgb(avgA, avgR, avgG, avgB);
    }

    protected virtual void Dispose(bool disposing) {
        if (!_disposedValue) {
            if (disposing) {
                // TODO: dispose managed state (managed objects)
            }

            NativeMemory.Free(_ptr);
            _disposedValue = true;
        }
    }

    // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~ColorArray() {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose() {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    
    public static ColorArray CopyFromBitmap(Bitmap bm) {
        var bmData = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadOnly, bm.PixelFormat);
        nuint length = (nuint)(bmData.Stride * bmData.Height);
        byte* ptr = (byte*)NativeMemory.Alloc(length);
        Buffer.MemoryCopy((byte*)bmData.Scan0, ptr, length, length);
        bm.UnlockBits(bmData);
        return new ColorArray(ptr, length, bm.PixelFormat);
    }
}
