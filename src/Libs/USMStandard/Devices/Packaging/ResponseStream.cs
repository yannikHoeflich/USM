using System.Collections.Generic;
using System.Linq;
using System.Text;

using System;

namespace USM.Devices.Packaging {
    internal class ResponseStream {
        private readonly byte[] _buffer;
        private int _index;

        public ResponseStream(byte[] buffer) {
            _buffer = buffer;
            _index = 0;
        }

        public byte GetInt8() => _buffer[_index++];
        public short GetInt16() {
            short value = BitConverter.ToInt16(_buffer, _index);
            _index += 2;
            return value;
        }

        public int GetInt32() {
            int value = BitConverter.ToInt32(_buffer, _index);
            _index += 4;
            return value;
        }

        public long GetInt64() {
            long value = BitConverter.ToInt64(_buffer, _index);
            _index += 8;
            return value;
        }

        public ReadOnlySpan<byte> GetBuffer(int length) => _buffer.AsSpan(_index, length);

        public void DropDefaultValues(bool isVersionTwoOrHigher) {
            _index += isVersionTwoOrHigher ? 6 : 2;
        }
    }
}
