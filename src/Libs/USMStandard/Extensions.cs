using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using System;
using USM.Devices.Packaging;

namespace USM {
    internal static class Extensions {
        public static void AddRange<T>(this LinkedList<T> @this, IEnumerable<T> collection) {
            foreach (var e in collection) {
                @this.AddLast(e);
            }
        }


        public static int IndexOf<T>(this IEnumerable<T> @this, T element) {
            int index = 0;
            foreach (var e in @this) {
                if (e.Equals(element)) {
                    return index;
                }

                index++;
            }
            return -1;
        }

        public static void ReplaceRange<T>(this LinkedList<T> @this, int index, IEnumerable<T> elements) {
            var currentEllement = @this.First;
            for (int i = 0; i < index; i++) {
                if (currentEllement == null)
                    throw new IndexOutOfRangeException(nameof(index));
                currentEllement = currentEllement.Next;
            }

            var enumerator = elements.GetEnumerator();
            while (enumerator.MoveNext()) {
                currentEllement.Value = enumerator.Current;

                currentEllement = currentEllement.Next;
                if (currentEllement == null)
                    throw new IndexOutOfRangeException(nameof(index));
            }
        }

        public static string ReplaceLineEndings(this string @this, string replacementText) {
            if (replacementText is null) {
                throw new ArgumentNullException(nameof(replacementText));
            }

            // Early-exit: do we need to do anything at all?
            // If not, return this string as-is.

            int idxOfFirstNewlineChar = IndexOfNewlineChar(@this, out int stride);
            if (idxOfFirstNewlineChar < 0) {
                return @this;
            }

            // While writing to the builder, we don't bother memcpying the first
            // or the last segment into the builder. We'll use the builder only
            // for the intermediate segments, then we'll sandwich everything together
            // with one final string.Concat call.

            ReadOnlySpan<char> firstSegment = @this.AsSpan(0, idxOfFirstNewlineChar);
            ReadOnlySpan<char> remaining = @this.AsSpan(idxOfFirstNewlineChar + stride);

            StringBuilder builder = new StringBuilder();
            while (true) {
                int idx = IndexOfNewlineChar(remaining, out stride);
                if (idx < 0) { break; } // no more newline chars
                builder.Append(replacementText);
                builder.Append(remaining.Slice(0, idx));
                remaining = remaining.Slice(idx + stride);
            }

            string retVal = string.Concat(new string(firstSegment), new string(builder.ToString().AsSpan()), new string(replacementText), new string(remaining));
            return retVal;
        }
        private static int IndexOfNewlineChar(ReadOnlySpan<char> text, out int stride) {
            // !! IMPORTANT !!
            //
            // We expect this method may be called with untrusted input, which means we need to
            // bound the worst-case runtime of this method. We rely on MemoryExtensions.IndexOfAny
            // having worst-case runtime O(i), where i is the index of the first needle match within
            // the haystack; or O(n) if no needle is found. This ensures that in the common case
            // of this method being called within a loop, the worst-case runtime is O(n) rather than
            // O(n^2), where n is the length of the input text.
            //
            // The Unicode Standard, Sec. 5.8, Recommendation R4 and Table 5-2 state that the CR, LF,
            // CRLF, NEL, LS, FF, and PS sequences are considered newline functions. That section
            // also specifically excludes VT from the list of newline functions, so we do not include
            // it in the needle list.

            const string needles = "\r\n\f\u0085\u2028\u2029";

            stride = default;
            int idx = text.IndexOfAny(needles);
            if ((uint)idx < (uint)text.Length) {
                stride = 1; // needle found

                // Did we match CR? If so, and if it's followed by LF, then we need
                // to consume both chars as a single newline function match.

                if (text[idx] == '\r') {
                    int nextCharIdx = idx + 1;
                    if ((uint)nextCharIdx < (uint)text.Length && text[nextCharIdx] == '\n') {
                        stride = 2;
                    }
                }
            }

            return idx;
        }

        public static IPEndPoint Clone(this IPEndPoint endPoint) {
            return new IPEndPoint(new IPAddress(endPoint.Address.GetAddressBytes()), endPoint.Port);
        }

        public static async Task<byte[]> ReceiveFromAsync(this UdpClient client, IPEndPoint remoteEndpoint, TimeSpan? timeout = null) {
            byte[] buffer = new byte[1024];
            ArraySegment<byte> bufferWrapper = new ArraySegment<byte>(buffer);

            timeout ??= TimeSpan.FromDays(49);

            var t = await Task.WhenAny(client.Client.ReceiveFromAsync(bufferWrapper, SocketFlags.None, remoteEndpoint), Task.Delay((TimeSpan)timeout));
            return t is Task<SocketReceiveFromResult> result 
                ? buffer[..result.Result.ReceivedBytes] 
                : throw new TimeoutException();
        }

        public static bool StartWith<T>(this IEnumerable<T> @this, IEnumerable<T> second) {
            if (@this == null)
                throw new NullReferenceException(nameof(@this));

            if (second == null)
                throw new ArgumentNullException(nameof(second));

            var thisEn = @this.GetEnumerator();
            var secondEn = second.GetEnumerator();

            while (secondEn.MoveNext()) {
                if (!thisEn.MoveNext())
                    return false;

                if (thisEn.Current == null ^ secondEn.Current == null) {
                    return false;
                }

                if(!thisEn.Current.Equals(secondEn.Current))
                    return false;
            }
            return true;
        }

        public static IEnumerable<T> Add<T>(this IEnumerable<T> @this, T toAdd) {
            foreach(var elem in @this) {
                yield return elem;
            }
            yield return toAdd;
        }

        public static void EnqueueAll<T>(this Queue<T> @this, IEnumerable<T> items) {
            foreach (var item in items) {
                @this.Enqueue(item);
            }
        }

        internal static ResponseStream GetResponseStream(this UdpReceiveResult receiveResult) => new ResponseStream(receiveResult.Buffer);
    }
}