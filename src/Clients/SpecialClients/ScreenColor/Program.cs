
using ScreenColor;

using System;
using System.Drawing;

using USM;
using USM.Devices;

const double fps = 20;

CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

var deviceManager = new DeviceManager();
await deviceManager.InitAsync();

ScreenRecorder recorder = new ScreenRecorder();
new Thread(async () => await recorder.StartRecording(new FrameTimer(10), cancellationTokenSource.Token)).Start();

AppDomain.CurrentDomain.UnhandledException += ExitEvent;
Console.CancelKeyPress += ExitEvent;

void ExitEvent(object sender, EventArgs e) {
    if (!cancellationTokenSource.IsCancellationRequested) {
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Token.WaitHandle.WaitOne();
    }
    deviceManager.SendColorAsync(Color.Black).Wait();
    Environment.Exit(0);
}

var frame = new FrameTimer(fps);
while (!cancellationTokenSource.Token.IsCancellationRequested) {
    frame.StartFrame();
    var colorArray = recorder.GetCurrentColorArray();
    var color = colorArray.GetAvarageColor();
    await deviceManager.SendColorAsync(color);
    colorArray.Dispose();
    await frame.WaitFrame(cancellationTokenSource.Token);
}