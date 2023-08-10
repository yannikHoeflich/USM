using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json;
using PhilipsHueRedirecter;
using Q42.HueApi;
using Q42.HueApi.ColorConverters;
using Q42.HueApi.ColorConverters.Gamut;
using Q42.HueApi.ColorConverters.HSB;
using Q42.HueApi.Interfaces;
using Q42.HueApi.Models.Groups;
using Q42.HueApi.Streaming;
using Q42.HueApi.Streaming.Extensions;
using Q42.HueApi.Streaming.Models;
using USM.Devices.Emulator;
using USM.Lal.Runtime;

string appKeyFile = "hueKey.json";



IBridgeLocator locator = new HttpBridgeLocator(); 
var bridges = await locator.LocateBridgesAsync(TimeSpan.FromSeconds(5));

if (!bridges.Any()) {
    Console.WriteLine("No Bridges found!");
    return;
}

var localClient = new LocalHueClient(bridges.First().IpAddress);

var keys = new KeyData();
if (!File.Exists(appKeyFile) || !localClient.TryInitialize((keys = JsonConvert.DeserializeObject<KeyData>( File.ReadAllText(appKeyFile))).AppKey)) {
    Console.WriteLine("registration nessesary");
    Console.WriteLine("Please press the button");
    Console.WriteLine("Please enter to continue");
    Console.ReadLine();
    var result = await localClient.RegisterAsync("UsmRedirecter", Environment.MachineName, true);
    keys.AppKey = result.Username;
    keys.StreamingKey = result.StreamingClientKey;
    File.WriteAllText(appKeyFile, JsonConvert.SerializeObject(keys));
    localClient.Initialize(appKeyFile);
}

using var client = new StreamingHueClient(bridges.FirstOrDefault().IpAddress, keys.AppKey, keys.StreamingKey);
IReadOnlyList<Group> groups = null;
for(int i = 0; i < 10; i++) {
    try {
        groups = await client.LocalHueClient.GetEntertainmentGroups().WaitAsync(TimeSpan.FromMilliseconds(500));
        break;
    } catch {
        if(i == 9) {
            Console.WriteLine("Error getting Groups: Connection failed multiple times");
            return;
        }
    }
}
Console.WriteLine("Entertainment Groups:"); {
    int i = 0;
    foreach (var g in groups) {
        Console.WriteLine($"{i}: {g.Name}");
        i++;
    }
}

Console.Write("\n Please select one:");
int groupIndex = int.Parse( Console.ReadLine());
var group = groups[groupIndex];
var entGroup = new StreamingGroup(group.Locations);
await client.Connect(group.Id);
Task.Run(async () => await client.AutoUpdate(entGroup, CancellationToken.None, 50));
//var entLayer = entGroup.GetNewLayer(isBaseLayer: true);

//The EntertainmentLayer contains all the lights, you can make subselections
var lights = entGroup.OrderBy(x => x.LightLocation.X).ToArray();

DeviceClient deviceClient = new DeviceClient(BitConverter.ToInt32(SHA256.Create().ComputeHash(Encoding.Unicode.GetBytes( Environment.MachineName))), lights.Length);
AsyncLalRuntime? runtime = null;

deviceClient.NewAnimationReceived += DeviceClient_NewAnimationReceived; 
deviceClient.NewDimmingReceived += DeviceClient_NewDimmingReceived;

await DeviceClient_NewDimmingReceived(new NewDimmingEventArgs(1));

Task.Run(async () => await deviceClient.StartListening(CancellationToken.None));

Task DeviceClient_NewDimmingReceived(NewDimmingEventArgs arg) {
    foreach(var l in lights) {
        l.State.SetBrightness(arg.DimmingValue);
    }
    client.ManualUpdate(entGroup);
    return Task.CompletedTask;
}

bool stop = false;
async Task DeviceClient_NewAnimationReceived(NewAnimationEventArgs arg) {
    stop = true;
    while (runtime != null) {
        await Task.Delay(10);
    }
    stop = false;
    var rt = new AsyncLalRuntime(arg.ByteCode);
    rt.LedCount = (short)deviceClient.LedCount;
    rt.SetLed = (index, r, g, b) => {
        lights[index].State.SetRGBColor(new RGBColor(r, g, b));
        return Task.CompletedTask;
    };
    rt.UpdateLeds = () => Task.Run(() => client.ManualUpdate(entGroup));
    rt.ShouldStop = () => {
        if (stop) {
            return true;
        }
        return false;
    };
    runtime = rt;
    Task.Run(async () => {
        await rt.Run();
        runtime = null;
    });
}
Console.ReadLine();