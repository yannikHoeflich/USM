using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USM.Lal.Runtime.RefWrappers {
    internal class RefShortWrapper : RefWrapper<short> {
        public static implicit operator RefShortWrapper(short value) => new RefShortWrapper(value);
        public static explicit operator short(RefShortWrapper value) => value.Value;

        public RefShortWrapper(short value) : base(value) {
        }
    }
}
