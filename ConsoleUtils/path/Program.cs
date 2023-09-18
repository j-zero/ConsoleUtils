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
        [STAThreadAttribute]
        static void Main(string[] args)
        {
            var path = "";
            if (args.Length > 0)
            {
                var f = Environment.CommandLine.Replace("\"" + Environment.GetCommandLineArgs()[0] + "\"", "").TrimStart();
                if (File.Exists(f) || Directory.Exists(f))
                    path = Path.GetFullPath(f);
            }
            else
                path = Environment.CurrentDirectory;
            
            Console.WriteLine(path);
            Clipboard.SetText(path);
        }


    }
}
