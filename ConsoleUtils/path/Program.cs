using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace path
{
    internal class Program
    {
    // raw command line without the program name (.NET: GetCommandLineArgs()[0] is the dll, not the exe)
    static string GetRawArguments()
    {
        string cl = Environment.CommandLine;
        string rest;
        if (cl.StartsWith("\""))
        {
            int end = cl.IndexOf('"', 1);
            rest = end >= 0 ? cl.Substring(end + 1) : "";
        }
        else
        {
            int sp = cl.IndexOf(' ');
            rest = sp >= 0 ? cl.Substring(sp + 1) : "";
        }
        return rest.TrimStart();
    }

        [STAThreadAttribute]
        static void Main(string[] args)
        {
            var path = "";
            if (args.Length > 0)
            {
                var f = GetRawArguments();
                if (File.Exists(f) || Directory.Exists(f))
                    path = Path.GetFullPath(f);
                else
                {
                    Console.Error.WriteLine($"Not found: {f}");
                    Environment.Exit(1);
                }
            }
            else
                path = Environment.CurrentDirectory;
            
            Console.WriteLine(path);
            Clipboard.SetText(path);
        }


    }
}
