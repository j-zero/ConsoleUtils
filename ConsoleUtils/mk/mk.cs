using System;
using System.IO;

namespace touch
{
    internal class mk
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

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                var f = GetRawArguments();

                try
                {
                    string fullPath = Path.GetFullPath(f);

                    if (fullPath.EndsWith(Path.DirectorySeparatorChar)) // Directory
                    {
                        if(!Directory.Exists(fullPath))
                            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                    }
                    else
                    {
                        if (!File.Exists(fullPath))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                            File.Create(fullPath).Close();
                        }
                        File.SetLastWriteTimeUtc(fullPath, DateTime.UtcNow);
                    }


                }
                catch (Exception ex2)
                {
                    Console.Write($"Error: {ex2.Message}");
                    Environment.Exit(1);
                }

            }
            else
            {
                
                Console.Write($"Usage: mk <path{Path.DirectorySeparatorChar}>[file]\n");
                Environment.Exit(1);
            }

            Environment.Exit(0);
        }
    }
}
