using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using System;
using USM.Debug;
using USM.Devices;

namespace USM.Lal.Compiler {
    public class Compiler {

        private Logger logger;

        private List<string> variables = new List<string>();
        private Dictionary<string, short> functions = new Dictionary<string, short>();

        private readonly Regex variableSetRegex = new Regex(@"[a-zA-Z_][a-zA-Z0-9_]* *=.+", RegexOptions.Compiled);
        private readonly Regex functionRegex = new Regex(@"func [a-zA-Z_][a-zA-Z0-9_]*", RegexOptions.Compiled);
        private readonly Regex ifRegex = new Regex(@"if .+", RegexOptions.Compiled);
        private readonly Regex whileRegex = new Regex(@"while .+");
        private readonly Regex functionCallRegex = new Regex(@"[a-zA-Z_][a-zA-Z0-9_]*\(.*\)", RegexOptions.Compiled);
        LinkedList<byte> bytes = new LinkedList<byte>();
        LinkedList<Branch> branches = new LinkedList<Branch>();

        public bool Compile(DeviceType deviceType, string raw, out byte[] byteCode, Logger logger) {
            this.logger = logger ?? new Logger();

            variables.Add("lal_tmp");
            if (deviceType == DeviceType.Light) {
                variables.Add("LED_COUNT");
            } 
            if(deviceType == DeviceType.Matrix) {
                variables.Add("WIDTH");
                variables.Add("HEIGHT");
            }

            functions.Add("Set", -1);
            functions.Add("UpdateLeds", -2);
            functions.Add("Random", -3);
            functions.Add("CreateArray", -4);
            functions.Add("SetArray", -5);
            functions.Add("GetArray", -6);
            functions.Add("SleepFrame", -7);
            functions.Add("Sleep", -8);
            functions.Add("GetTime", -9);
            functions.Add("Break", -10);
            functions.Add("Log", -11);

            byteCode = Array.Empty<byte>();

            this.logger.Log(this, "parsing raw file");

            string[] lines = raw.ReplaceLineEndings("")
                               .Split(';')
                               .SelectMany(x => x.Split('{'))
                               .Select(x => x.Trim())
                               .ToArray();

            this.logger.Log(this, "compiling");
            double lastPercentOutput = 0;

            int lineNumber = 0;
            foreach (string line in lines) {
                lineNumber++;
                try {
                    if (line.ToLower().StartsWith("return")) {
                        bytes.AddRange(GetReturnBytes(line));
                    } else if (variableSetRegex.Match(line).Length == line.Length) {
                        bytes.AddRange(GetSetBytes(line));
                    } else if (functionRegex.Match(line).Length == line.Length) {
                        bytes.AddRange(GetFuncBytes(line));
                    } else if (ifRegex.Match(line).Length == line.Length) {
                        bytes.AddRange(GetIfBytes(line));
                    } else if (whileRegex.Match(line).Length == line.Length) {
                        bytes.AddRange(GetWhileBytes(line));
                    } else if (functionCallRegex.Match(line).Length == line.Length) {
                        bytes.AddRange(GetFunctionCallBytes(line));
                    } else if (line == "}") {
                        bytes.AddRange(GetBranchEnd());
                    } else if (!String.IsNullOrWhiteSpace(line)) {
                        throw new CompilingError("Invalid statement");
                    }
                } catch (CompilingError error) {
                    this.logger.Log(this, $"{error.CompilerMessage} at statement {lineNumber}: {line}");
                    return false;
                }

                double percent = (double)lineNumber / lines.Length * 100;
                if (percent >= lastPercentOutput + 10) {
                    this.logger.Log(this, $"progress: {Math.Round(percent * 100) / 100}%");
                    lastPercentOutput = percent;
                }
            }

            byteCode = bytes.ToArray();

            variables = new List<string>();
            functions = new Dictionary<string, short>();
            bytes = new LinkedList<byte>();
            branches = new LinkedList<Branch>();

            this.logger.Log(this, $"progress: 100%");

            return true;
        }

        private IEnumerable<byte> GetBranchEnd() {
            var branch = branches.Last;
            if (branch == null) {
                throw new CompilingError("Unexpected character '}', there is no matching statement to end this block.");
            }
            branches.RemoveLast();

            var value = branch.Value;
            return value.Type switch {
                BranchType.If => GetEndIfBytes(value),
                BranchType.While => GetEndWhileBytes(value),
                BranchType.Function => GetEndFuncBytes(value),
                _ => Array.Empty<byte>(),
            };
        }

        private IEnumerable<byte> GetFunctionCallBytes(string line) {
            yield return 3;
            foreach (byte b in GetValueBytes(line)) {
                yield return b;
            }
        }

        private IEnumerable<byte> GetEndWhileBytes(Branch branch) {
            var toJump = branch.Data[0];
            var toReplace = branch.Data[1];


            yield return 2;

            foreach (byte b in BitConverter.GetBytes((short)-1)) {
                yield return b;
            }

            foreach (byte b in BitConverter.GetBytes(toJump)) {
                yield return b;
            }


            short index = toReplace;
            byte[] value = BitConverter.GetBytes((short)bytes.Count);
            bytes.ReplaceRange(index, value);
        }

        private IEnumerable<byte> GetWhileBytes(string line) {
            string value = line["while".Length..].Trim();

            if (string.IsNullOrWhiteSpace(value))
                throw new CompilingError("an while statement needs a value or calculation");

            var stackData = new short[] { (short)bytes.Count };
            foreach (byte b in GetSetBytes($"lal_tmp = {value}")) {
                yield return b;
            }


            yield return 2;

            foreach (byte b in BitConverter.GetBytes((short)variables.IndexOf("lal_tmp"))) {
                yield return b;
            }

            stackData = new short[] { stackData[0], (short)bytes.Count };
            yield return 0;
            yield return 0;
            branches.AddLast(new Branch(stackData, BranchType.While));
        }

        private IEnumerable<byte> GetEndIfBytes(Branch branch) {

            byte[] value = BitConverter.GetBytes((short)bytes.Count);

            short index = branch.Data[0];
            bytes.ReplaceRange(index, value);

            return Enumerable.Empty<byte>();
        }

        private IEnumerable<byte> GetIfBytes(string line) {
            string value = line["if".Length..].Trim();

            if (string.IsNullOrWhiteSpace(value))
                throw new CompilingError("an if statement needs a value or calculation");

            foreach (byte b in GetSetBytes($"lal_tmp = {value}")) {
                yield return b;
            }


            yield return 2;

            foreach (byte b in BitConverter.GetBytes((short)variables.IndexOf("lal_tmp"))) {
                yield return b;
            }

            branches.AddLast(new Branch(new short[] { (short)bytes.Count }, BranchType.If));
            yield return 0;
            yield return 0;
        }

        private IEnumerable<byte> GetEndFuncBytes(Branch branch) {
            yield return 0;
            foreach (byte b in GetCalculationBytes("0")) {
                yield return b;
            }

            byte[] currentIndex = BitConverter.GetBytes((short)bytes.Count);

            short index = branch.Data[0];
            bytes.ReplaceRange(index, currentIndex);
        }

        private IEnumerable<byte> GetFuncBytes(string line) {
            string[] parts = line.Split(' ');
            if (parts.Length != 2) {
                throw new CompilingError("wrong function definition: func [function name];");
            }

            string name = parts[1];

            if (functions.ContainsKey(name)) {
                throw new CompilingError($"the function name {name} does already exists");
            }

            yield return 2;

            foreach (byte b in BitConverter.GetBytes((short)-1)) {
                yield return b;
            }

            branches.AddLast(new Branch(new short[] { (short)bytes.Count }, BranchType.Function));
            yield return 0;
            yield return 0;

            functions.Add(name, (short)bytes.Count);
        }

        private IEnumerable<byte> GetReturnBytes(string line) {
            yield return 0;

            string value = line["return".Length..];

            foreach (byte b in GetCalculationBytes(value)) {
                yield return b;
            }
        }

        char[] calculationSymbols = new char[] { '+', '-', '*', '/', '%', '&', '|', '^', '=', '!', '<', '>' };
        private IEnumerable<byte> GetSetBytes(string line) {
            if (string.IsNullOrWhiteSpace(line))
                yield break;

            string[] parts = line.Split('=');
            string name = parts[0].Trim();
            string value = string.Join('=', parts[1..]);

            if (name.StartsWith('_')) {
                if (short.TryParse(name[1..], out short localIndex)) {
                    yield return 4;
                    foreach (byte b in BitConverter.GetBytes(localIndex)) {
                        yield return b;
                    }
                } else {
                    throw new CompilingError("the local variable contains other symbols that letters, please remove them");
                }
            } else {
                yield return 1;
                short variableIndex = 0;
                if ((variableIndex = (short)variables.FindIndex(x => x == name)) == -1) {
                    variableIndex = (short)variables.Count;
                    variables.Add(name);
                }

                foreach (byte b in BitConverter.GetBytes(variableIndex)) {
                    yield return b;
                }
            }
            foreach (byte b in GetCalculationBytes(value)) {
                yield return b;
            }
        }

        private IEnumerable<byte> GetCalculationBytes(string fullCalculation) {
            string calculation = "";
            int index = 0;
            byte calculationAmount = 0;
            LinkedList<byte> calcBytes = new LinkedList<byte>();
            while (index < fullCalculation.Length) {
                calculationAmount++;
                while (index < fullCalculation.Length && !calculationSymbols.Contains(fullCalculation[index])) {
                    calculation += fullCalculation[index];
                    index++;
                }
                index--;
                calcBytes.AddRange(GetInnerCalculationBytes(calculation));
                index++;
                if (index >= fullCalculation.Length)
                    break;
                calculation = new string(new char[] { fullCalculation[index] });
                index++;
            }
            yield return calculationAmount;
            foreach (byte b in calcBytes) {
                yield return b;
            }
        }
        private IEnumerable<byte> GetInnerCalculationBytes(string calculation) {

            string str = calculation.Trim();
            if (string.IsNullOrWhiteSpace(str))
                yield break;

            int calculationType = calculationSymbols.IndexOf(str[0]);
            yield return calculationType == -1 ? (byte)0 : (byte)calculationType;


            if (calculationType > -1)
                str = str[1..];

            foreach (byte b in GetValueBytes(str)) {
                yield return b;
            }
        }

        private Regex valueVariableRegex = new Regex(@"[a-zA-Z][a-zA-Z0-9_]*");
        private Regex valueFunctionRegex = new Regex(@"[a-zA-Z][a-zA-Z0-9_]*\(.*\)");
        private Regex valueLocalVariableRegex = new Regex(@"_[0-9]+");
        private IEnumerable<byte> GetValueBytes(string valueStr) {
            string str = valueStr.Trim();

            if (short.TryParse(str, out short value)) {
                yield return 0;
                foreach (byte b in BitConverter.GetBytes(value)) {
                    yield return b;
                }
                yield break;
            }

            if (valueVariableRegex.Match(str).Length == str.Length) {
                short index;
                if ((index = (short)variables.FindIndex(x => x == str)) != -1) {
                    yield return 1;
                    foreach (byte b in BitConverter.GetBytes(index)) {
                        yield return b;
                    }
                    yield break;
                }
                throw new CompilingError($"can't find the variable with the name \"{str}\", make sure that the variable is defined before this statement");
            }

            if (valueFunctionRegex.Match(str).Length == str.Length) {
                string[] parts = str.Split('(');
                string name = parts[0];
                string[] values = parts[1][..^1].Split(',').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

                if (functions.TryGetValue(name, out short index)) {
                    yield return 2;
                    foreach (byte b in BitConverter.GetBytes(index)) {
                        yield return b;
                    }

                    foreach (byte b in BitConverter.GetBytes((short)values.Length)) {
                        yield return b;
                    }

                    foreach (var param in values) {
                        foreach (byte b in GetValueBytes(param)) {
                            yield return b;
                        }
                    }


                    yield break;
                }

                throw new CompilingError($"can't find the function with the name \"{str}\", make sure that the variable is defined before this statement");
            }


            if (valueLocalVariableRegex.Match(str).Length == str.Length) {
                yield return 3;
                foreach (byte b in BitConverter.GetBytes(short.Parse(str[1..]))) {
                    yield return b;
                }
                yield break;
            }

            throw new CompilingError("couldn't parse the value");
        }
    }
}
