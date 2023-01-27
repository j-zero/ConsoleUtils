using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pastel;

namespace klemmbrett
{
    internal class Program
    {
        static CmdParser cmd;
        static void Main(string[] args)
        {

            cmd = new CmdParser(args)
            {
                {"show","s", CmdCommandTypes.VERB, "Show information" },
                {"list","l", CmdCommandTypes.VERB, "List formats" },
                { "format", "f", CmdCommandTypes.PARAMETER,
                    new CmdParameters() {
                        { CmdParameterTypes.INT, 1},
                    },
                    "Get clipboard data in format"
                },
                { "formatstring", "F", CmdCommandTypes.PARAMETER,
                    new CmdParameters() {
                        { CmdParameterTypes.STRING, "CF_TEXT"}, 
                    },
                    "Get clipboard data in format"
                },
                {"bin","b", CmdCommandTypes.FLAG, "bin dump" },
                {"hex","h", CmdCommandTypes.FLAG, "hex dump" },
                {"ascii","a", CmdCommandTypes.FLAG, "ascii dump" },
                {"unicode","U", CmdCommandTypes.FLAG, "unicode dump" },
                {"utf8","u", CmdCommandTypes.FLAG, "utf8 dump" }
            };

            cmd.DefaultVerb = "list";
            cmd.Parse();

            foreach(string s in cmd.Verbs)
            {
                //Console.WriteLine(s);
            }

            //ShowHelp();
            if (cmd.FirstVerb == "list")
            {
                ClipboardHelper.ListClipboardFormats();
            }
            else if (cmd.FirstVerb == "show")
            {
                uint format = (uint)(cmd["format"].Int);
                byte[] b = ClipboardHelper.GetClipboardDataBytes(format);
                if (cmd.HasFlag("bin"))
                    ConsoleHelper.BinDump(b);
                else if (cmd.HasFlag("hex"))
                    ConsoleHelper.SimpleHexDump(b);
                else if (cmd.HasFlag("ascii"))
                    Console.WriteLine(Encoding.ASCII.GetString(b));
                else if (cmd.HasFlag("unicode"))
                    Console.WriteLine(Encoding.Unicode.GetString(b));
                else if (cmd.HasFlag("utf8"))
                    Console.WriteLine(Encoding.UTF8.GetString(b));
            }
            ;
        }
        static void ShowHelp()
        {
            Console.WriteLine($"klemmbrett, {ConsoleHelper.GetVersionString()}");
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [Verb] [Options]");

            Console.WriteLine($"\nVerbs:");
            foreach (CmdOption c in cmd.SelectVerbs.OrderBy(x => x.Name))
            {
                string l = $"  {c.Name}".Pastel("9CDCFE") + (!string.IsNullOrEmpty(c.ShortName) ? $", {("" + c.ShortName).Pastel("9CDCFE")}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel("569CD6")).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }

            Console.WriteLine($"\nOptions:");
            foreach (CmdOption c in cmd.SelectOptions.OrderBy(x => x.Name))
            {
                string l = $"  --{c.Name}".Pastel("9CDCFE") + (!string.IsNullOrEmpty(c.ShortName) ? $", {("-" + c.ShortName).Pastel("9CDCFE")}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel("569CD6")).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
            Environment.Exit(0);
        }
    }
}
