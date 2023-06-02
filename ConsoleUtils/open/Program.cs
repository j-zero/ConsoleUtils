using System;
using System.Diagnostics;
using System.IO;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        bool openWith = false;
        if (args.Length > 0) {
            var f = Environment.CommandLine.Replace("\"" + Environment.GetCommandLineArgs()[0] + "\"", "").TrimStart();
            if (f.StartsWith("-w ") || f.StartsWith("--with ")) // open With
            {
                f = f.Substring(3);
                openWith = true;
            }
            if (f == "--help" || f == "-w")
            {
                Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [Options] {{file|url}}\n  If no parameter is given, the current directory will be opened.");
                Console.WriteLine($"Options:");
                Console.WriteLine($"  --with,-w: Show \"Open With\" dialog");

                return;
            }
            else
            {
                try
                {
                    Start(f, openWith);
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
                                System.IO.File.Create(f);
                                Start(f, openWith);
                            }
                            catch(Exception ex2)
                            {
                                Console.Write($"Error: {ex2.Message}");
                                Environment.Exit(1);
                            }
                        }
                    }
                }
            }
        }
        else
            Start(Environment.CurrentDirectory, false);

        Environment.Exit(0);
    }

    static void Start(string filename, bool openWith)
    {
        if (openWith)
        {

            Process.Start("rundll32.exe", "shell32.dll, OpenAs_RunDLL " + Path.GetFullPath(filename));
        }
        else
        {
            Process.Start(filename);
        }
    }
}
