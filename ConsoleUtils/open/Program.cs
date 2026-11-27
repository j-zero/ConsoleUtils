using System;
using System.Diagnostics;
using System.IO;

static class Program
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

    [STAThread]
    static void Main(string[] args)
    {
        bool openWith = false;

        if (args.Length > 0) {
            var f = GetRawArguments();

            if (f.StartsWith("-w ")) // open With
            {
                f = f.Substring(3);
                openWith = true;
            }

            if (f == "--help" || f == "-w")
            {
                Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [Options] {{file|url}}\n  If no parameter is given, the current directory will be opened.");
                Console.WriteLine($"Options:");
                Console.WriteLine($" -w: Show \"Open With\" dialog.");

                return;
            }
            else
            {
                try
                {
                    Start(f,openWith);
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    // File not found
                    if (ex.NativeErrorCode == 0x02) //https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-erref/18d8fbe8-a967-4f1c-ae50-99ca8e491d2d
                    {
                        Console.Write($"\"{f}\" not found, create? [y/N] ");
                        ConsoleKey response = Console.ReadKey(false).Key;
                        Console.WriteLine();
                        if (response == ConsoleKey.Y)
                        {
                            try
                            {
                                string fullPath = Path.GetFullPath(f);
                                string directory = Path.GetDirectoryName(fullPath);

                                if (!File.Exists(fullPath))
                                {
                                    Directory.CreateDirectory(directory);
                                    File.Create(fullPath).Close();
                                }

                                File.SetLastWriteTimeUtc(fullPath, DateTime.UtcNow);

                                Start(fullPath, openWith);
                            }
                            catch(Exception ex2)
                            {
                                Console.Write($"Error: {ex2.Message}");
                                Environment.Exit(1);
                            }
                        }
                    }
                    else
                    {
                        Console.Write($"Error: {ex.Message}");
                    }
                }
            }
        }
        else
            Start(Environment.CurrentDirectory, openWith);

        Environment.Exit(0);
    }

    static void Start(string filename, bool openWith)
    {
        var proc = new ProcessStartInfo();
        proc.UseShellExecute = true;
        proc.WorkingDirectory = Environment.CurrentDirectory;

        if (openWith)
        {
            proc.FileName = "rundll32.exe";
            proc.Arguments = "shell32.dll, OpenAs_RunDLL " + Path.GetFullPath(filename);
        }
        else
            proc.FileName = filename;

        Process.Start(proc);
    }
    
}
