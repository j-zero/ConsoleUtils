using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pastel;
using System.Threading;
using System.IO;
using System.Drawing;

namespace lognote
{
    internal class Program
    {
        static bool debug = false;
        static bool exit = false;
        static string prompt = " > ";
        static string thema = "default";
        static string dateTimeFormat = "yyyy'-'MM'-'dd' 'HH':'mm':'ss";
        

        [STAThread] // needed for clipboard data
        static void Main(string[] args)
        {

            Globals.db.dateTimeFormat = dateTimeFormat;

            ReadLine.AutoCompletionHandler = new AutoCompletionHandler();
            ReadLine.RollingComplete = false;
            ReadLine.RollingShowSuggestions = false;

            do
            {

                string line = ReadLine.Read($"{thema.Pastel(ColorTheme.Default1)}{prompt}");
                ParseCommand(line);

            } while (!exit);
        }


        static void ParseCommand(string line)
        {
            string cmd = line.Trim();
            if (cmd.StartsWith(":"))
            {
                if (cmd == ":exit" || cmd == ":quit" || cmd == ":q")
                {
                    exit = true;
                }
                else if (cmd == ":i")
                {
                    Globals.db.SaveScreenshot(thema);
                }
                else
                {
                    Console.WriteLine($"unkown command".Pastel(ColorTheme.Error));
                }
            }
            else if (cmd.StartsWith("#"))
            {
                string[] parts = cmd.Substring(1).Split(' ');
  
                if (parts.Length > 1)
                {
                    string rest = cmd.Substring(cmd.IndexOf(' ') + 1);
                    //SaveData(parts.FirstOrDefault(), rest);
                    ParseCommand(rest); // recursion!
                }
                else
                {
                    thema = parts.FirstOrDefault();
                    Console.WriteLine($"Switched to {thema.Pastel(ColorTheme.Default1)}");
                }

                
            }
            else // Text
            {
                Globals.db.SaveData(thema,cmd);

                ///string saveLine = line.Replace("\r\n", "\r").Replace("\n", "\r");

                
            }
        }



        static void PrintMessage(string msg, bool error = false)
        {
            if(error)
                Console.WriteLine($"{DateTime.Now.ToString(dateTimeFormat).Pastel(ColorTheme.OffsetColor)} {("(!) " + msg).Pastel(ColorTheme.Error)}");
            else
                Console.WriteLine($"{DateTime.Now.ToString(dateTimeFormat).Pastel(ColorTheme.OffsetColor)} {msg}");
        }





    }
    class AutoCompletionHandler : IAutoCompleteHandler
    {
        
        public char[] Separators { get; set; } = new char[] { ' ', ':', '#' };
        public string[] GetSuggestions(string text, int index)
        {
            string currentWord = text.Substring(index);

            if (text.StartsWith("git "))
                return (new string[] { "init", "clone", "pull", "push" }).Where(x => x.StartsWith(currentWord)).ToArray();
            else if (text.StartsWith("#"))
            {
                string word = text.Substring(1);
                string[] dirs = Globals.db.GetAllThemas();
                string[] suggestions = dirs.Where(x => x.StartsWith(word.Trim())).ToArray();
                return suggestions;
            }
            else
                return null;
        }
    }
}
