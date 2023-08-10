using USM.Debug;
using USM.Devices;
using USM.Lal.Compiler;

namespace LalCompiler;
class Program {

    public static int Main(string[] args) {
        Logger logger = new Logger();
        logger.NewDataAvailable += (s, e) => Console.WriteLine(e);
        if(args.Length >= 3 && args[0] == "@a") {
            if(!Directory.Exists(args[1])) {
                Console.WriteLine("the directory does not exist");
                return 1;
            }
            if(!Directory.Exists(args[2]))
                Directory.CreateDirectory(args[2]);

            foreach(string file in Directory.GetFiles(Path.GetFullPath(args[1]))) {
                if(Path.GetExtension(file) != ".ulal")
                    continue;
                string output = Path.Combine(Path.GetFullPath(args[2]), Path.GetFileNameWithoutExtension(file) + ".ubc");
                Console.WriteLine($"building file {Path.GetFileName(file)}");
                if(Main(new string[] { file, output }) != 0) {
                    Console.WriteLine("\n\nAborted all following builds");
                    return 1;
                }
                Console.WriteLine("------------------------------\n\n");
            }
            return 0;
        }

        if(args.Length < 2) {
            Console.WriteLine("Syntax: [input file name] [output file name]");
            Console.WriteLine("Syntax: @a");
            return 1;
        }

        string inputFile = args[0];
        string outputFile = args[1];

        if(!File.Exists(inputFile)) {
            Console.WriteLine($"The input file \"{inputFile}\" does not exists");
            return 1;
        }

        var compiler = new Compiler();
        if(compiler.Compile(DeviceType.Light, File.ReadAllText(inputFile), out byte[] byteCode, logger)) {
            Console.WriteLine($"writing in file");
            File.WriteAllBytes(outputFile, byteCode);
            Console.WriteLine($"build successful");
            return 0;
        } else {
            Console.WriteLine("Build aborted");
            return 1;
        }
    }
}