using System.Collections.Generic;
using System.Text;

using System;

namespace USM.Devices.Packaging.PackageParts {
    internal class Int8PackagePart : IPackagePart {
        private readonly byte _byte;

        public int Length { get; } = 1;

        public IEnumerable<byte> BuildBytes() {
            yield return _byte;
        }

        public Int8PackagePart(byte data) {
            _byte = data;
        }
    }
}
