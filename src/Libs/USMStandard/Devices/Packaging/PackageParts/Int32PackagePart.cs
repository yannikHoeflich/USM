using System.Collections.Generic;
using System.Text;

using System;

namespace USM.Devices.Packaging.PackageParts {
    internal class Int32PackagePart : IPackagePart {
        private readonly int _data;

        public int Length { get; } = 4;
        public Int32PackagePart(int data) {
            _data = data;
        }
        public IEnumerable<byte> BuildBytes() {
            return BitConverter.GetBytes(_data);
        }
    }
}
