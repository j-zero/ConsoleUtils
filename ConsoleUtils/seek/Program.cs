using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pastel;

namespace seek
{
    internal class Program
    {
        static bool debug = false;
        static string color1 = "#fff75e";
        static string color2 = "#ffe246";
        static CmdParser cmd;

        static void Main(string[] args)
        {
            cmd = new CmdParser(args)
            {
                { "help", "", CmdCommandTypes.FLAG, "Show this help." },
                { "recursive", "r", CmdCommandTypes.FLAG, "Seek recursive." },
                { "pattern", "", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Pattern to find" },
                { "directory", "d", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, Environment.CurrentDirectory }
                    }, "Directory to seek in" },
            };

            cmd.DefaultParameter = "pattern";

            try
            {
                cmd.Parse();
                
                if (cmd.HasFlag("help"))
                {
                    ShowHelp();
                }

                if (cmd["pattern"].StringIsNotNull)
                {
                    int foundFilesCount = Seek(cmd["pattern"].String, cmd["directory"].String, cmd.HasFlag("recursive"));
                    if (foundFilesCount < 1)
                        ConsoleHelper.WriteError($"No files found matching \"{cmd["pattern"].String.Pastel(ColorTheme.Text)}\"");
                }
                else
                {
                    ShowHelp();
                }
            }
            catch {

            }
        }

        static int Seek(string needle, string haystack, bool recursive = false)
        {
            var connection = new OleDbConnection(@"Provider=Search.CollatorDSO;Extended Properties=""Application=Windows""");

            needle = needle.Replace("*", "%");
            string scope = recursive ? "scope" : "directory";


            DebugWrite($"seeking {needle}...\n");

            // File name search (case insensitive), also searches sub directories
            var query1 = @"SELECT System.ItemPathDisplay FROM SystemIndex WHERE " + scope + " = 'file:" + haystack + "\\' AND System.ItemName LIKE '" + needle + "'";
            DebugWrite(query1);

            /*
            // File name search (case insensitive), does not search sub directories
            var query2 = @"SELECT System.ItemPathDisplay FROM SystemIndex WHERE directory = 'file:C:/' AND System.ItemName LIKE '%Test%' ";

            // Folder name search (case insensitive)
            var query3 = @"SELECT System.ItemPathDisplay FROM SystemIndex " +
                        @"WHERE scope = 'file:C:/' AND System.ItemType = 'Directory' AND System.Itemname LIKE '%Test%' ";

            // Folder name search (case insensitive), does not search sub directories
            var query4 = @"SELECT System.ItemPathDisplay FROM SystemIndex " +
                        @"WHERE directory = 'file:C:/' AND System.ItemType = 'Directory' AND System.Itemname LIKE '%Test%' ";
            */

            connection.Open();

            var command = new OleDbCommand(query1, connection);
            int counter = 0;
            using (var r = command.ExecuteReader())
            {
                while (r.Read())
                {
                    counter++;
                    Console.WriteLine(r[0]);
                }
            }

            connection.Close();
            return counter;
        }

        static void DebugWrite(string msg)
        {
            if (debug)
                Console.WriteLine($"[{"D".Pastel(ColorTheme.HighLight1)}]: " + msg);
        }

        static void ShowHelp()
        {
            ShowVersion();
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName.Pastel(color1)} [{"Options".Pastel(color2)}] {{\"file\"|{"-i".Pastel(color2)} \"input string\"}}");
            Console.WriteLine($"\n{"Options".Pastel(color2)}:");
            foreach (CmdOption c in cmd.OrderBy(x => x.Name))
            {
                string l = $"  --{c.Name}".Pastel(color1) + (!string.IsNullOrEmpty(c.ShortName) ? $", {("-" + c.ShortName).Pastel(color1)}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel(color2)).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
            Environment.Exit(0);
        }

        static void ShowVersion()
        {
            string version_string = ("v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " ").PadLeft(28).Pastel("#ffd53e");
            Console.WriteLine(@"   ▄▄▄▄▄   ▄███▄   ▄███▄   █  █▀ ".Pastel("#fff75e"));
            Console.WriteLine(@"  █     ▀▄ █▀   ▀  █▀   ▀  █▄█   ".Pastel("#fff056"));
            Console.WriteLine(@"▄  ▀▀▀▀▄   ██▄▄    ██▄▄    █▀▄   ".Pastel("#ffe94e"));
            Console.WriteLine(@" ▀▄▄▄▄▀    █▄   ▄▀ █▄   ▄▀ █  █  ".Pastel("#ffe246"));
            Console.WriteLine(@"           ▀███▀   ▀███▀     █   ".Pastel("#ffda3d"));
            Console.WriteLine(version_string +            @"▀    ".Pastel("#ffd53e"));
            Console.WriteLine("seek is part of " + ConsoleHelper.GetVersionString());
        }
    }
}
