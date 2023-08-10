using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USM.Devices.Emulator {
    public class NewAnimationEventArgs : EventArgs {
        public byte[] ByteCode { get; }
        public NewAnimationEventArgs(byte[] byteCode) : base() {
            this.ByteCode = byteCode;
        }
    }
}
