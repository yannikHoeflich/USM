using System;

namespace USM.Debug {

    public class Logger {
        public event EventHandler<string> NewDataAvailable;

        public void Log(object sender, string message) {
            NewDataAvailable?.Invoke(sender, message);
        }
    }
}