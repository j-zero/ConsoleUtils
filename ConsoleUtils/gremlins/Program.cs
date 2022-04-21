using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pastel;
using System.Drawing;

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
            { // Todo: is default[verb|parameter]

                { "cut", "c", CmdCommandTypes.PARAMETER, 
                    new CmdParameters() {
                        { CmdParameterTypes.INT, 0},
                        { CmdParameterTypes.INT, 0},
                    }, 
                    "Cut from here to there" 
                },

                { "head", "h", CmdCommandTypes.FLAG, 
                    $"Show first {defaultLineCount} lines"
                },

                { "tail", "t", CmdCommandTypes.FLAG,
                    $"Show first {defaultLineCount} lines"
                },

                { "lines", "n", CmdCommandTypes.PARAMETER,
                    new CmdParameters() {
                        { CmdParameterTypes.INT, defaultLineCount},
                    },
                    $"Number of lines for head/tail. Default: {defaultLineCount}"
                },

                { "no-line-numbers", "l", CmdCommandTypes.FLAG, "No line numbers" },
                { "non-ascii-gremlins", "", CmdCommandTypes.FLAG, "Everythin above 0xff is gremlin" },

                { "all", "a", CmdCommandTypes.FLAG, "show all lines" },

                { "no-cr", "", CmdCommandTypes.FLAG, "don't show carrige return" },
                { "no-space", "", CmdCommandTypes.FLAG, "don't mark space" },
                { "no-tab", "", CmdCommandTypes.FLAG, "don't mark space" },

                { "file", "f", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, null }
                }, "File to read" }

            };

            cmd.DefaultParameter = "file";
            cmd.Parse();

            string[] lines = new string[0];

            long lineCount = cmd["lines"].Longs[0];

            long offset = cmd["cut"].Longs[0];
            long length = cmd["cut"].Longs[1];

            if (cmd.HasFlag("tail"))
            {
                offset = lineCount * -1;
                length = lineCount;
            }
            else if (cmd.HasFlag("head")){
                offset = 0;
                length = lineCount;
            }


            if (Console.IsInputRedirected)
            {
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
                    try
                    {
                        lines = File.ReadAllText(path).Split('\n');
                    }
                    catch(Exception ex)
                    {
                        ConsoleHelper.WriteError(ex.Message);
                    }
                }
            }

            GremlinDump(lines, (int)offset, (int)length);
        }

        public static void GremlinDump(string[] lines, int start = 0, int length = 0)
        {
            if (lines.Length == 0) return;
            //string[] lines = Encoding.UTF8.GetString(bytes).Split('\n');
            int lineNumberLength = (int)Math.Log10(lines.Length) + 1;
            //.PadLeft(offsetLength, '0')
            int offset = start;
            bool utf8Gremlin = cmd.HasFlag("non-ascii-gremlins");
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

                bool isGremlin = lineEndsWithSpace;

                //if ((l == lastLine - 1) && (lines[l] == string.Empty))
                //    continue;

                lineNumber++;
                
                //string l = line.Replace("\r", "\\r".Pastel("80ff80"));

                foreach (char c in lines[l])
                {
                    int i = (int)c;
                    string color = ColorTheme.GetColor(i, true);
                    //color = "9CDCFE";

                    if (isGremlin == false)                                       //LF        //CR          //TAB
                        isGremlin = (i < 32 || (utf8Gremlin || i > 127)) && (i != 0x0a && i != 0x0d && i != 0x09);

                    if (i == 0x0d)    // CR
                        newLine += ((cmd.HasFlag("no-cr") ? "\r" : "¬").Pastel("606060"));
                    else if (i == 0x09)    // Tab
                        newLine += ((cmd.HasFlag("no-tab") ? "\t" : $"\\t{nonPrintableChar}{nonPrintableChar}").Pastel("606060"));
                    else if (i == 0x20)    // Space
                        newLine += ((cmd.HasFlag("no-space") ? " " : "_").Pastel("606060"));
                    else if (i > 255)    // UTF-8
                        newLine += ("\\x" + i.ToString("X").ToLower()).Pastel("E17B7C");
                    else if (i < 32)    // UTF-8
                        newLine += ("\\x" + i.ToString("X").ToLower()).Pastel("E17B7C");
                    else
                        newLine += ($"{c}".Pastel(color));

                }
                if ((!cmd.HasFlag("all") && isGremlin) || cmd.HasFlag("all"))
                {
                    string strLineNumber = lineNumber.ToString().PadLeft(lineNumberLength, '0').Pastel(isGremlin ? "ffff80" : "eeeeee");
                    Console.Write((!cmd.HasFlag("no-line-numbers") ? $"{strLineNumber}: " : "") + $"{newLine}\n");
                }
            }
        }
    }
}
