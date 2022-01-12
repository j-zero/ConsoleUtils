using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace klemmbrett
{
    internal class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            /* Read STDIN
            if (Console.IsInputRedirected)
            {
                using (Stream s = Console.OpenStandardInput())
                {
                    using (StreamReader reader = new StreamReader(s))
                    {
                        Console.Write(reader.ReadToEnd());
                    }
                }
            }
            */
            if(args.Length == 0)
            {
                Console.WriteLine("No command given!");
                return 1;
            }
            string command = args[0];

            if (args.Length == 2) // command + parameter
            {
                if (command == "string" || command == "s")
                {
                    string str = args[1];
                    Clipboard.SetText(str);
                }
                else if (command == "text" || command == "t")
                {
                    string path = Path.GetFullPath(args[1]);
                    if (File.Exists(path))
                    {
                        string fileStr = File.ReadAllText(path);
                        Clipboard.SetText(fileStr);
                    }
                }
                else if (command == "copy" || command == "c")
                {        // copy files to clipyboard
                    string path = Path.GetFullPath(args[1]);
                    if (File.Exists(path))
                    {
                        Clipboard.SetFileDropList(new StringCollection() { path });
                    }
                    else if (Directory.Exists(path))
                    {
                        Clipboard.SetFileDropList(new StringCollection() { path });
                    }
                    else
                    {
                        Console.WriteLine($"File \"{path}\" not found!");
                        return 1;
                    }

                }
            }
            else if (args.Length == 1) // command only
            {
                if (command == "paste" || command == "p")
                {
                    if (ClipboardHelper.ContainsFileDropList())
                    {
                        foreach (string source in Clipboard.GetFileDropList())
                        {
                            var destination = Path.Combine(Environment.CurrentDirectory, Path.GetFileName(source));
                            try
                            {
                                // TODO: Ask for overrite
                                Console.WriteLine($"Copying \"{source}\" to \"{destination}\"");
                                File.Copy(source, destination, true);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("No pastable content!");
                        return 1;
                    }
                }
                else if(command == "show")
                {
                    if (ClipboardHelper.ContainsText())
                    {
                        WriteHeader("String:");
                        Console.Write(Clipboard.GetText());
                    }
                    else if(ClipboardHelper.ContainsFileDropList()){
                        WriteHeader("Files:");
                        foreach (string source in Clipboard.GetFileDropList())
                        {
                            Console.WriteLine(source);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Wat?");
                return 255;
            }


            return 2;
        }

        static void WriteHeader(string Text)
        {
            if (!Console.IsOutputRedirected)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(Text);
                Console.ResetColor();
            }
        }
    }
}
