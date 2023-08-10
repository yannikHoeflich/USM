using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace USM.Lal.Runtime.RefWrappers {
    internal class RefWrapper<T> {
        public static implicit operator RefWrapper<T>(T value) => new RefWrapper<T>(value);
        public static explicit operator T(RefWrapper<T> value) => value.Value;

        public RefWrapper(T value) {
            Value = value;
        }

        public T Value { get; set; }

        public override string ToString() {
            return Value.ToString();
        }
    }
}
