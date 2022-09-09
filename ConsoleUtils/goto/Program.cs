using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace notes
{
    internal class Program
    {
        static string extension = ".md";
        static CmdParser cmd;

        static void Main(string[] args)
        {
            string user_profile_path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".datenpirat", "notes");
            if(!Directory.Exists(user_profile_path))
                Directory.CreateDirectory(user_profile_path);

            cmd = new CmdParser(args)
            {
                {"save","s", CmdCommandTypes.VERB, "Save current folder" },
                { "delete", "d", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, null }
                }, "Delete folder" },
                { "load", "l", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, null }
                }, "Load folder" }
            };
            cmd.DefaultParameter = "save";
            cmd.Parse();
            
            string path = null;

            if (cmd["load"].Strings.Length > 0 && cmd["load"].Strings[0] != null)
                path = cmd["load"].Strings[0];

            string file_to_load = Path.Combine(user_profile_path, path + extension);

            if (File.Exists(file_to_load))
            {
                try
                {
                    System.Diagnostics.Process.Start(file_to_load);
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    ;
                }
            }
            else
            {
                //Console.Write($"\"{path}\" not found, create? [y/N] ");
                //ConsoleKey response = Console.ReadKey(false).Key;
                //Console.WriteLine();
                //if (response == ConsoleKey.Y)
                //{
                    try
                    {
                        System.IO.File.Create(file_to_load);
                        System.Diagnostics.Process.Start(file_to_load);
                    }
                    catch (Exception ex2)
                    {
                        Console.Write($"Error: {ex2.Message}");
                        Environment.Exit(1);
                    }
                //}
            }





        }
    }
}
