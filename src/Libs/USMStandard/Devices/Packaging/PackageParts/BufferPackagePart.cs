using System.Collections.Generic;
using System.Text;

using System;

namespace USM.Devices.Packaging.PackageParts {
    internal class BufferPackagePart : IPackagePart {
        private readonly ICollection<byte> _buffer;
        public int Length => _buffer.Count;

        public BufferPackagePart(ICollection<byte> buffer) {
            _buffer = buffer;
        }


        public IEnumerable<byte> BuildBytes() => _buffer;
    }
}
