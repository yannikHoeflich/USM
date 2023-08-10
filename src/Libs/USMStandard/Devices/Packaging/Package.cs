using System.Collections.Generic;
using System.Linq;
using System.Text;

using System;
using USM.Devices.Packaging.PackageParts;

namespace USM.Devices.Packaging {
    internal class Package {
        private readonly LinkedList<IPackagePart> _packageParts = new LinkedList<IPackagePart>();

        public Package Add(byte value) => Add(new Int8PackagePart(value));
        public Package Add(short value) => Add(new Int16PackagePart(value));
        public Package Add(int value) => Add(new Int32PackagePart(value));
        public Package Add(ICollection<byte> value) => Add(new BufferPackagePart(value));

        public Package(byte packageType) {
            Add(packageType);
        }

        public Package(byte packageType, IDevice device) {
            Add(packageType);

            if (device.ProtocolVersion > 1)
                Add(device.Id);
        }


        public Package Add(IPackagePart part) {
            if (part == null) {
                throw new ArgumentNullException(nameof(part), "Package part is null");
            }

            _packageParts.AddLast(part);

            return this;
        }

        public byte[] Build() {
            int length = _packageParts.Sum(x => x.Length);
            byte[] buffer = new byte[length];
            int i = 0;
            foreach (IPackagePart part in _packageParts) {
                foreach(byte b in part.BuildBytes()) {
                    buffer[i++] = b;
                }
            }

            return buffer;
        }
    }

    public enum PackageType {
        Response = 0,
        Scan = 1,
        GeneralInitialization = 2,
        SpecificInitialization = 3,
        AnimationLight = 16,
        DimLight = 17,

    }
}
