using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenColor;
internal class FrameTimer {
    private DateTime _frameStart;

    public TimeSpan FrameTime { get; set; }
    public double Fps => 1.0 / FrameTime.TotalSeconds;

    public FrameTimer(double fps) : this(TimeSpan.FromSeconds(1.0 / fps)) { }
    public FrameTimer(TimeSpan frameTime) {
        FrameTime = frameTime;
    }

    public void StartFrame() {
        _frameStart = DateTime.Now;
    }

    public async Task WaitFrame(CancellationToken token) {
        var waitTime = FrameTime - (DateTime.Now - _frameStart);
        if(waitTime > TimeSpan.Zero)
            await Task.Delay(waitTime, token);
    }
}
