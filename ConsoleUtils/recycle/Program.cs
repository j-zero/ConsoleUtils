using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace trash
{
    internal class Program
    {
        static void Main(string[] args)
        {

            foreach(string arg in args)
            {
                try
                {
                    var files = FileAndDirectoryFilter.Get(args, FileAndDirectoryFilter.FileAndDirectoryMode.Directories | FileAndDirectoryFilter.FileAndDirectoryMode.Files);
                    foreach (var file in files)
                        FileOperationAPIWrapper.Recylce(file,FileOperationAPIWrapper.FileOperationFlags.FOF_SILENT);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                
            }
        }
    }
}
