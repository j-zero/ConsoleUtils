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
        static string color1 = "cb997e";
        static string color2 = "b7b7a4";
        //static bool debug = false;
        static bool exit = false;
        static string prompt = " $ ".Pastel(color1);
        static string thema = "notes";
        static string dateTimeFormat = "yyyy'-'MM'-'dd' 'HH':'mm':'ss";
        static char cmdSeperator = ' ';
        static string folder = "";




        [STAThread] // needed for clipboard data
        static void Main(string[] args)
        {
            Console.Clear();
            ShowVersion();
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

                string line = ReadLine.Read($"{thema.Pastel(color2)}{prompt}");
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
                        Console.WriteLine(dir.Pastel(color2));
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
                    //Console.WriteLine($"Switched to {thema.Pastel(color1)}");
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
            Console.WriteLine(":".Pastel(color2) + "COMMAND".Pastel(color1));
            Console.WriteLine("\t" + ":".Pastel(color2) + "q".Pastel(color1) + "[".Pastel(ColorTheme.DarkText) + "uit".Pastel(color2) + "]".Pastel(ColorTheme.DarkText) + "     - quits this application".Pastel(ColorTheme.Text));
            Console.WriteLine("\t" + ":".Pastel(color2) + "i".Pastel(color1) + "[".Pastel(ColorTheme.DarkText) + "mage".Pastel(color2) + "]".Pastel(ColorTheme.DarkText) + "    - saves screenshot from clipboard".Pastel(ColorTheme.Text));
            Console.WriteLine("\t" + ":".Pastel(color2) + "clear".Pastel(color1) + "      - clears current console".Pastel(ColorTheme.Text));
            Console.WriteLine("\t" + ":".Pastel(color2) + "l".Pastel(color1) + "[".Pastel(ColorTheme.DarkText) + "ist".Pastel(color2) + "]".Pastel(ColorTheme.DarkText) + "|".Pastel(ColorTheme.Text) + "ls".Pastel(color1) + "  - list all themes".Pastel(ColorTheme.Text));
            Console.WriteLine("\t" + ":".Pastel(color2) + "cat".Pastel(color1) + "        - shows current content".Pastel(ColorTheme.Text));
            Console.WriteLine("\t" + ":".Pastel(color2) + "open".Pastel(color1) + "       - open current folder".Pastel(ColorTheme.Text));
            Console.WriteLine();
            Console.WriteLine("#".Pastel(color2) + "THEME".Pastel(color1) + ", switches to theme " + "COMMAND".Pastel(color1) + ".");
            Console.WriteLine("\t" + "#".Pastel(color2) + "theme".Pastel(color1) + " " + "[".Pastel(ColorTheme.DarkText) + "text".Pastel(color2) + "]".Pastel(ColorTheme.DarkText) + " - adds text to theme.".Pastel(ColorTheme.Text));

        }

        static void ShowVersion()
        {
            Console.WriteLine(@"█    ████▄   ▄▀     ▄   ████▄    ▄▄▄▄▀ ▄███▄   ".Pastel("#b98b73"));
            Console.WriteLine(@"█    █   █ ▄▀        █  █   █ ▀▀▀ █    █▀   ▀  ".Pastel("#cb997e"));
            Console.WriteLine(@"█    █   █ █ ▀▄  ██   █ █   █     █    ██▄▄    ".Pastel("#ddbea9"));
            Console.WriteLine(@"███▄ ▀████ █   █ █ █  █ ▀████    █     █▄   ▄▀ ".Pastel("#ffe8d6"));
            Console.WriteLine(@"    ▀       ███  █  █ █         ▀      ▀███▀   ".Pastel("#d4c7b0"));
            Console.WriteLine((@"                 █   ██  v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()).Pastel("#b7b7a4"));

            Console.WriteLine($"{"lognote".Pastel(color1)} is part of " + ConsoleHelper.GetVersionString(color2, color2));
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
