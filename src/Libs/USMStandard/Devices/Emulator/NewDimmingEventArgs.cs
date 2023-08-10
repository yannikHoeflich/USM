using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USM.Devices.Emulator {
    public class NewDimmingEventArgs : EventArgs {
        public double DimmingValue { get; }

        public NewDimmingEventArgs(double dimmingValue) : base() {
            this.DimmingValue = dimmingValue;
        }
    }
}
