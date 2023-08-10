using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using USM.Lal.Runtime.RefWrappers;

namespace USM.Lal.Runtime {
    public class AsyncLalRuntime {

        private static readonly Random random = new Random();

        private DateTime lastFrame = DateTime.MinValue;
        private readonly byte[] bytes;

        private short currentArrayLength = 0;
        private readonly short[] arrayBuffer = new short[short.MaxValue];
        private DateTime lastScriptframe = DateTime.MinValue;

        public Func<short, short, short, short, Task> SetLed { get; set; }
        public Func<Task> UpdateLeds { get; set; }
        public Func<bool> ShouldStop { get; set; }
        public short LedCount { get; set; }

        public AsyncLalRuntime(byte[] bytes) {
            this.bytes = bytes;
        }

        private short CreateArray(short length) {
            short pointer = currentArrayLength;
            currentArrayLength += length;
            return pointer;
        }


        private async Task WaitFrame(short time) {
            TimeSpan sleepTime = TimeSpan.FromMilliseconds(time) - (DateTime.Now - lastScriptframe);
            if (sleepTime.TotalMilliseconds > 0) {
                await Task.Delay(sleepTime);
            }

            lastScriptframe = DateTime.Now;
        }

        public async Task Run() {
            List<short> variables = new List<short> {
                0,
                LedCount
            };

            short pos = 0;
            await Run(pos, Array.Empty<short>(), variables);
        }


        private async Task<short> Run(short startPos, short[] locals, List<short> variables) {
            var pos = new RefShortWrapper(startPos);
            if (pos.Value < 0) {
                switch (pos.Value) {
                    case -1:
                        await SetLed.Invoke(locals[0], locals[1], locals[2], locals[3]);
                        return 0;
                    case -2: {
                            await UpdateLeds.Invoke();
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
                        await WaitFrame(locals[0]);
                        return 0;
                    case -8:
                        Thread.Sleep(locals[0]);
                        return 0;
                    case -9: {
                            DateTime now = DateTime.Now;
                            return (short)((now.Hour * 60) + now.Minute);
                        }
                    default:
                        return 0;
                }
            }

            while (pos.Value < bytes.Length && !ShouldStop.Invoke()) {
                switch (bytes[pos.Value++]) {
                    case 0: {
                            short value = await Calculate(pos, locals, variables);
                            return value;
                        }
                    case 1: {
                            await SetVariable(pos, locals, variables);
                            break;
                        }
                    case 2: {
                            Jump(pos, locals, variables);
                            break;
                        }
                    case 3: {
                            await GetValue(pos, locals, variables);
                            break;
                        }
                    case 4: {
                            await SetLocal(pos, locals, variables);
                            break;

                        }

                    default:
                        break;
                }
            }
            return 0;
        }

        private byte GetByte(RefShortWrapper pos) {
            return bytes[pos.Value++];
        }

        private short GetShort(RefShortWrapper pos) {
            return (short)(bytes[pos.Value++] + (bytes[pos.Value++] << 8));
        }
        private async Task SetLocal(RefShortWrapper pos, short[] locals, List<short> variables) {
            short index = GetShort(pos);
            locals[index] = await Calculate(pos, locals, variables);
        }

        private void Jump(RefShortWrapper pos, short[] locals, List<short> variables) {
            short variableIndex = GetShort(pos);
            short newPos = GetShort(pos);
            if (variableIndex == -1 || variables[variableIndex] == 0) {
                pos.Value = newPos;
            }
        }

        private async Task SetVariable(RefShortWrapper pos, short[] locals, List<short> variables) {
            short variableIndex = GetShort(pos);

            while (variables.Count <= variableIndex) {
                variables.Add(0);
            }

            variables[variableIndex] = await Calculate(pos, locals, variables);
        }

        private async Task<short> Calculate(RefShortWrapper pos, short[] locals, List<short> variables) {
            byte calculationAmount = GetByte(pos);

            short value = 0;
            for (int i = 0; i < calculationAmount; i++) {
                byte calculationType = GetByte(pos);
                short tempValue = await GetValue(pos, locals, variables);

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

        private async Task<short> GetValue(RefShortWrapper pos, short[] locals, List<short> variables) {
            short value = 0;
            switch (bytes[pos.Value++]) {
                case 0: {
                        value = GetShort(pos);
                        break;
                    }
                case 1: {
                        short variableIndex = GetShort(pos);
                        value = variables[variableIndex];
                        break;
                    }
                case 2: {
                        short funcPointer = GetShort(pos);
                        short parameterAmount = GetShort(pos);
                        short[] parameters = new short[parameterAmount];
                        for (int i = 0; i < parameterAmount; i++) {
                            parameters[i] = await GetValue(pos, locals, variables);
                        }
                        value = await Run(funcPointer, parameters, variables);
                        break;
                    }
                case 3: {
                        short variableIndex = GetShort(pos);
                        value = locals[variableIndex];
                        break;
                    }
            }
            return value;
        }
    }
}
