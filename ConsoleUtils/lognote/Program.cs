using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pastel;
using System.Threading;
using System.IO;
using System.Drawing;
using System.Diagnostics;

namespace lognote
{
    internal class Program
    {
        //static bool debug = false;
        static bool exit = false;
        static string prompt = " $ ".Pastel(ColorTheme.OffsetColor);
        static string thema = "notes";
        static string dateTimeFormat = "yyyy'-'MM'-'dd' 'HH':'mm':'ss";
        static char cmdSeperator = ' ';
        static string folder = "";


        [STAThread] // needed for clipboard data
        static void Main(string[] args)
        {

            //var f = Environment.CommandLine;
            //Console.WriteLine(f);
            
            // setup
            folder = PathHelper.GetSpecialFolder(Environment.SpecialFolder.MyDocuments, "LogNote");


            Globals.db.folder = folder;
            Globals.db.dateTimeFormat = dateTimeFormat;

            GetLastThema();
            Globals.db.thema = thema;

            ReadLine.AutoCompletionHandler = new AutoCompletionHandler();
            ReadLine.RollingComplete = false;
            ReadLine.RollingShowSuggestions = false;

            LoadHistory();

            //if (!string.IsNullOrWhiteSpace(f))
                ;// ParseLine(f);

            // main loop
            do
            {

                string line = ReadLine.Read($"{thema.Pastel(ColorTheme.OffsetColorHighlight)}{prompt}");
                if (!string.IsNullOrWhiteSpace(line))
                {
                    ParseLine(line);
                }
                else
                {
                    Globals.db.PrintMessage("");
                }

            } while (!exit);
        }

        static void AddToHistory(string line)
        {
            ReadLine.AddHistory(line);
            File.AppendAllText(Path.Combine(folder, ".lognote_history"), line + Environment.NewLine); // todo error handling
        }

        static void LoadHistory()
        {
            string path = Path.Combine(folder, ".lognote_history");
            if(File.Exists(path))
                ReadLine.AddHistory(File.ReadAllLines(path));
        }

        static void SaveLastThema(string thema)
        {
            //ReadLine.AddHistory(line);
            File.WriteAllText(Path.Combine(folder, ".lognote_thema"), thema + Environment.NewLine); // todo error handling
        }
        static void GetLastThema()
        {
            string path = Path.Combine(folder, ".lognote_thema");
            if (File.Exists(path))
            {
                string tmpThema = File.ReadAllLines(path)[0];
                if (!string.IsNullOrEmpty(tmpThema))
                    thema = tmpThema;
            }
        }


        static void ParseLine(string line)
        {
            string cmd = line.Trim().ToLower();
            if (cmd == "?")
            {
                ShowHelp();
            }
            else if (cmd.StartsWith(":"))
            {
                AddToHistory(line);
                if (cmd == ":quit" || cmd == ":q")
                {
                    exit = true;
                }
                else if (cmd == ":open")
                {
                    Process.Start(Globals.db.GetFolder(thema));
                }
                else if (cmd == ":image" || cmd == ":i")
                {
                    Globals.db.SaveScreenshot(thema);
                }
                else if (cmd == ":clear")
                {
                    Console.Clear();
                }
                else if (cmd == ":list" || cmd == ":ls" || cmd == ":l")
                {
                    string[] dirs = Globals.db.GetAllThemas();
                    foreach(string dir in dirs)
                        Console.WriteLine(dir.Pastel(ColorTheme.Default2));
                }
                else if (cmd.StartsWith(":cat"))
                {
                    string[] parts = cmd.Substring(1).Split(cmdSeperator);
                    if(parts.Length == 1) // command
                        Globals.db.PrintData(thema);
                    else
                    {
                        string tmpThema = parts[1];
                        if (tmpThema.StartsWith("#"))
                            tmpThema = tmpThema.Substring(1);

                        Globals.db.PrintData(tmpThema);
                    }

                    ;
                }
                else
                {
                    Console.WriteLine($"unknown command".Pastel(ColorTheme.Error1));
                }
            }
            else if (cmd.StartsWith("#"))
            {
                string[] parts = cmd.Substring(1).Split(cmdSeperator);
  
                if (parts.Length > 1)
                {
                    string rest = cmd.Substring(cmd.IndexOf(cmdSeperator) + 1);
                    string tmpThema = parts.FirstOrDefault();
                    //SaveData(parts.FirstOrDefault(), rest);
                    Globals.db.SaveData(tmpThema, rest);
                   // ParseCommand(rest); // recursion!
                }
                else
                {
                    thema = parts.FirstOrDefault();
                    SaveLastThema(thema);
                    //Console.WriteLine($"Switched to {thema.Pastel(ColorTheme.Default1)}");
                }

                
            }
            else // Text
            {
                Globals.db.SaveData(thema,cmd);

                ///string saveLine = line.Replace("\r\n", "\r").Replace("\n", "\r");

                
            }
        }



        static void ShowHelp()
        {
            Console.WriteLine(":".Pastel(ColorTheme.Default2) + "COMMAND".Pastel(ColorTheme.Default1));
            Console.WriteLine("\t" + ":".Pastel(ColorTheme.Default2) + "q".Pastel(ColorTheme.Default1) + "[".Pastel(ColorTheme.DarkText) + "uit".Pastel(ColorTheme.Default2) + "]".Pastel(ColorTheme.DarkText) + "     - quits this application".Pastel(ColorTheme.Text));
            Console.WriteLine("\t" + ":".Pastel(ColorTheme.Default2) + "i".Pastel(ColorTheme.Default1) + "[".Pastel(ColorTheme.DarkText) + "mage".Pastel(ColorTheme.Default2) + "]".Pastel(ColorTheme.DarkText) + "    - saves screenshot from clipboard".Pastel(ColorTheme.Text));
            Console.WriteLine("\t" + ":".Pastel(ColorTheme.Default2) + "clear".Pastel(ColorTheme.Default1) + "      - clears current console".Pastel(ColorTheme.Text));
            Console.WriteLine("\t" + ":".Pastel(ColorTheme.Default2) + "l".Pastel(ColorTheme.Default1) + "[".Pastel(ColorTheme.DarkText) + "ist".Pastel(ColorTheme.Default2) + "]".Pastel(ColorTheme.DarkText) + "|".Pastel(ColorTheme.Text) + "ls".Pastel(ColorTheme.Default1) + "  - list all themes".Pastel(ColorTheme.Text));
            Console.WriteLine("\t" + ":".Pastel(ColorTheme.Default2) + "cat".Pastel(ColorTheme.Default1) + "        - shows current content".Pastel(ColorTheme.Text));
            Console.WriteLine("\t" + ":".Pastel(ColorTheme.Default2) + "open".Pastel(ColorTheme.Default1) + "       - open current folder".Pastel(ColorTheme.Text));
            Console.WriteLine();
            Console.WriteLine("#".Pastel(ColorTheme.Default2) + "THEME".Pastel(ColorTheme.Default1) + ", switches to theme " + "COMMAND".Pastel(ColorTheme.Default1) + ".");
            Console.WriteLine("\t" + "#".Pastel(ColorTheme.Default2) + "theme".Pastel(ColorTheme.Default1) + " " + "[".Pastel(ColorTheme.DarkText) + "text".Pastel(ColorTheme.Default2) + "]".Pastel(ColorTheme.DarkText) + " - adds text to theme.".Pastel(ColorTheme.Text));

        }





    }
    class AutoCompletionHandler : IAutoCompleteHandler
    {
        
        public char[] Separators { get; set; } = new char[] { ' ', ':', '#' };
        public string[] GetSuggestions(string text, int index)
        {
            string currentWord = text.Substring(index);

            //if (text.StartsWith("git "))
            //    return (new string[] { "init", "clone", "pull", "push" }).Where(x => x.StartsWith(currentWord)).ToArray();
            //else 
            if (text.StartsWith("#"))
            {
                string word = text.Substring(1);
                string[] dirs = Globals.db.GetAllThemas();
                string[] suggestions = dirs.Where(x => x.StartsWith(word.Trim())).ToArray();
                return suggestions;
            }
            else if (text.StartsWith(":cat"))
            {
                //string word = text.Substring(1);
                string[] dirs = Globals.db.GetAllThemas();
                string[] suggestions = dirs.Where(x => x.StartsWith(currentWord.Trim())).ToArray();
                return suggestions;
            }
            else
                return null;
        }
    }
}
