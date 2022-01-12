using HeyRed.Mime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace file
{
    internal class Program
    {
        static int Main(string[] args)
        {
            if(args.Length > 0)
            {
                string path = args[0];
                var magic = new Magic(MagicOpenFlags.MAGIC_NONE);
                Console.WriteLine(magic.Read(path));
                return 0;
            }
            else
            {
                Console.WriteLine("No file given!");
                return 1;
            }
        }
    }
}
