﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pastel;
using System.Drawing;
using System.Text.RegularExpressions;

namespace gremlins
{
    internal class Program
    {
        static CmdParser cmd;
        static int defaultLineCount = 10;

        static char nonPrintableChar = '·';

        static void Main(string[] args)
        {
            cmd = new CmdParser(args)
            { 

                { "cut", "c", CmdCommandTypes.PARAMETER, 
                    new CmdParameters() {
                        { CmdParameterTypes.INT, 0},
                        { CmdParameterTypes.INT, 0},
                    }, 
                    "Look from here to there." 
                },

                { "head", "h", CmdCommandTypes.FLAG, $"Show first X lines, can be modified with --lines."},
                { "tail", "t", CmdCommandTypes.FLAG, $"Show first X lines, can be modified with --lines."},

                { "help", "", CmdCommandTypes.FLAG, "Show this help." },

                { "lines", "n", CmdCommandTypes.PARAMETER,
                    new CmdParameters() {
                        { CmdParameterTypes.INT, defaultLineCount},
                    },
                    $"Number of lines for head/tail. Default: {defaultLineCount}"
                },

                
                { "only-non-ascii", "", CmdCommandTypes.FLAG, "Everything above 0xff is gremlin" },
                { "empty-lines", "", CmdCommandTypes.FLAG, "Parse empty lines as gremlins" },
                { "no-space-on-line-end", "", CmdCommandTypes.FLAG, "Parse spaces on line end not as gremlin" },

                { "all", "a", CmdCommandTypes.FLAG, "Show all lines" },
                { "invert", "v", CmdCommandTypes.FLAG, "Show all lines but no gremlins" },

                { "no-cr", "", CmdCommandTypes.FLAG, "Don't show carrige return" },
                { "no-space", "", CmdCommandTypes.FLAG, "Don't mark space" },
                { "no-tab", "", CmdCommandTypes.FLAG, "Don't mark space" },
                { "no-colors", "", CmdCommandTypes.FLAG, "Don't color output" },
                { "no-line-numbers", "l", CmdCommandTypes.FLAG, "Don't show line numbers" },

                { "plain", "p", CmdCommandTypes.FLAG, "Combines --no-cr, --no-space, --no-tab, --no-line-numbers, --no-colors" },

                { "regex", "r", CmdCommandTypes.MULTIPE_PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Additional regex for gremlin detection" }, 

                { "file", "f", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "File to read" }

                
            };
   
            cmd.DefaultParameter = "file";
            try
            {
                cmd.Parse();

                // HELP
                if (cmd.HasFlag("help"))
                {
                    ShowHelp();
                }

                if (cmd.HasFlag("no-colors") || cmd.HasFlag("plain"))
                    Pastel.ConsoleExtensions.Disable();

                string[] lines = new string[0];

                long lineCount = cmd["lines"].Longs[0];

                long offset = cmd["cut"].Longs[0];
                long length = cmd["cut"].Longs[1];

                if (cmd.HasFlag("tail"))
                {
                    offset = lineCount * -1;
                    length = lineCount;
                }
                else if (cmd.HasFlag("head"))
                {
                    offset = 0;
                    length = lineCount;
                }


                if (Console.IsInputRedirected)
                {
                    Pastel.ConsoleExtensions.Disable();
                    using (Stream s = Console.OpenStandardInput())
                    {
                        using (StreamReader reader = new StreamReader(s))
                        {
                            lines = reader.ReadToEnd().Split('\n');
                        }
                    }
                }
                else
                {
                    if (cmd["file"].Strings.Length > 0 && cmd["file"].Strings[0] != null)
                    {
                        string path = cmd["file"].Strings[0];
                        lines = File.ReadAllText(path).Split('\n');
                    }
                    else
                    {
                        ShowHelp(); 
                        // Exit
                    }

                }

                GremlinDump(lines, (int)offset, (int)length);
            }
            catch (FileNotFoundException ex)
            {
                ConsoleHelper.WriteError(ex.Message);
            }
            catch (ArgumentException ex)
            {
                ConsoleHelper.WriteError(ex.Message);
                Environment.Exit(255);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Environment.Exit(255);
            }
            if (System.Diagnostics.Debugger.IsAttached)
                Console.ReadLine();
        }

        public static void GremlinDump(string[] lines, int start = 0, int length = 0)
        {
            if (lines.Length == 0) return;
            //string[] lines = Encoding.UTF8.GetString(bytes).Split('\n');
            int lineNumberLength = (int)Math.Log10(lines.Length) + 1;
            //.PadLeft(offsetLength, '0')
            int offset = start;
            bool utf8Gremlin = cmd.HasFlag("only-non-ascii");

            if (offset < 0)
            {
                if (lines.Length < (start * -1))
                    offset = 0;
                else
                    offset = lines.Length + offset;
            }

            int lineNumber = offset;

            int lastLine = offset + length;

            if (lastLine > lines.Length || lastLine == 0)
                lastLine = lines.Length;

            for (int l = offset; l < lastLine; l++)
            {
                string newLine = "";
               
                bool lineEndsWithSpace = lines[l].EndsWith("\t\r") || lines[l].EndsWith(" \r") || lines[l].EndsWith(" ") || lines[l].EndsWith("\t");

                bool isGremlin = (!cmd.HasFlag("no-space-on-line-end") && lineEndsWithSpace) || cmd.HasFlag("empty-lines") && (string.IsNullOrEmpty(lines[l]) || Regex.IsMatch(lines[l], @"^\s*$"));
                bool isCustomGremlin = false;

                //if ((l == lastLine - 1) && (lines[l] == string.Empty))
                //    continue;

                lineNumber++;

                //string l = line.Replace("\r", "\\r".Pastel("80ff80"));



                foreach (char c in lines[l])
                {
                    int i = (int)c;
                    string color = ColorTheme.GetColor(i, true);
                    //color = "9CDCFE";

                    if (!isGremlin)
                    {
                        if(utf8Gremlin)
                        //                                                LF           CR           TAB
                            isGremlin = (i < 32 || i > 0xff) && (i != 0x0a && i != 0x0d && i != 0x09);
                        else
                            isGremlin = (i < 32 || i > 0x7f) && (i != 0x0a && i != 0x0d && i != 0x09);

                    }
                    if (i == 0x0d)    // CR
                        newLine += ((cmd.HasFlag("no-cr") || cmd.HasFlag("plain") ? "\r" : "¬").Pastel(ColorTheme.DarkColor));
                    else if (i == 0x09)    // Tab
                        newLine += ((cmd.HasFlag("no-tab") || cmd.HasFlag("plain") ? "\t" : $"\\t{nonPrintableChar}{nonPrintableChar}").Pastel(ColorTheme.DarkColor));
                    else if (i == 0x20)    // Space
                        newLine += ((cmd.HasFlag("no-space") || cmd.HasFlag("plain") ? " " : "_").Pastel(ColorTheme.DarkColor));
                    else if (i > 255)    // UTF-8
                        newLine += ("\\x" + i.ToString("X").ToLower()).Pastel(ColorTheme.HighLight2);
                    else if (i < 32)    // UTF-8
                        newLine += ("\\x" + i.ToString("X").ToLower()).Pastel(ColorTheme.HighLight2);
                    else
                        newLine += ($"{c}".Pastel(color));

                }

                if (cmd.Exists("regex"))
                {   /* Doesn't work because of coloring every character before
                    Regex r = new Regex(cmd["regex"].Strings[0]);
                    isCustomGremlin = r.IsMatch(lines[l]);

                    
                    foreach (Match match in r.Matches(lines[l]))
                    {
                        newLine = lines[l].Replace(match.Value, match.Value.Pastel(ColorTheme.HighLight2));
                    }
                    */
                    foreach(string pattern in cmd["regex"].Strings)
                        isCustomGremlin |= Regex.IsMatch(lines[l], pattern);
                    if (!isGremlin)
                        isGremlin = isCustomGremlin;

                }

                if ((!cmd.HasFlag("invert") && isGremlin) ^ (cmd.HasFlag("invert") && !isGremlin) || cmd.HasFlag("all"))
                {
                    string lineColor = isGremlin ? ColorTheme.OffsetColorHighlight : ColorTheme.OffsetColor;
                    if (isCustomGremlin)
                        lineColor = ColorTheme.HighLight2;
                    string strLineNumber = lineNumber.ToString().PadLeft(lineNumberLength, '0').Pastel(lineColor);
                    Console.Write(
                        (cmd.HasFlag("no-line-numbers") || cmd.HasFlag("plain") ? "" : $"{strLineNumber}: ") + $"{newLine}\n"
                       );
                }
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine($"gremlins, {ConsoleHelper.GetVersionString()}");
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [Options] {{[--file|-f] file}}");
            Console.WriteLine($"Options:");
            foreach (CmdOption c in cmd.OrderBy(x => x.Name))
            {
                string l = $"  --{c.Name}".Pastel("9CDCFE") + (!string.IsNullOrEmpty(c.ShortName) ? $", {("-" + c.ShortName).Pastel("9CDCFE")}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel("569CD6")).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
            //WriteError("Usage: subnet [ip/cidr|ip/mask|ip number_of_hosts]");
            Environment.Exit(0);
        }
    }
}
