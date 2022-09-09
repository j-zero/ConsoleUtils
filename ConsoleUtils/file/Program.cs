using HeyRed.Mime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace file
{
    internal class Program
    {
        
        static int Main(string[] args)
        {
            int exitCode = 0;
            string parrentProcess = ConsoleUtilsCore.ParentProcessUtilities.GetParentProcess().ProcessName;
            //Console.WriteLine(parrentProcess);

            bool isStartedFromExplorer = System.Diagnostics.Debugger.IsAttached || parrentProcess.ToLower().Contains("explorer"); // is debugger attached or started by double-click/file-drag


            Magic magic = null;
            if (args.Length > 0)
            {
                string path = args[0];

                if (isStartedFromExplorer)
                {
                    Console.WriteLine("File:      " + Path.GetFullPath(path));
                }

                magic = new Magic(MagicOpenFlags.MAGIC_NONE);
                Console.WriteLine("Info:      " + magic.Read(path));

                magic = new Magic(MagicOpenFlags.MAGIC_MIME_ENCODING);
                Console.WriteLine("Encoding:  " + magic.Read(path));

                magic = new Magic(MagicOpenFlags.MAGIC_MIME_TYPE);
                Console.WriteLine("MIME-Type: " + magic.Read(path));

                //Console.WriteLine(magicStr);
                exitCode = 0;
            }
            else
            {
                Console.WriteLine("No file given!");
                exitCode = 1;
            }

            if (isStartedFromExplorer) {
                Console.WriteLine("\nPress any key to exit.");
                Console.ReadKey();
            }

            return exitCode;
        }
    }
}
