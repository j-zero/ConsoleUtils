using System;
using System.IO;

namespace touch
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                var f = Environment.CommandLine.Replace("\"" + Environment.GetCommandLineArgs()[0] + "\"", "").TrimStart();

                try
                {
                    string fullPath = Path.GetFullPath(f);
                    if (!File.Exists(fullPath))
                        File.Create(fullPath).Close();
                    File.SetLastWriteTimeUtc(fullPath, DateTime.UtcNow);
                }
                catch (Exception ex2)
                {
                    Console.Write($"Error: {ex2.Message}");
                    Environment.Exit(1);
                }

            }
            else
            {
                Console.Write($"Usage: touch [file]\n");
                Environment.Exit(1);
            }

            Environment.Exit(0);
        }
    }
}
