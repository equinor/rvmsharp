using rvmsharp.Rvm;
using System;
using System.IO;

namespace rvmsharp
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileStream = File.OpenRead(@"e:\gush\projects\ModelSlicer\work\rvm\JSR\RC110-PSUP.RVM");
            var rvm = RvmParser.ReadRvm(fileStream);
            Console.WriteLine("Done!");
        }
    }
}
