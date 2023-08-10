using System.Collections.Generic;
using System.Text;

using System;

namespace USM.Devices.Packaging.PackageParts {
    internal interface IPackagePart {
        public int Length { get; }
        public IEnumerable<byte> BuildBytes();
    }
}
