using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace trash
{
    internal class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0 || args.Any(a => a is "-h" or "--help" or "/?"))
            {
                Console.WriteLine("Usage: rip <file|directory|wildcard> [...]");
                Console.WriteLine();
                Console.WriteLine("Moves files and directories to the recycle bin instead of deleting them.");
                Console.WriteLine("Wildcards (*, ?) are expanded by rip. Exit code 1 if something was not found or could not be moved.");
                return args.Length == 0 ? 1 : 0;
            }

            bool quiet = args.Contains("-q") || args.Contains("--quiet");
            var patterns = args.Where(a => a != "-q" && a != "--quiet").ToArray();

            int recycled = 0, failed = 0;
            var done = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string pattern in patterns)
            {
                // the same file given twice is moved once
                if (!pattern.Contains('*') && !pattern.Contains('?') && done.Contains(SafeFullPath(pattern)))
                    continue;

                StringCollection files;
                try
                {
                    files = FileAndDirectoryFilter.Get(new[] { pattern },
                        FileAndDirectoryFilter.FileAndDirectoryMode.Directories | FileAndDirectoryFilter.FileAndDirectoryMode.Files);
                }
                catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or ArgumentException)
                {
                    Console.Error.WriteLine($"{pattern}: {ex.Message}");
                    failed++;
                    continue;
                }

                if (files.Count == 0)
                {
                    Console.Error.WriteLine($"\"{pattern}\" not found.");
                    failed++;
                    continue;
                }

                foreach (string file in files)
                {
                    if (!done.Add(System.IO.Path.GetFullPath(file)))
                        continue; // given twice or matched by two patterns

                    if (FileOperationAPIWrapper.Recylce(file, FileOperationAPIWrapper.FileOperationFlags.FOF_SILENT))
                    {
                        recycled++;
                        if (!quiet)
                            Console.WriteLine($"recycled: {file}");
                    }
                    else
                    {
                        failed++;
                        Console.Error.WriteLine($"could not recycle: {file}");
                    }
                }
            }

            if (!quiet && recycled > 1)
                Console.WriteLine($"{recycled} entries recycled.");
            return failed == 0 ? 0 : 1;
        }

        static string SafeFullPath(string path)
        {
            try { return System.IO.Path.GetFullPath(path); }
            catch (ArgumentException) { return path; }
        }
    }
}
