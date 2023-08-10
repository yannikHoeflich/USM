using System.Drawing;

using USM.Debug;
using USM.Devices;

namespace USM.Lal.Compiler {
    public static class DynamicCompiler {
        private static Compiler compiler = new Compiler();
        public static byte[] OneColor(byte r, byte g, byte b, Logger logger = null) {
            string code = @$"i = 0; while i < LED_COUNT{{ Set(i, {r}, {g}, {b}); i = i + 1; }}; UpdateLeds();";
            compiler.Compile(DeviceType.Light, code, out byte[] compiled, logger);
            return compiled;
        }
        public static byte[] OneColorMatrix(byte r, byte g, byte b, Logger logger = null) {
            string code = @$"x = 0; y = 0; while y < HEIGHT{{ x = 0; while x < WIDTH {{ Set(x, y, {r}, {g}, {b}); x = x + 1; }} y = y + 1; }}; UpdateLeds();";
            compiler.Compile(DeviceType.Matrix,code, out byte[] compiled, logger);
            return compiled;
        }

        public static byte[] AlarmClock(Color color, int minutesOfDay, int fadeInMinutes, int goOutAfterMinutes, Logger logger = null) {
            string code = $@"
                while 1{{
                    time = GetTime();
                    margin = {minutesOfDay} - time;
                    isLater = 0 - {goOutAfterMinutes} > margin;
                    if margin < {fadeInMinutes} & isLater{{
                        r = {color.R};
                        g = {color.G};
                        b = {color.B};
                        if margin < 0{{
                            r = {fadeInMinutes} - margin * {color.R} / {fadeInMinutes};
                            g = {fadeInMinutes} - margin * {color.G} / {fadeInMinutes};
                            b = {fadeInMinutes} - margin * {color.B} / {fadeInMinutes};
                        }};
                        i = 0;
                        while i < LED_COUNT{{
                            Set(i, r, g, b); i = i + 1;
                        }};
                        UpdateLeds();
                    }};
                    SleepFrame(1000);
                }};";
            compiler.Compile(DeviceType.Light, code, out byte[] compiled, logger);
            return compiled;
        }

        public static byte[] CreateOnOffAnimation(bool on, Logger logger = null) {
            int onNum = on? 1 : 0;
            string code = @$"i = 0; while i < LED_COUNT{{ Set(i, {onNum}); i = i + 1; }}; UpdateLeds();";
            compiler.Compile(DeviceType.Light, code, out byte[] compiled, logger);
            return compiled;
        }
    }
}