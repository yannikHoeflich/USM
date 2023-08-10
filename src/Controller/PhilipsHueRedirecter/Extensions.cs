using Q42.HueApi.Interfaces;
using Q42.HueApi.Streaming;
using Q42.HueApi.Streaming.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhilipsHueRedirecter;
internal static class Extensions {
    public static bool TryInitialize(this ILocalHueClient client, string appKey) {
        try {
            client.Initialize(appKey);
            return true;
        } catch {
            return false;
        }
    }

    public static async Task AutoKeepAlive(this StreamingHueClient client, StreamingGroup group) {
        while (true) {
            client.ManualKeepAlive(group);
            await Task.Delay(100);
        }
    }
    public static void ManualKeepAlive(this StreamingHueClient client, StreamingGroup group) {
        var dirts = group.Select(x => x.State.IsDirty).ToArray();
        foreach (var l in group) {
            l.State.IsDirty = false;
        }
        client.ManualUpdate(group, true);
        for (int i = 0; i < dirts.Length; i++) {
            group[i].State.IsDirty = dirts[i];
        }
    }
}
