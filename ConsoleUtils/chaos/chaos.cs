using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pastel;

namespace chaos
{
    internal class chaos
    {
        static CmdParser cmd;
        static Encoding encoding = Encoding.UTF8;
        static Encoding defaultEncoding = Encoding.UTF8;

        static string color1 = "88d4ab";
        static string color2 = "56ab91";

        static void Main(string[] args)
        {


            cmd = new CmdParser(args)
            {
                { "help", "", CmdCommandTypes.FLAG, "Show this help." },
                { "file", "f", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "File to read" },
                { "encoding", "e", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, "utf8" }
                  },   $"Force encoding to {"ascii".Pastel(color2)}, {"utf8".Pastel(color2)} (default), {"utf16".Pastel(color2)}, {"utf7".Pastel(color2)}, {"utf32".Pastel(color2)}, {"utf16be".Pastel(color2)} or <{"int".Pastel(color2)}> as codepage" },
                { "uniq", "u", CmdCommandTypes.FLAG, false, "only uniq strings" },
                { "uniq-ignore-case", "U", CmdCommandTypes.FLAG, "only uniq strings (ignore case)" },
                { "sort", "s", CmdCommandTypes.FLAG, false, "sort" },
                { "sort-ignore-case", "S", CmdCommandTypes.FLAG, "sort and ignore case" },
                { "desc", "d", CmdCommandTypes.FLAG, "sort descendings" },
                { "to-lower", "l", CmdCommandTypes.FLAG, "output all lines lower case (after uniq & sort)" },
                { "to-upper", "L", CmdCommandTypes.FLAG, "output all lines uppter case (after uniq & sort)" },
                { "count", "c", CmdCommandTypes.FLAG, "count output lines" },

            };

            cmd.DefaultParameter = "file";
            cmd.Parse();

            // HELP
            if (cmd.HasFlag("help"))
            {
                ShowLongHelp();
            }

            string[] lines = new string[0];
            string[] result_lines = new string[0];

            if (cmd["encoding"].WasUserSet)
            {
                // "utf8" (default), "ascii", "utf7", "utf16", "utf16be","utf32", "utf32be"
                //encoding = EncodingHelper.GetEncodingFromName(cmd["encoding"].Strings[0]);
                string str_encoding = cmd["encoding"].String.Trim().ToLower();

                switch (str_encoding)
                {
                    case "ascii":
                        encoding = Encoding.ASCII;
                        break;
                    case "utf8":
                        encoding = Encoding.UTF8;
                        break;
                    case "utf16":
                        encoding = Encoding.Unicode;
                        break;
                    case "utf7":
                        encoding = Encoding.UTF7;
                        break;
                    case "utf32":
                        encoding = Encoding.UTF32;
                        break;
                    case "utf16be":
                        encoding = Encoding.BigEndianUnicode;
                        break;
                    default:
                        int codepage = 0;
                        if (int.TryParse(str_encoding, out codepage))
                        {
                            try
                            {
                                encoding = Encoding.GetEncoding(codepage);
                            }
                            catch
                            {
                                Die("Invalid codepage for " + "--encoding".Pastel("#a71e34") + ": \"" + str_encoding + "\"", 2);
                            }
                        }
                        else
                        {
                            Die("Unkown option for " + "--encoding".Pastel("#a71e34") + ": \"" + str_encoding + "\"", 2);
                        }
                        break;
                }
            }

            if (Console.IsInputRedirected)
            {
                Pastel.ConsoleExtensions.Disable();
                using (Stream s = Console.OpenStandardInput())
                {
                    using (MemoryStream reader = new MemoryStream())
                    {
                        s.CopyTo(reader);
                        lines = encoding.GetString(reader.ToArray()).Split('\n');
                    }
                }
            }
            else
            {
                if (cmd["file"].StringIsNotNull)
                {
                    string path = cmd["file"].Strings[0];

                    if (!cmd["encoding"].WasUserSet)
                    {
                        //encoding = EncodingHelper.GetEncodingFromFile(path);
                        encoding = defaultEncoding;
                        if (encoding != Encoding.ASCII && encoding != Encoding.UTF8)
                            Console.Error.WriteLine($"{"Warning:".Pastel(ColorTheme.OffsetColorHighlight)} files has {encoding.EncodingName} encoding.");
                    }

                    lines = encoding.GetString(File.ReadAllBytes(path)).Split(new string[] { "\r\n", "\n" },StringSplitOptions.None);
                    //lines = File.ReadAllText(path, encoding).Split('\n');
                }
                else
                {
                    ShowLongHelp();
                    // Exit
                }

            }
            result_lines = lines;



            if (cmd.HasFlag("uniq-ignore-case"))
            {
                result_lines = result_lines.Distinct(StringComparer.InvariantCultureIgnoreCase).ToArray();
            }
            else if (cmd.HasFlag("uniq"))
            {
                result_lines = result_lines.Distinct().ToArray();
            }
            if (cmd.HasFlag("sort-ignore-case"))
            {
                Array.Sort(result_lines, StringComparer.CurrentCultureIgnoreCase);
            }
            else if (cmd.HasFlag("sort"))
            {
                Array.Sort(result_lines);
            }

            // Array.Sort(result_lines, (x, y) => x.Length.CompareTo(y.Length)); // sort by length
            if (cmd.HasFlag("desc"))
                result_lines = result_lines.OrderByDescending(c => c).ToArray();

            if (cmd.HasFlag("count"))
            {
                Console.WriteLine(result_lines.Length);
                Exit(0);
            }


            foreach (string line in result_lines)
            {
                string output_line = line.TrimEnd('\r');
                if (cmd.HasFlag("to-lower"))
                    output_line = output_line.ToLower();
                if (cmd.HasFlag("to-upper"))
                    output_line = output_line.ToUpper();

                Console.WriteLine(output_line);
            }
        }

        string[][] SortTable(string[][] array, int index)
        {
            string[][] result = new string[0][];
            array.CopyTo(result, 0);
 
            Array.Sort(array, delegate (object[] x, object[] y){
                return (x[index] as IComparable).CompareTo(y[index]);
            });
            return array;
        }


        static void ShowHelp(bool more = true)
        {
            ShowVersion();
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName.Replace(".exe", "").Pastel(color1)} [{"Options".Pastel(color2)}] \"{"file".Pastel(color2)}\"");
            if (more)
                Console.WriteLine($"For more options, use {"--help".Pastel(color1)}");
        }
        static void ShowLongHelp()
        {
            ShowHelp(false);
            //Console.WriteLine($"gremlins, {ConsoleHelper.GetVersionString()}");
            Console.WriteLine($"\n{"Options".Pastel(color2)}:");
            foreach (CmdOption c in cmd.OrderBy(x => x.Name))
            {
                string l = $"  --{c.Name}".Pastel(color1) + (!string.IsNullOrEmpty(c.ShortName) ? $", {("-" + c.ShortName).Pastel(color1)}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel(color2)).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
            //WriteError("Usage: subnet [ip/cidr|ip/mask|ip number_of_hosts]");
            Exit(0);
        }

        static void ShowVersion()
        {
            Console.WriteLine(@"▄█▄     ▄  █ ██   ████▄    ▄▄▄▄▄   ".Pastel("#99e2b4"));
            Console.WriteLine(@"█▀ ▀▄  █   █ █ █  █   █   █     ▀▄ ".Pastel("#88d4ab"));
            Console.WriteLine(@"█   ▀  ██▀▀█ █▄▄█ █   █ ▄  ▀▀▀▀▄   ".Pastel("#78c6a3"));
            Console.WriteLine(@"█▄  ▄▀ █   █ █  █ ▀████  ▀▄▄▄▄▀    ".Pastel("#67b99a"));
            Console.WriteLine(@"▀███▀     █     █ ".Pastel("#56ab91") + ("v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()).Pastel("#358f80"));
            Console.WriteLine(@"         ▀     █                   ".Pastel("#469d89"));
            Console.WriteLine(@"              ▀                    ".Pastel("#358f80"));
            Console.WriteLine($"{"chaos".Pastel(color1)} is part of " + ConsoleHelper.GetVersionString(color2, color2));
        }

        static void Exit(int exitCode)
        {
            string parrentProcess = ConsoleUtilsCore.ParentProcessUtilities.GetParentProcess().ProcessName;
            //Console.WriteLine(parrentProcess);

            if (System.Diagnostics.Debugger.IsAttached || parrentProcess.ToLower().Contains("explorer")) // is debugger attached or started by double-click/file-drag
            {
                Console.WriteLine("\nPress any key to exit.");
                Console.ReadKey();
            }

            Environment.Exit(exitCode);
        }

        public static void Die(string msg, int errorcode)
        {
            ConsoleHelper.WriteError(msg);
            Environment.Exit(errorcode);
        }
    }
}
