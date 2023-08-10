using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace USM.Lal.Runtime {
    public class LalRuntime {
        private static readonly Random random = new Random();

        public static readonly TimeSpan frameTime = TimeSpan.FromMilliseconds(30);

        private DateTime lastFrame = DateTime.MinValue;
        private readonly byte[] bytes;

        private short currentArrayLength = 0;
        private readonly short[] arrayBuffer = new short[short.MaxValue];
        private DateTime lastScriptframe = DateTime.MinValue;

        public Action<short, short, short, short> SetLed { get; set; }
        public Action UpdateLeds { get; set; }
        public Func<bool> ShouldStop { get; set; }
        public short LedCount { get; set; }
        public Action DebugBreak { get; set; }
        public Action<short> Log { get; set; }

        public LalRuntime(byte[] bytes) {
            this.bytes = bytes;
        }

        private short CreateArray(short length) {
            short pointer = currentArrayLength;
            currentArrayLength += length;
            return pointer;
        }


        private void WaitFrame(short time) {
            TimeSpan sleepTime = TimeSpan.FromMilliseconds(time) - (DateTime.Now - lastScriptframe);
            if (sleepTime.TotalMilliseconds > 0) {
                Thread.Sleep(sleepTime);
            }

            lastScriptframe = DateTime.Now;
        }

        public void Run() {
            List<short> variables = new List<short> {
                0,
                LedCount
            };

            short pos = 0;
            Run(pos, Array.Empty<short>(), variables);
        }


        private short Run(short pos, short[] locals, List<short> variables) {
            if (pos < 0) {
                switch (pos) {
                    case -1:
                        SetLed?.Invoke(locals[0], locals[1], locals[2], locals[3]);
                        return 0;
                    case -2: {
                            UpdateLeds?.Invoke();

                            DateTime now = DateTime.Now;
                            TimeSpan sleeptime = frameTime - (now - lastFrame);
                            if (sleeptime.TotalMilliseconds > 0) {
                                Thread.Sleep(sleeptime);
                            }

                            lastFrame = now;
                            return 0;
                        }
                    case -3:

                        return (short)random.Next();
                    case -4:
                        return CreateArray(locals[0]);
                    case -5:
                        arrayBuffer[locals[0] + locals[1]] = locals[2];
                        return 0;
                    case -6:
                        return arrayBuffer[locals[0] + locals[1]];
                    case -7:
                            WaitFrame(locals[0]);
                        return 0;
                    case -8:
                        Thread.Sleep(locals[0]);
                        return 0;
                    case -9: {
                            DateTime now = DateTime.Now;
                            return (short)((now.Hour * 60) + now.Minute);
                        }
                    case -10: {
                            DebugBreak?.Invoke();
                            return 0;
                        }
                    case -11: {
                            Log?.Invoke(locals[0]);
                            return 0;
                        }
                    default:
                        return 0;
                }
            }

            while (pos < bytes.Length && !ShouldStop.Invoke()) {
                switch (bytes[pos]) {
                    case 0: {
                            pos++;
                            short value = Calculate(ref pos, locals, variables);
                            return value;
                        }
                    case 1: {
                            pos++;
                            SetVariable(ref pos, locals, variables);
                            break;
                        }
                    case 2: {
                            pos++;
                            Jump(ref pos, locals, variables);
                            break;
                        }
                    case 3: {
                            pos++;
                            GetValue(ref pos, locals, variables);
                            break;
                        }
                    case 4: {
                            pos++;
                            SetLocal(ref pos, locals, variables);
                            break;

                        }

                    default:
                        pos++;
                        break;
                }
            }
            return 0;
        }

        private void SetLocal(ref short pos, short[] locals, List<short> variables) {
            short localIndex = BitConverter.ToInt16(bytes.Skip(pos).Take(2).ToArray());
            pos += 2;

            locals[localIndex] = Calculate(ref pos, locals, variables);
        }

        private void Jump(ref short pos, short[] locals, List<short> variables) {
            short variableIndex = BitConverter.ToInt16(bytes.Skip(pos).Take(2).ToArray());
            pos += 2;
            short newPos = BitConverter.ToInt16(bytes.Skip(pos).Take(2).ToArray());
            pos += 2;
            if (variableIndex == -1 || variables[variableIndex] == 0) {
                pos = newPos;
            }
        }

        private void SetVariable(ref short pos, short[] locals, List<short> variables) {
            short variableIndex = BitConverter.ToInt16(bytes.Skip(pos).Take(2).ToArray());
            pos += 2;

            while (variables.Count <= variableIndex) {
                variables.Add(0);
            }

            variables[variableIndex] = Calculate(ref pos, locals, variables);
        }

        private short Calculate(ref short pos, short[] locals, List<short> variables) {
            byte calculationAmount = bytes[pos];
            pos++;

            short value = 0;
            for (int i = 0; i < calculationAmount; i++) {
                byte calculationType = bytes[pos];
                pos++;
                short tempValue = GetValue(ref pos, locals, variables);

                switch (calculationType) {
                    case 0:
                        value += tempValue;
                        break;

                    case 1:
                        value -= tempValue;
                        break;

                    case 2:
                        value *= tempValue;
                        break;

                    case 3:
                        value /= tempValue;
                        break;

                    case 4:
                        value %= tempValue;
                        break;

                    case 5:
                        value &= tempValue;
                        break;

                    case 6:
                        value |= tempValue;
                        break;

                    case 7:
                        value ^= tempValue;
                        break;

                    case 8:
                        value = (short)(value == tempValue ? 1 : 0);
                        break;

                    case 9:
                        value = (short)(value != tempValue ? 1 : 0);
                        break;

                    case 10:
                        value = (short)(value < tempValue ? 1 : 0);
                        break;

                    case 11:
                        value = (short)(value > tempValue ? 1 : 0);
                        break;
                }
            }

            return value;
        }

        private short GetValue(ref short pos, short[] locals, List<short> variables) {
            short value = 0;
            switch (bytes[pos]) {
                case 0: {
                        pos++;
                        value = BitConverter.ToInt16(bytes.Skip(pos).Take(2).ToArray());
                        pos += 2;
                        break;
                    }
                case 1: {
                        pos++;
                        short variableIndex = BitConverter.ToInt16(bytes.Skip(pos).Take(2).ToArray());
                        pos += 2;
                        value = variables[variableIndex];
                        break;
                    }
                case 2: {
                        pos++;
                        short funcPointer = BitConverter.ToInt16(bytes.Skip(pos).Take(2).ToArray());
                        pos += 2;
                        short parameterAmount = BitConverter.ToInt16(bytes.Skip(pos).Take(2).ToArray());
                        pos += 2;
                        short[] parameters = new short[parameterAmount];
                        for (int i = 0; i < parameterAmount; i++) {
                            parameters[i] = GetValue(ref pos, locals, variables);
                        }
                        value = Run(funcPointer, parameters, variables);
                        break;
                    }
                case 3: {
                        pos++;
                        short variableIndex = BitConverter.ToInt16(bytes.Skip(pos).Take(2).ToArray());
                        pos += 2;
                        value = locals[variableIndex];
                        break;
                    }
            }
            return value;
        }
    }
}
