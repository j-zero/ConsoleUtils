using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pastel;
using System.Drawing;
using NtfsDataStreams;

namespace hexe
{
    // TODO cut to binary, patch, remove, skip
    internal class Program
    {
        public enum OutputMode
        {
            Hex,
            Dec,
            Oct,
            Bin
        }

        static int firstHexColumn = 12; // 8 characters for the address +  3 spaces
        static int firstCharColumn = 0;
        static int lineLength = 0;
        static int bytesPerLine = 16;
        static int dynamicSteps = 8;

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

        static OutputMode outputMode = OutputMode.Hex;

        static void Main(string[] args)
        {

            cmd = new CmdParser(args)
            { // Todo: is default[verb|parameter]
                { "help", "", CmdCommandTypes.FLAG, "Show this help." },


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
                }, "Skip from offset <int>, count <int>" },

                { "head", "h", CmdCommandTypes.FLAG, $"Show first X bytes, can be modified with --count."},
                { "tail", "t", CmdCommandTypes.FLAG, $"Show last X bytes, can be modified with --count."},

                { "debug", "D", CmdCommandTypes.FLAG, "Debug mode" },
                { "no-header", "H", CmdCommandTypes.FLAG, "disable header" },
                { "zero", "z", CmdCommandTypes.FLAG, "Set offset to zero on cut" },
                { "no-offset", "", CmdCommandTypes.FLAG, "Show no offset" },
                { "no-ascii", "", CmdCommandTypes.FLAG, "Show no ascii" },
                { "no-cr", "", CmdCommandTypes.FLAG, "Don't show carrige return" },
                { "convert-space", "", CmdCommandTypes.FLAG, "Mark space" },
                { "no-tab", "", CmdCommandTypes.FLAG, "Don't mark space" },
                { "no-colors", "", CmdCommandTypes.FLAG, "Don't color output" },
                { "no-line-numbers", "l", CmdCommandTypes.FLAG, "Don't show line numbers" },
                { "convert-hex", "", CmdCommandTypes.FLAG, "Show unprintable chars as hex values" },



                { "plain", "p", CmdCommandTypes.FLAG, "Combines --no-cr, --no-space, --no-tab, --no-line-numbers, --no-colors" },

                { "dump", "d", CmdCommandTypes.FLAG, "dump binary to [output]-file" },

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

                { "bytes-per-line", "B", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.INT, 16 }
                }, "bytes per line" },

                { "file", "f", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, null } 
                }, "File to read" },

                { "find", "F", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, null }
                }, "Find pattern" },

                { "find-string", "", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, null }
                }, "Find string" },

                { "base16", "", CmdCommandTypes.FLAG, "Output bytes as hexadecimal values (default)" },
                { "base8", "", CmdCommandTypes.FLAG, "Output bytes as octodecimal values" },
                { "base10", "", CmdCommandTypes.FLAG, "Output bytes as octodecimal values" },
                { "base2", "", CmdCommandTypes.FLAG, "Output bytes as binary values" },


                { "ascii", "", CmdCommandTypes.FLAG, "Set encoding to ASCII" },
                { "utf8", "", CmdCommandTypes.FLAG, "Set encoding to UTF8 (default)" },
                { "utf16", "", CmdCommandTypes.FLAG, "Set encoding to UTF16" },
                { "utf7", "", CmdCommandTypes.FLAG, "Set encoding to UTF7" },
                { "utf32", "", CmdCommandTypes.FLAG, "Set encoding to UTF32" },
                { "utf16be", "", CmdCommandTypes.FLAG, "Set encoding to UTF16BE" },

                { "codepage", "", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.INT, 0 }
                }, "Set encoding to codepage <int>" },


                { "output", "O", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Output file" },

            };

            cmd.DefaultParameter = "file";
            //cmd.DefaultVerb = "show";
            try
            {
                cmd.Parse();

                if (cmd.HasFlag("help"))
                    ShowHelp();

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

                /* 
                { "ascii", "", CmdCommandTypes.FLAG, "Set encoding to ASCII" },
                { "utf8", "", CmdCommandTypes.FLAG, "Set encoding to UTF8 (default)" },
                { "utf16", "", CmdCommandTypes.FLAG, "Set encoding to UTF16" },
                { "utf7", "", CmdCommandTypes.FLAG, "Set encoding to UTF7" },
                { "utf32", "", CmdCommandTypes.FLAG, "Set encoding to UTF32" },
                { "utf16be", "", CmdCommandTypes.FLAG, "Set encoding to UTF16BE" },
                */

                if (cmd["codepage"].WasUserSet)
                {
                    encoding = Encoding.GetEncoding((int)cmd["codepage"].Int);
                }
                else if (cmd.HasFlag("ascii"))
                    encoding = Encoding.ASCII;
                else if (cmd.HasFlag("utf8"))
                    encoding = Encoding.UTF8;
                else if (cmd.HasFlag("utf16"))
                    encoding = Encoding.Unicode;
                else if (cmd.HasFlag("utf7"))
                    encoding = Encoding.UTF7;
                else if (cmd.HasFlag("utf32"))
                    encoding = Encoding.UTF32;
                else if (cmd.HasFlag("utf16be"))
                    encoding = Encoding.BigEndianUnicode;


                if (cmd.HasFlag("base16"))
                    outputMode = OutputMode.Hex;
                else if (cmd.HasFlag("base8"))
                    outputMode = OutputMode.Oct;
                else if (cmd.HasFlag("base10"))
                    outputMode = OutputMode.Dec;
                else if (cmd.HasFlag("base2"))
                {
                    if (!cmd["cut"].WasUserSet)
                        bytesPerLine = 8;
                    outputMode = OutputMode.Bin;
                }

                /*
                if (cmd.Verbs.Length > 1)
                    throw new ArgumentException("You can't use more than one verb!");

                string verb = cmd.Verbs[0];
                */

                // TODO: alle offsets vorher speichern, daten je nach input-stream einlesen und dann im loop ausgeben

                if (cmd.HasFlag("short"))
                {
                    int cHeight = (windowHeight / 2 - 4);
                    
                    defaultLength = cHeight * bytesPerLine;

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
                else
                {
                    if (cmd["file"].Strings.Length > 0 && cmd["file"].Strings[0] != null)
                    {
                        string path = cmd["file"].Strings[0];
                        foreach(Selection s in parts)
                            data.Add(ReadFile(path, s.Offset, s.Length)); // needs to skip
                    }
                    else
                    {
                        ShowHelp();
                    }
                }



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
                        ArrayDump(data[i].Data);
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
                            needle = StringToByteArray(cmd["find"].String);
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

                                //Blob blob = new Blob(offset, new byte[needle.Length]);
                                Blob blob = new Blob(offset1, new byte[size]);
                                //Buffer.BlockCopy(data[i].Data, (int)offset, blob.Data, 0, needle.Length);
                                Buffer.BlockCopy(data[i].Data, (int)offset1, blob.Data, 0, size);

                                if(counter != 0)
                                    Console.WriteLine("...");

                                HexDump(blob, bytesPerLine, counter++ == 0, (ulong)(data.Last().Offset + data.Last().Length), false, offset, needle.Length, outputMode);
                                
                                //foundData.Add(blob);
                            }
                                    

                        }

                        
                    }

                    else
                    {
                        HexDump(data[i], bytesPerLine, !cmd.HasFlag("no-header") && (i != 1), (ulong)(data.Last().Offset + data.Last().Length), (cmd.HasFlag("zero")) && (data.Count > 1), -1, -1, outputMode);
                    }
                    if(i != data.Count - 1)
                        WriteLine("...");
                }
                
                        

            }
            catch (ArgumentException ex)
            {
                ConsoleHelper.WriteError(ex.Message);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError(ex.Message);
                if(cmd.HasFlag("debug") || System.Diagnostics.Debugger.IsAttached)
                    Console.WriteLine(ex.StackTrace);
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


        // https://stackoverflow.com/a/321404
        public static byte[] StringToByteArray(string input) // slow as fuck, but works
        {
            string hex = input.Replace(" ", "").Replace("0x","");

            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
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

        public static void ArrayDump(byte[] bytes, int lineLength = 32)
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

                        line += ("0x" + b.ToString("X").ToLower().PadLeft(2, '0')).Pastel(color);
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


        public static void HexDump(Blob bytes, int BytesPerLine, bool header = false, ulong largestOffset = 0, bool zeroOffset = false, int highlightOffset = -1, int highlightLength = -1, OutputMode outputMode = OutputMode.Hex)
        {
            if (bytes == null) return;

            string spacer = "   ";
            string offsetPrefix = "0x";
            int bytesLength = bytes.Length;

            int offsetLength = largestOffset != 0 ? (largestOffset).ToString("X").Length : (bytes.Offset + bytes.Length - 1).ToString("X").Length;

            if (offsetLength % 2 != 0) offsetLength++;

            if (BytesPerLine == 0)  // dynamic
            {
                int dynWidth = windowWidth;
                int minWidth = 32;
                while (lineLength < dynWidth - dynamicSteps - Environment.NewLine.Length)
                {
                    SetBytesPerLine(bytesPerLine += dynamicSteps);
                    if (lineLength > dynWidth)
                    {
                        SetBytesPerLine(bytesPerLine -= dynamicSteps);
                        break;
                    }

                }
                if (lineLength < minWidth)
                    SetBytesPerLine(dynamicSteps);
            }
            else
            {
                SetBytesPerLine(BytesPerLine);
            }

            int padding = 2;
            if (outputMode == OutputMode.Hex)
                padding = 2;
            else if (outputMode == OutputMode.Dec || outputMode == OutputMode.Oct)
                padding = 3;
            else if (outputMode == OutputMode.Bin)
                padding = 8;


            //Console.WriteLine(bytes.Length.ToString("X"));

            // header
            if (header)
            {
                for (int i = 0; i < offsetLength + offsetPrefix.Length; i++)
                    Write(spaceChar);
                Write(spacer); // spacer
                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & (dynamicSteps - 1)) == 0)
                        Write(spaceChar);
                    Write(j.ToString("X").ToLower().PadLeft(padding, '0').Pastel(ColorTheme.OffsetColor) + spaceChar);
                }
                Write(spacer); // spacer
                for (int j = 0; j < bytesPerLine; j++)
                {
                    Write((j % 16).ToString("X").ToLower().Pastel(ColorTheme.OffsetColor));
                }
                Write(Environment.NewLine);
            }

            for (int i = 0; i < bytesLength; i += bytesPerLine)
            {
                string offsetPart = string.Empty;
                string hexPart = string.Empty;
                string asciiPart = string.Empty;

                offsetPart = (i + (zeroOffset ? 0 : Math.Abs(bytes.Offset))).ToString("X").ToLower().PadLeft(offsetLength, '0');

                for (int j = 0; j < bytesPerLine; j++)
                {
                    int pos = i + j;
                    int relativePos = pos + (int)bytes.Offset;

                    if (j > 0 && (j & (dynamicSteps - 1)) == 0)
                        hexPart += spaceChar;

                    if (pos >= bytesLength)
                    {
                        for (int s = 0; s <= padding; s++) hexPart += spaceChar; // Spaces before ascii-part 
                    }
                    else
                    {
                       
                        byte b = bytes.Data[pos];
                        int first = (b >> 4) & 0xF;
                        int second = b & 0xF;

                        string newHexPart = string.Empty;


                        if (outputMode == OutputMode.Hex)
                        {

                            newHexPart += HexChars[first];
                            newHexPart += HexChars[second];

                            //newHexPart += " (" +b.ToString().PadLeft(3, '0') + ")";
                        }
                        else if (outputMode == OutputMode.Dec)
                        {
                            newHexPart += b.ToString().PadLeft(3, '0');
                        }
                        else if (outputMode == OutputMode.Oct)
                        {
                            newHexPart += Convert.ToString(b, 8).PadLeft(3, '0');
                        }

                        else if (outputMode == OutputMode.Bin)
                        {
                            newHexPart += Convert.ToString(first, 2).PadLeft(4, '0');
                            newHexPart += Convert.ToString(second, 2).PadLeft(4, '0');
                        }

                        string color = ColorTheme.GetColor(b, j % 2 == 0);

                        if((highlightOffset != -1 && highlightLength != -1))
                        {
                            if((relativePos < highlightOffset) || (relativePos >= (highlightOffset + highlightLength)))
                                color = "#666666";
                        }

                        hexPart += newHexPart.Pastel(color);
                        hexPart += spaceChar;

                        asciiPart += ("" + (b < 32 ? nonPrintableChar : (char)b)).Pastel(color);

                    }
                }
                WriteLine((noOffset ? string.Empty : (offsetPrefix + offsetPart).Pastel(ColorTheme.OffsetColor) + spacer) + hexPart + (noText ? string.Empty : spacer + asciiPart));
            }
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

        static void ShowHelp()
        {
            Console.WriteLine($"hexe, {ConsoleHelper.GetVersionString()}");
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [Options] {{file}}");
            Console.WriteLine($"Options:");
            foreach (CmdOption c in cmd.OrderBy(x => x.Name))
            {
                string l = $"  --{c.Name}".Pastel("9CDCFE") + (!string.IsNullOrEmpty(c.ShortName) ? $", {("-" + c.ShortName).Pastel("9CDCFE")}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel("569CD6")).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
            //WriteError("Usage: subnet [ip/cidr|ip/mask|ip number_of_hosts]");
            Environment.Exit(0);
        }

        /// Based on https://www.codeproject.com/Articles/36747/Quick-and-Dirty-HexDump-of-a-Byte-Array
        /*
        public static void HexDump2(byte[] bytes)
        {
            if (bytes == null) return;
            int bytesLength = bytes.Length;

            char[] line = (new String(spaceChar, lineLength - Environment.NewLine.Length) + Environment.NewLine).ToCharArray();
            //int expectedLines = (bytesLength + bytesPerLine - 1) / bytesPerLine;
            //StringBuilder result = new StringBuilder(expectedLines * lineLength);



            for (int i = 0; i < bytesLength; i += bytesPerLine)
            {
                // Offset
                if (!noOffset)
                {
                    line[0] = HexChars[((i + startOffset) >> 28) & 0xF];
                    line[1] = HexChars[((i + startOffset) >> 24) & 0xF];
                    line[2] = HexChars[((i + startOffset) >> 20) & 0xF];
                    line[3] = HexChars[((i + startOffset) >> 16) & 0xF];
                    line[4] = HexChars[((i + startOffset) >> 12) & 0xF];
                    line[5] = HexChars[((i + startOffset) >> 8) & 0xF];
                    line[6] = HexChars[((i + startOffset) >> 4) & 0xF];
                    line[7] = HexChars[((i + startOffset) >> 0) & 0xF];
                    // End Offset
                }
                int hexColumn = firstHexColumn;
                int charColumn = firstCharColumn;

                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & (dynamicSteps - 1)) == 0)
                        hexColumn++;

                    if (i + j >= bytesLength)
                    {
                        //line[hexColumn] = spaceChar;
                        //line[hexColumn + 1] = spaceChar;
                        line[charColumn] = spaceChar;
                    }
                    else
                    {
                        byte b = bytes[i + j];
                        line[hexColumn] = HexChars[(b >> 4) & 0xF];
                        line[hexColumn + 1] = HexChars[b & 0xF];
                        line[charColumn] = (b < 32 ? nonPrintableChar : (char)b);
                    }
                    hexColumn += 3;
                    charColumn++;
                }
                //result.Append(line);
                Console.Write(line);
            }
            //return result.ToString();
        }
        */
    }
}
