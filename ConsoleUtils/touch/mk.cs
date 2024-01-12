using System;
using System.IO;

namespace touch
{
    internal class mk
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                var cmdArgs = Environment.GetCommandLineArgs();
                var replaceStr = cmdArgs[0];

                if (Environment.CommandLine.StartsWith("\""))
                    replaceStr = "\"" + cmdArgs[0] + "\"";
                
                var f = Environment.CommandLine.Replace(replaceStr, "").TrimStart();

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
