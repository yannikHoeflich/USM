using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USM.Lal.Compiler {
    internal class Branch {
        public readonly short[] Data;
        public readonly BranchType Type;
        public Branch(short[] data, BranchType type) {
            Data = data;
            Type = type;
        }
    }

    enum BranchType {
        If,
        While,
        Function
    }
}
