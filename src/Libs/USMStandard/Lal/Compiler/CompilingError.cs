using System;

namespace USM.Lal.Compiler {
    public class CompilingError : Exception {
        public string CompilerMessage { get; }
        public CompilingError(string message) {
            this.CompilerMessage = message;
        }
    }
}
