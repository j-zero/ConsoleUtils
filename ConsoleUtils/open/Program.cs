using System;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length > 0) {
            string f = args[0];
            if (f == "--help")
            {
                Console.Write($"Usage: open [file|url]\nIf no parameter is given, the current directory will be opened.");
                return;
            }
            else
            {
                try
                {
                    System.Diagnostics.Process.Start(f);
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    if (ex.NativeErrorCode == 0x02) //https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-erref/18d8fbe8-a967-4f1c-ae50-99ca8e491d2d
                    {
                        Console.Write($"File \"{f}\" not found, create? [y/N]");
                        ConsoleKey response = Console.ReadKey(false).Key;
                        if (response == ConsoleKey.Y)
                        {
                            System.IO.File.Create(f);
                            System.Diagnostics.Process.Start(f);
                        }
                    }
                }
            }
        }
        else
            System.Diagnostics.Process.Start(Environment.CurrentDirectory);
    }
}
