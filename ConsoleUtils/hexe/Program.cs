using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pastel;
using System.Drawing;
using NtfsDataStreams;
using ConsoleUtilsCore;

namespace hexe
{
    // TODO cut to binary, patch, remove, skip
    internal class Program
    { 
        static int firstHexColumn = 12; // 8 characters for the address +  3 spaces
        static int firstCharColumn = 0;
        static int lineLength = 0;
        static int dynamicSteps = 8;
        static int bytesPerLine = 16;

        static char nonPrintableChar = '·';
        static char spaceChar = ' ';

        static char[] HexChars = "0123456789abcdef".ToCharArray();
        static bool noOffset = false;
        static bool noText = false;
        //static long startOffset = 0;
        static int defaultLength = 256;

        static CmdParser cmd;

        static string output_file = null;

        static int windowWidth = 120;
        static int windowHeight = 80;

        static Encoding encoding = Encoding.UTF8;

        static ConsoleHelper.OutputMode outputMode = ConsoleHelper.OutputMode.Hex;

        static string color1 = "#e01e37";
        static string color2 = "#a71e34";


        static void Main(string[] args)
        {

            cmd = new CmdParser(args)
            { // Todo: is default[verb|parameter]
                { "help", "", CmdCommandTypes.FLAG, "Show this help." },
                { "version", "V", CmdCommandTypes.FLAG, "Shows the version." },

                //{ "show", null, CmdCommandTypes.VERB, $"Show complete file. Default." },
                //{ "find", null, CmdCommandTypes.VERB, $"Find byte pattern in complete file" },

                { "short", "s", CmdCommandTypes.FLAG, $"Show head and tail" },

                { "bin", "b", CmdCommandTypes.FLAG, "Binary mode" },
                { "string", "S", CmdCommandTypes.FLAG, "String mode" },
                { "array", "", CmdCommandTypes.FLAG, "Output C/C++ array" },

                { "cut", "c", CmdCommandTypes.MULTIPE_PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.INT, 0},
                    { CmdParameterTypes.INT, defaultLength},
                }, "Cut offset to offset" },

                { "skip", "", CmdCommandTypes.MULTIPE_PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.INT, 0},
                    { CmdParameterTypes.INT, 1},
                }, $"Skip from offset <{"int".Pastel(color2)}>, count <{"int".Pastel(color2)}>" },

                { "head", "h", CmdCommandTypes.FLAG, $"Show first X bytes, can be modified with {"--count".Pastel(color1)}."},
                { "tail", "t", CmdCommandTypes.FLAG, $"Show last X bytes, can be modified with {"--count".Pastel(color1)}."},

                { "debug", "D", CmdCommandTypes.FLAG, "Debug mode" },
                { "no-header", "", CmdCommandTypes.FLAG, "Disable header" },
                { "zero", "z", CmdCommandTypes.FLAG, "Set offset to zero on cut" },
                { "no-offset", "", CmdCommandTypes.FLAG, "Show no offset" },
                { "no-ascii", "", CmdCommandTypes.FLAG, "Show no ascii" },
                { "no-cr", "", CmdCommandTypes.FLAG, "Don't show carrige return" },
                { "convert-space", "", CmdCommandTypes.FLAG, "Mark space in string mode" },
                //{ "no-tab", "", CmdCommandTypes.FLAG, "Don't mark space" },
                { "no-colors", "", CmdCommandTypes.FLAG, "Don't color output" },
                { "no-line-numbers", "l", CmdCommandTypes.FLAG, "Don't show line numbers" },
                { "convert-hex", "", CmdCommandTypes.FLAG, "Show unprintable chars as hex values in string mode" },

                { "plain", "p", CmdCommandTypes.FLAG, $"Combines {"--no-cr".Pastel(color1)},{"--no-space".Pastel(color1)}, {"--no-line-numbers".Pastel(color1)}, {"--no-colors".Pastel(color1)}" },

                { "dump", "d", CmdCommandTypes.FLAG, $"Dump binary to the file specified by {"--output".Pastel(color1)}" },

                { "count", "n", CmdCommandTypes.PARAMETER,
                    new CmdParameters() {
                        { CmdParameterTypes.INT, 0},
                    },
                    $"Number of bytes to show"
                },
                { "offset", "o", CmdCommandTypes.PARAMETER,
                    new CmdParameters() {
                        { CmdParameterTypes.INT, 0},
                    },
                    $"Offset to start with"
                },

                { "bytes-per-line", "L", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.INT, 16 }
                }, "Bytes per line" },

                { "file", "f", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, null } 
                }, "Input file" },

                { "input-hex-string", "H", CmdCommandTypes.FLAG, "Sets input mode to hexadecimal string" },

                { "input", "i", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, null }
                }, "Input string" },

                { "find", "F", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, null }
                }, "Find hex pattern" },

                { "find-string", "", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, null }
                }, "Find string" },

                { "byte-mode", "", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, "hex" }
                }, $"View bytes as: {"hex".Pastel(color2)} (default), {"dec".Pastel(color2)}, {"oct".Pastel(color2)} or {"bin".Pastel(color2)}" },

                { "encoding", "", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, "utf8" }
                }, $"Sets string encoding to {"ascii".Pastel(color2)}, {"utf8".Pastel(color2)} (default), {"utf16".Pastel(color2)}, {"utf7".Pastel(color2)}, {"utf32".Pastel(color2)}, {"utf16be".Pastel(color2)} or <{"int".Pastel(color2)}> as codepage" },
                //}, $"Sets string encoding to: ascii, utf8 (default), utf16, utf7, utf32, utf16be or <{"int".Pastel(color2)}> as codepage" },
  
                { "output", "O", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Output file" },

            };

            cmd.DefaultParameter = "file";

            try
            {
                cmd.Parse();

                if (cmd.HasFlag("help"))
                    ShowLongHelp();
                if (cmd.HasFlag("version"))
                {
                    ShowVersion();
                    Environment.Exit(0);
                }
                

                noText = cmd.HasFlag("no-ascii");
                noOffset = cmd.HasFlag("no-offset");

                bytesPerLine = (int)cmd["bytes-per-line"].Longs[0];

                if (cmd.HasFlag("no-colors") || cmd.HasFlag("plain") || cmd.HasFlag("output") || Console.IsOutputRedirected)
                    Pastel.ConsoleExtensions.Disable();

                List<Blob> data = new List<Blob>();
                
                List<Selection> parts = new List<Selection>();

                parts.Add(new Selection(0, 0));

                if (noOffset)
                    firstHexColumn = 0;

                parts[0].Offset = (int)cmd["offset"].Long;
                parts[0].Length = (int)cmd["count"].Long;

                if (cmd["cut"].WasUserSet)
                {
                    int k = 0;
                    int i = 0;
                    while(i < cmd["cut"].Longs.Length)
                    {
                        if (parts.Count <= k)
                            parts.Add(new Selection(0, 0));

                        var offset = cmd["cut"].Ints[i++];
                        var end = cmd["cut"].Ints[i++];

                        parts[k].Offset = offset;
                        parts[k].End = end + 1;
                        k++;
                    }
                }

                else if (cmd.HasFlag("tail"))
                {
                    parts[0].Offset = (cmd["count"].WasUserSet ? parts[0].Length : defaultLength) * -1;
                    parts[0].Length = defaultLength;
                }
                else if (cmd.HasFlag("head"))
                {
                    parts[0].Offset = 0;
                    parts[0].Length = cmd["count"].WasUserSet ? parts[0].Length : defaultLength;
                }

                output_file = cmd["output"].String;

                if (!Console.IsOutputRedirected)
                {
                    windowHeight = Console.WindowHeight;
                    windowWidth = Console.WindowWidth;
                }

                if (cmd["encoding"].WasUserSet)
                {
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


                if (cmd["byte-mode"].WasUserSet)
                {
                    string str_outputmode = cmd["byte-mode"].String.Trim().ToLower();

                    switch (str_outputmode)
                    {
                        case "dec":
                            outputMode = ConsoleHelper.OutputMode.Dec;
                            break;
                        case "bin":
                            outputMode = ConsoleHelper.OutputMode.Bin;
                            if (!cmd["cut"].WasUserSet)
                                bytesPerLine = 8;
                            break;
                        case "oct":
                            outputMode = ConsoleHelper.OutputMode.Oct;
                            break;
                        case "hex":
                            outputMode = ConsoleHelper.OutputMode.Hex;
                            break;
                        case "char":
                            outputMode = ConsoleHelper.OutputMode.Char;
                            break;
                        default:
                            Die("Unkown option for " + "--byte-mode".Pastel("#a71e34") + ": \"" + str_outputmode + "\"", 2);
                            break;
                    }

                }


                // TODO: alle offsets vorher speichern, daten je nach input-stream einlesen und dann im loop ausgeben

                if (cmd.HasFlag("short"))
                {
                    int cHeight = (windowHeight / 2) - 2;
                    defaultLength = cHeight * bytesPerLine;
                    //Console.WriteLine(cHeight);

                    //head
                    parts[0].Offset = 0;
                    parts[0].Length = cmd["count"].WasUserSet ? parts[0].Length : defaultLength;

                    parts.Add(new Selection(0, 0));
                    //tail
                    parts[1].Offset = (cmd["count"].WasUserSet ? parts[0].Length : defaultLength) * -1;
                    parts[1].Length = defaultLength;
                }


                // Read Data
                if (Console.IsInputRedirected)
                {
                    using (Stream s = Console.OpenStandardInput())
                    {
                        byte[] allData = ReadByteStream(s);
                        foreach (Selection p in parts)
                        {
                            if (p.Length == 0)
                                p.Length = allData.Length - p.Offset;
                            Blob blob = new Blob(p.Offset, new byte[p.Length]);
                            Buffer.BlockCopy(allData, p.Offset, blob.Data, 0, p.Length);
                            data.Add(blob);
                        }
                    }
                }
                else if (cmd["file"].Strings.Length > 0 && cmd["file"].Strings[0] != null)
                {
                    string path = cmd["file"].Strings[0];
                    if (File.Exists(path))
                    {
                        foreach (Selection s in parts)
                        {
                            Blob b = ReadFile(path, s.Offset, s.Length);
                            if (b != null)
                                data.Add(b); // needs to skip
                        }
                    }
                    else
                        throw new Exception($"File \"{path}\" not found!");

                }
                else if (cmd["input"].StringIsNotNull)
                {
                    byte[] allData;
                    if(cmd.HasFlag("input-hex-string"))
                        allData = ConvertHelper.HexStringToByteArray(cmd["input"].Strings[0]);
                    else
                        allData = encoding.GetBytes(cmd["input"].Strings[0]);

                    foreach (Selection p in parts)
                    {
                        if (p.Length == 0)
                            p.Length = allData.Length - p.Offset;
                        Blob blob = new Blob(p.Offset, new byte[p.Length]);
                        Buffer.BlockCopy(allData, p.Offset, blob.Data, 0, p.Length);
                        data.Add(blob);
                    }
                }
                else
                {
                    ShowHelp();
                }


                // output data
                for(int i = 0; i < data.Count; i++)
                {
                    if (cmd.HasFlag("bin"))
                    {
                        BinDump(data[i].Data);
                    }
                    else if (cmd.HasFlag("string"))
                    {
                        StringDump(data[i].Data, encoding);
                    }
                    else if (cmd.HasFlag("array"))
                    {
                        ArrayDump(data[i].Data, bytesPerLine, outputMode);
                    }
                    else if (cmd.HasFlag("dump"))
                    {
                        if (Console.IsOutputRedirected || output_file == null || output_file == "-")
                        {
                            Console.WriteLine(encoding.GetString(data[i].Data));
                        }
                        else
                        {
                            if (output_file == null)
                                ConsoleHelper.WriteError("No output file given");
                            FileMode mode = FileMode.Create;

                            if (i > 0)
                                mode = FileMode.Append;

                            WriteAllBytes(output_file, data[i].Data, mode);
                        }
                    }
                    else if (cmd["find"].WasUserSet || cmd["find-string"].WasUserSet)
                    {
                        List<Blob> foundData = new List<Blob>();
                        //string hexString = cmd["find"].String;
                        byte[] needle = null;

                        if (cmd["find"].WasUserSet)
                            needle = ConvertHelper.HexStringToByteArray(cmd["find"].String);
                        else if (cmd["find-string"].WasUserSet)
                            needle = encoding.GetBytes(cmd["find-string"].String);

                        
                        int offset = 0;
                        int counter = 0;
                        while (offset != -1 && data[i].Data.Length > (offset+1)) {
                            offset = Find(needle, data[i].Data, offset+1);
                            if(offset != -1)
                            {
                                int offset1 = offset - (offset % bytesPerLine);
                                int offset2 = (offset + needle.Length + bytesPerLine);
                                int offset3 = offset2 - (offset2 % bytesPerLine);

                                int size = offset3 - offset1;

                                Blob blob = new Blob(offset1, new byte[size]);
                                Buffer.BlockCopy(data[i].Data, (int)offset1, blob.Data, 0, size);

                                if(counter != 0)
                                    WriteLine("...".Pastel(ColorTheme.HighLight2));

                                ConsoleHelper.HexDump(blob, bytesPerLine, counter++ == 0, (ulong)(data.Last().Offset + data.Last().Length), false, offset, needle.Length, outputMode, noOffset, noText);
                                //ConsoleHelper.HexDump(data[i], bytesPerLine, !cmd.HasFlag("no-header") && (i != 1), (ulong)(data.Last().Offset + data.Last().Length), (cmd.HasFlag("zero")) && (data.Count > 1), -1, -1, outputMode, noOffset, noText);
                            }
                                    

                        }

                        
                    }

                    else
                    {
                        ConsoleHelper.HexDump(data[i], bytesPerLine, !cmd.HasFlag("no-header") && (i != 1), (ulong)(data.Last().Offset + data.Last().Length), (cmd.HasFlag("zero")) && (data.Count > 1), -1, -1, outputMode, noOffset, noText);
                    }
                    if(i != data.Count - 1)
                        WriteLine("...".Pastel(ColorTheme.HighLight2));
                }
                
                        

            }
            catch (ArgumentException ex)
            {
                ConsoleHelper.WriteError(ex.Message);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError(ex.Message);
                if (cmd.HasFlag("debug") || System.Diagnostics.Debugger.IsAttached)
                {
                    Console.WriteLine(ex.StackTrace);
                }
            }

            if (System.Diagnostics.Debugger.IsAttached)
                Console.ReadLine();
        }

        public static void WriteAllBytes(string path, byte[] bytes, FileMode mode = FileMode.Create)
        {
            using (var stream = new FileStream(path, mode))
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }


        public static byte[] ReadByteStream(Stream stream)
        {
            byte[] buffer = new byte[32768];
            using (MemoryStream ms = new MemoryStream())
            {
                while (true)
                {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        return ms.ToArray();
                    ms.Write(buffer, 0, read);
                }
            }
        }

        static Blob ReadFile(string path, long offset = 0, long length = 0)
        {
            byte[] result = new byte[0];

            if (File.Exists(path))
            {
                if (offset == 0 && length == 0)
                {
                    result = File.ReadAllBytes(path);
                }
                else
                {
                    long size = new FileInfo(path).Length;

                    if (offset < 0)
                        offset = size + offset;

                    if (offset < 0)
                        return null;

                    if (length == 0)
                    {
                        length = (int)size - offset;
                    }

                    
                    if (offset > size)
                        throw new Exception("Offset out of range.");
                    if (offset + length > size)
                        length = (int)size - offset;

                    result = new byte[length];

                    using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
                    {
                        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                        reader.Read(result, 0, (int)length);
                    }
                }
            }
            else {

                int idx = path.LastIndexOf(':');
                if (idx != -1 && idx != 2)
                {
                    string filename = path.Substring(0, idx);
                    string datastream = path.Substring(idx + 1);
                    if (File.Exists(filename))
                    {

                        var fdss = new FileInfo(filename).GetDataStreams().Where(c => c.Name == datastream || c.Name == datastream + ":$DATA");

                        if(fdss == null)
                            throw new Exception($"Alternate data stream \"{path}\" not found!");

                        var fds = fdss.First();

                        long size = fds.Length;

                        if (offset < 0)
                            offset = size + offset;

                        if (offset < 0)
                            return null;

                        if (length == 0)
                        {
                            length = (int)size - offset;
                        }


                        if (offset > size)
                            throw new Exception("Offset out of range.");
                        if (offset + length > size)
                            length = (int)size - offset;

                        result = new byte[length];

                        using (BinaryReader reader = new BinaryReader(fds.OpenRead()))
                        {
                            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                            reader.Read(result, 0, (int)length);
                        }
                    }
                    else
                    {
                        throw new Exception($"File \"{path}\" not found!");
                    }
                    

                }
                    
            }

            return new Blob((int)offset, result);
        }

        /*
        static string[] ReadLines(string path, int offset = 0, int length = 0)
        {
            string[] result = new string[0];
            if (File.Exists(path))
            {
                result = File.ReadAllText(path).Split('\n');
                if (offset == 0 && length == 0)
                {
                    return result.ToArray();
                }
                else
                {
                    result = (string[])result.Skip(offset).Take(length).Select(eachElement => eachElement.Clone()).ToArray();
                }
            }
            else
                throw new Exception($"File \"{path}\" not found!");

            return result.ToArray();
        }
        */

        static int SetBytesPerLine(int bPerLine)
        {
            bytesPerLine = bPerLine;

            firstCharColumn = firstHexColumn
                                + bytesPerLine * 3       // - 2 digit for the hexadecimal value and 1 space
                                + (bytesPerLine - 1) / dynamicSteps // - 1 extra space every 8 characters from the 9th
                                + 2;                  // 2 spaces 

            lineLength = firstCharColumn + bytesPerLine + Environment.NewLine.Length;       // - characters to show the ascii value + Carriage return and line feed (should normally be 2)

            return lineLength;
        }

        public static void BinDump(byte[] bytes, int lineLength = 0)
        {
            if (lineLength == 0)
                lineLength = windowWidth - (windowWidth % 2) - 1;
            if (bytes == null) return;
            for (int i = 0; i < bytes.Length; i += lineLength)
            {
                string line = "";
                for (int j = 0; j < lineLength; j++)
                {
                    if (!(i + j >= bytes.Length))
                    {
                        byte b = bytes[i + j];
                        string color = ColorTheme.GetColor(b, j % 2 == 0);

                        line += ("" + (b < 32 ? nonPrintableChar : (char)b)).Pastel(color);
                    }
                    else
                    {
                        line += spaceChar;
                    }
                }
                WriteLine(line);
            }
        }

        public static void ArrayDump(byte[] bytes, int lineLength = 32, ConsoleHelper.OutputMode outputMode = ConsoleHelper.OutputMode.Hex)
        {

            if (lineLength == 0)
                lineLength = windowWidth - (windowWidth % 2) - 1;
            if (bytes == null) return;

            Write("{ ");

            for (int i = 0; i < bytes.Length; i += lineLength)
            {
                string line = "";
                for (int j = 0; j < lineLength; j++)
                {
                    var c = i + j;
                    if (!(c >= bytes.Length))
                    {
                        byte b = bytes[i + j];
                        string color = ColorTheme.GetColor(b, j % 2 == 0);

                        if (outputMode == ConsoleHelper.OutputMode.Hex)
                        {
                            line += ("0x" + b.ToString("X").ToLower().PadLeft(2, '0')).Pastel(color);
                        }
                        else if (outputMode == ConsoleHelper.OutputMode.Dec)
                        {
                            line += Convert.ToString(b, 10).Pastel(color);
                        }
                        else if (outputMode == ConsoleHelper.OutputMode.Oct)
                        {
                            line += Convert.ToString(b, 8).PadLeft(3, '0').Pastel(color);
                        }

                        else if (outputMode == ConsoleHelper.OutputMode.Bin)
                        {
                            line += Convert.ToString(b, 2).PadLeft(8, '0').Pastel(color);
                        }
                        else if (outputMode == ConsoleHelper.OutputMode.Char)
                        {
                            if(!((b < 32) || (b > 255)))
                                line += "'" + (char)b + "'".Pastel(color);
                            else
                                line += ("0x" + b.ToString("X").ToLower().PadLeft(2, '0')).Pastel(color);
                        }


                        if (c < bytes.Length-1)
                            line += ",";
                        line += spaceChar;
                    }
                    else
                    {
                        //line += spaceChar;
                    }
                }
                Write(line);
            }
            WriteLine("};");
        }

        public static void StringDump(byte[] bytes, Encoding encoding)
        {
            // TODO Linenumbers + Gremlins
            if (bytes == null) return;
            string output = encoding.GetString(bytes);
            //Console.WriteLine(output);
            bool isGremlin = false;
            bool utf8Gremlin = false;
            string newLine = string.Empty;
            int lineCounter = 0;

            foreach (char c in output)
            {
                int i = (int)c;
                string color = ColorTheme.GetColor(i, true);


                if (!isGremlin)
                {
                    if (utf8Gremlin)
                        //                                                LF           CR           TAB
                        isGremlin = (i < 32 || i > 0xff) && (i != 0x0a && i != 0x0d && i != 0x09);
                    else
                        isGremlin = (i < 32 || i > 0x7f) && (i != 0x0a && i != 0x0d && i != 0x09);
                }

                /*
                
                else if (i == 0x09)    // Tab
                    Console.Write((cmd.HasFlag("no-tab") || cmd.HasFlag("plain") ? "\t" : $"\\t{nonPrintableChar}{nonPrintableChar}").Pastel(ColorTheme.DarkColor));

                */
                if (i == 0x0A)
                {     // LF
                    lineCounter++;
                    WriteLine($"{newLine}");
                    newLine = string.Empty;
                }

                else if (i == 0x20)    // Space
                    newLine += (((!cmd.HasFlag("convert-space") || cmd.HasFlag("plain") ? " " : "_").Pastel(ColorTheme.DarkColor)));

                else if (i == 0x0d)    // CR
                    newLine += (((cmd.HasFlag("no-cr") || cmd.HasFlag("plain") ? "\r" : "¬").Pastel(ColorTheme.DarkColor)));
                else if (((i < 32) || (i > 255)) && i != 0xA)    // Unprintable (control chars, UTF-8, etc.)
                    newLine += (((cmd.HasFlag("plain") || !cmd.HasFlag("convert-hex") ? c.ToString() : ("\\x" + i.ToString("X").ToLower())).Pastel(ColorTheme.HighLight2)));
                else
                    newLine += ($"{c}".Pastel(color));

            }
            if(lineCounter == 0)
                WriteLine($"{newLine}");
        }

        // based on https://stackoverflow.com/a/38625726
        public static int Find(byte[] needle, byte[] haystack, int offset = 0)
        {
            int c = haystack.Length - needle.Length + 1;
            for (int i = offset; i < c; i++)
            {
                if (haystack[i] != needle[0]) // compare only first byte
                    continue;

                if (needle.Length == 1) // return offset if needle is only one byte
                    return i;

                // found a match on first byte, now try to match rest of the pattern
                for (int j = needle.Length - 1; j >= 1; j--)
                {
                    if (haystack[i + j] != needle[j]) break;
                    if (j == 1) return i;
                }
            }
            return -1;
        }

        static void Write(char input)
        {
           Console.Write(input);
        }

        static void Write(string input)
        {
            Console.Write(input);
        }

        static void WriteLine(string input)
        {
            Write(input + Environment.NewLine);
        }

        public static void Die(string msg, int errorcode)
        {
            ConsoleHelper.WriteError(msg);
            Environment.Exit(errorcode);
        }

        static void ShowHelp(bool more = true)
        {
            ShowVersion();
            WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName.Replace(".exe","").Pastel(color1)} [{"Options".Pastel(color2)}] \"{"file".Pastel(color2)}\" {"|".Pastel(ColorTheme.DarkText)} {"-i".Pastel(color1)} \"{"input string".Pastel(color2)}\"");
            if(more)
                WriteLine($"\nFor more options, use {"--help".Pastel(color1)}");
        }
        static void ShowLongHelp()
        {
            ShowHelp(false);
            WriteLine($"\n{"Options".Pastel(color2)}:");
            foreach (CmdOption c in cmd.OrderBy(x => x.Name))
            {
                string l = $"  --{c.Name}".Pastel(color1) + (!string.IsNullOrEmpty(c.ShortName) ? $", {("-" + c.ShortName).Pastel(color1)}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel(color2)).ToArray()) + ">" : "") + ": " + c.Description;
                WriteLine(l);
            }
            //WriteError("Usage: subnet [ip/cidr|ip/mask|ip number_of_hosts]");
            Environment.Exit(0);
        }
        static void ShowVersion()
        {
            WriteLine(" ▄  █ ▄███▄      ▄  ▄███▄   ".Pastel("#e01e37"));
            WriteLine("█   █ █▀   ▀ ▀▄   █ █▀   ▀  ".Pastel("#c71f37"));
            WriteLine("██▀▀█ ██▄▄     █ ▀  ██▄▄    ".Pastel("#bd1f36"));
            WriteLine("█   █ █▄   ▄▀ ▄ █   █▄   ▄▀ ".Pastel("#a71e34"));
            WriteLine("   █  ▀███▀  █   ▀▄ ▀███▀   ".Pastel("#85182a"));
            WriteLine("  ▀           ▀ ".Pastel("#641220") + ("v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()).Pastel("#a71e34"));
            WriteLine($"{"hexe".Pastel(color1)} is part of " + ConsoleHelper.GetVersionString(color2,color2));
        }

    }
}
