using SevenZipExtractor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace unpack
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string relativePath = @"test.zip";
            string fullPath = Path.GetFullPath(relativePath);
            Console.WriteLine(fullPath);
            using (ArchiveFile file = new ArchiveFile(fullPath))
            {
                foreach(var entry in file.Entries)
                {
                    Console.WriteLine(entry.FileName);
                }
            }
        }
    }
}
