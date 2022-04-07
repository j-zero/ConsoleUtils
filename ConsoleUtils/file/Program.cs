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
            Magic magic = null;
            if (args.Length > 0)
            {
                string path = args[0];

                magic = new Magic(MagicOpenFlags.MAGIC_NONE);
                Console.WriteLine("Info:      " + magic.Read(path));

                magic = new Magic(MagicOpenFlags.MAGIC_MIME_ENCODING);
                Console.WriteLine("Encoding:  " + magic.Read(path));

                magic = new Magic(MagicOpenFlags.MAGIC_MIME_TYPE);
                Console.WriteLine("MIME-Type: " + magic.Read(path)); 

                //Console.WriteLine(magicStr);
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
