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


            //Magic magic = null;
            if (args.Length > 0)
            {
                string path = args[0];

                if (isStartedFromExplorer)
                {
                    Console.WriteLine("File:      " + Path.GetFullPath(path));
                }

                string mime = MIMEHelper.GetMIMEType(path);

                Console.WriteLine("Info:      " + MIMEHelper.GetDescription(path));
                Console.WriteLine("MIME-Type: " + mime);
                Console.WriteLine("Encoding:  " + MIMEHelper.GetEncoding(path));
                //string ext = HeyRed.Mime.MimeTypesMap.GetExtension(mime);
                Console.WriteLine("Extension: ." + MIMEHelper.GetExtension(mime));


                if(mime == "application/x-dosexec")
                {
                //    var reader = new PeHeaderReader(path);
                //    var foo = reader.FileHeader;
                }

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
