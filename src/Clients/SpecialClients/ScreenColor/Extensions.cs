using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ScreenColor;
internal static class Extensions {
    public static async Task<T[]> ToArrayAsync<T>(this IAsyncEnumerable<T> source) {
        LinkedList<T> list = new LinkedList<T>();
        await foreach (var item in source) {
            list.AddLast(item);
        }
        return list.ToArray();
    }
    public static async Task<LinkedList<T>> ToLinkedListAsync<T>(this IAsyncEnumerable<T> source) {
        LinkedList<T> list = new LinkedList<T>();
        await foreach (var item in source) {
            list.AddLast(item);
        }
        return list;
    }

    public static IEnumerable<T> MakeSync<T>(this IAsyncEnumerable<T> source) {
        var enumrator = source.GetAsyncEnumerator();
        while (enumrator.MoveNextAsync().GetAwaiter().GetResult()) {
            yield return enumrator.Current;
        }
    }
    public static async Task<IEnumerable<T>> AwaitBuffer<T>(this IAsyncEnumerable<T> source) {
        LinkedList<T> list = new LinkedList<T>();
        await foreach (var item in source) {
            list.AddLast(item);
        }
        return list;
    }
}
