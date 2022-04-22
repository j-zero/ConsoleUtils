using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pastel;
using System.Drawing;

namespace hexe
{
    // TODO cut to binary, patch
    internal class Program
    {
        static int firstHexColumn = 12; // 8 characters for the address +  3 spaces
        static int firstCharColumn = 0;
        static int lineLength = 0;
        static int bytesPerLine = 16;
        static int dynamicSteps = 8;

        static char nonPrintableChar = '·';
        static char spaceChar = ' ';

        static char[] HexChars = "0123456789abcdef".ToCharArray();
        static bool noOffset = false;
        static bool noAscii = false;
        //static long startOffset = 0;
        static int defaultLength = 256;

        static CmdParser cmd;



        static void Main(string[] args)
        {

            cmd = new CmdParser(args)
            { // Todo: is default[verb|parameter]
                { "show", null, CmdCommandTypes.VERB, $"Show complete file. Default." },
                { "find", null, CmdCommandTypes.VERB, $"Find byte pattern in complete file" },

                { "short", null, CmdCommandTypes.FLAG, $"Show head and tail" },
                { "bin", "b", CmdCommandTypes.FLAG, "Binary mode" },

                { "cut", "c", CmdCommandTypes.MULTIPE_PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.INT, 0},
                    { CmdParameterTypes.INT, defaultLength},
                }, "Cut from here to there" },

                { "head", "h", CmdCommandTypes.FLAG, $"Show first X bytes, can be modified with --count."},
                { "tail", "t", CmdCommandTypes.FLAG, $"Show last X bytes, can be modified with --count."},

                { "debug", "d", CmdCommandTypes.FLAG, "Debug mode" },

                { "no-offset", "", CmdCommandTypes.FLAG, "Show no offset" },
                { "no-ascii", "", CmdCommandTypes.FLAG, "Show no ascii" },

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
                }, "File to read" }

            };

            cmd.DefaultParameter = "file";
            cmd.DefaultVerb = "show";
            try
            {
                cmd.Parse();

                noAscii = cmd.HasFlag("no-ascii");
                noOffset = cmd.HasFlag("no-offset");

                bytesPerLine = (int)cmd["bytes-per-line"].Longs[0];


                List<Blob> data = new List<Blob>();
                List<Selection> parts = new List<Selection>();

                //long offset = 0;
                //long length = 0;

                parts.Add(new Selection(0, 0));

                if (noOffset)
                    firstHexColumn = 0;

                //long count = cmd["count"].Longs[0];
                parts[0].Offset = (int)cmd["offset"].Long;
                parts[0].Length = (int)cmd["count"].Long;
                /*
                offset = cmd["cut"].Longs[0];
                length = cmd["cut"].Longs[1];
                */

                if (cmd["cut"].WasUserSet)
                {
                    int k = 0;
                    int i = 0;
                    while(i < cmd["cut"].Longs.Length)
                    {
                        if (parts.Count <= k)
                            parts.Add(new Selection(0, 0));

                        parts[k].Offset = cmd["cut"].Ints[i++];
                        parts[k].Length = cmd["cut"].Ints[i++];
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



                if (cmd.Verbs.Length > 1)
                    throw new ArgumentException("You can't use more than one verb!");

                string verb = cmd.Verbs[0];


                // TODO: alle offsets vorher speichern, daten je nach input-stream einlesen und dann im loop ausgeben

                if (cmd.HasFlag("short"))
                {
                    int cHeight = (Console.WindowHeight / 2 - 1);
                    defaultLength = cHeight * bytesPerLine;

                    //head
                    parts[0].Offset = 0;
                    parts[0].Length = cmd["count"].WasUserSet ? parts[0].Length : defaultLength;

                    parts.Add(new Selection(0, 0));
                    //tail
                    parts[1].Offset = (cmd["count"].WasUserSet ? parts[0].Length : defaultLength) * -1;
                    parts[1].Length = defaultLength;
                }


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
                            data.Add(ReadFile(path, s.Offset, s.Length));
                    }
                }



                for(int i = 0; i < data.Count; i++)
                {
                    if (cmd.HasFlag("bin"))
                    {
                        int binLineLength = Console.WindowWidth - (Console.WindowWidth % 2) - 1;
                        BinDump(data[i].Data, binLineLength);
                    }
                    else
                    {
                        HexDump(data[i], bytesPerLine, false, (ulong)(data.Last().Offset + data.Last().Length));
                    }
                    if(i != data.Count - 1)
                        Console.WriteLine("...");
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

                    ;

                    result = new byte[length];
                    using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
                    {
                        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                        reader.Read(result, 0, (int)length);
                    }
                }
            }
            else
                throw new Exception($"File \"{path}\" not found!");

            return new Blob((int)offset, result);
        }

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
        public static void BinDump(byte[] bytes, int lineLength)
        {
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
                Console.WriteLine(line);
            }
        }

        

        public static void HexDump(Blob bytes, int BytesPerLine, bool header = false, ulong largestOffset = 0)
        {
            if (bytes == null) return;

            string spacer = "   ";
            string offsetPrefix = "0x";
            int bytesLength = bytes.Length;

            int offsetLength = largestOffset != 0 ? (largestOffset).ToString("X").Length : (bytes.Offset + bytes.Length - 1).ToString("X").Length;

            if (offsetLength % 2 != 0) offsetLength++;

            if (BytesPerLine == 0)  // dynamic
            {
                int dynWidth = Console.WindowWidth;
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

            

            //Console.WriteLine(bytes.Length.ToString("X"));

            // header
            if (header)
            {
                for (int i = 0; i < offsetLength + offsetPrefix.Length; i++)
                    Console.Write(spaceChar);
                Console.Write(spacer); // spacer
                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & (dynamicSteps - 1)) == 0)
                        Console.Write(spaceChar);
                    Console.Write(j.ToString("X").ToLower().PadLeft(2, '0') + spaceChar);
                }
                Console.Write(spacer); // spacer
                for (int j = 0; j < bytesPerLine; j++)
                {
                    Console.Write((j % 16).ToString("X").ToLower());
                }
                Console.Write(Environment.NewLine + Environment.NewLine);
            }

            for (int i = 0; i < bytesLength; i += bytesPerLine)
            {
                string offsetPart = string.Empty;
                string hexPart = string.Empty;
                string asciiPart = string.Empty;

                //int offsetShift = 0;

                offsetPart = (i + Math.Abs(bytes.Offset)).ToString("X").ToLower().PadLeft(offsetLength, '0');

                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & (dynamicSteps - 1)) == 0)
                        hexPart += spaceChar;

                    if (i + j >= bytesLength)
                    {
                        hexPart += (new string(new char[] { spaceChar , spaceChar , spaceChar }));
                    }
                    else
                    {
                        byte b = bytes.Data[i + j];

                        string newHexPart = string.Empty;

                        newHexPart += (HexChars[(b >> 4) & 0xF]);
                        newHexPart += (HexChars[b & 0xF]);

                        string color = ColorTheme.GetColor(b, j % 2 == 0);


                        hexPart += newHexPart.Pastel(color);
                        hexPart += spaceChar;

                        asciiPart += ("" + (b < 32 ? nonPrintableChar : (char)b)).Pastel(color);

                    }
                }
                //Console.WriteLine((noOffset ? string.Empty : "0x" + new string(offsetPart).Pastel("DCDCDC") + "   ") + hexPart + (noAscii ? string.Empty : "   " + asciiPart)); 
                Console.WriteLine((noOffset ? string.Empty : offsetPrefix + offsetPart.Pastel("DCDCDC") + spacer) + hexPart + (noAscii ? string.Empty : spacer + asciiPart));
            }
        }


        // based on https://stackoverflow.com/a/38625726
        public static long Find(byte[] needle, byte[] haystack, long offset = 0)
        {
            int c = haystack.Length - needle.Length + 1;
            for (long i = offset; i < c; i++)
            {
                if (haystack[i] != needle[0]) // compare only first byte
                    continue;

                // found a match on first byte, now try to match rest of the pattern
                for (int j = needle.Length - 1; j >= 1; j--)
                {
                    if (haystack[i + j] != needle[j]) break;
                    if (j == 1) return i;
                }
            }
            return -1;
        }



        public static void Die(string msg, int errorcode)
        {
            ConsoleHelper.WriteError(msg);
            Environment.Exit(errorcode);
        }

        public static void BinDump2(byte[] bytes, int lineLength)
        {
            if (bytes == null) return;



            // line[charColumn] = (b < 32 ? '·' : (char)b);

            char[] line = (new String(spaceChar, lineLength - Environment.NewLine.Length) + Environment.NewLine).ToCharArray();


            for (int i = 0; i < bytes.Length; i += lineLength)
            {
                int hexColumn = firstHexColumn;
                int charColumn = firstCharColumn;

                for (int j = 0; j < lineLength; j++)
                {
                    if (!(i + j >= bytes.Length))
                    {
                        byte b = bytes[i + j];
                        line[charColumn] = (b < 32 ? nonPrintableChar : (char)b);
                    }
                    else
                    {
                        line[charColumn] = spaceChar;
                    }

                    charColumn++;
                }
                Console.WriteLine(line);
            }
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
