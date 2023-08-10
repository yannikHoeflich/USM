using System.Collections.Generic;
using System.Text;

using System;

namespace USM.Devices.Packaging.PackageParts {
    internal class Int16PackagePart : IPackagePart {
        private readonly short _data;

        public int Length { get; } = 2;
        public Int16PackagePart(short data) {
            _data = data;
        }
        public IEnumerable<byte> BuildBytes() {
            return BitConverter.GetBytes(_data);
        }
    }
}
