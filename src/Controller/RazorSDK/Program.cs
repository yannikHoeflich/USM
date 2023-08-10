using Newtonsoft.Json;

using RazorSDK;

using System.Security.Cryptography;
using System.Text;

using USM.Devices.Emulator;
using USM.Lal.Runtime;

var razer = new DeviceController();
await razer.InitAsync();

DeviceClient deviceClient = new DeviceClient(BitConverter.ToInt32(SHA256.Create().ComputeHash(Encoding.Unicode.GetBytes(Environment.MachineName))), razer.LedCount);
AsyncLalRuntime? runtime = null;

deviceClient.NewAnimationReceived += DeviceClient_NewAnimationReceived;
deviceClient.NewDimmingReceived += DeviceClient_NewDimmingReceived;

await DeviceClient_NewDimmingReceived(new NewDimmingEventArgs(1));

Task.Run(async () => await deviceClient.StartListening(CancellationToken.None));

Task DeviceClient_NewDimmingReceived(NewDimmingEventArgs arg) {
    return Task.CompletedTask;
}

async Task DeviceClient_NewAnimationReceived(NewAnimationEventArgs arg) {
    await razer.RunAnimation(arg.ByteCode);
}

Console.WriteLine("running . . .");
while (true) {
    await Task.Delay(TimeSpan.FromHours(1));
}