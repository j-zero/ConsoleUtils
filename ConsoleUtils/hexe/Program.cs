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
        static long startOffset = 0;
        static long defaultLength = 256;

        static CmdParser cmd;

        static void Main(string[] args)
        {

            cmd = new CmdParser(args)
            { // Todo: is default[verb|parameter]
                { "show", null, CmdCommandTypes.VERB, $"Show complete file, default." },

                { "cut", "c", CmdCommandTypes.VERB, new CmdParameters() {
                    { CmdParameterTypes.INT, 0},
                    { CmdParameterTypes.INT, defaultLength},
                }, "Cut from here to there" },

                { "head", "h", CmdCommandTypes.VERB, $"Show first {defaultLength} bytes" },
                { "tail", "t", CmdCommandTypes.VERB, $"Show least {defaultLength} bytes" },
                { "bin", "b", CmdCommandTypes.FLAG, "Binary mode" },
                { "debug", "d", CmdCommandTypes.FLAG, "Debug mode" },

                { "nooffset", "n", CmdCommandTypes.FLAG, "Show no offset" },
                { "noascii", "a", CmdCommandTypes.FLAG, "Show no ascii" },
                { "bytes", null, CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.INT, 16 }
                }, "File to read" },
                { "file", "f", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, null } 
                }, "File to read" }

            };

            cmd.DefaultParameter = "file";
            cmd.DefaultVerb = "show";

            cmd.Parse();

            noAscii = cmd.HasFlag("noascii");
            noOffset = cmd.HasFlag("nooffset");
            bytesPerLine = (int)cmd["bytes"].Longs[0];


            byte[] data = new byte[0];
            long offset = 0;
            long length = 0;

            if (noOffset)
                firstHexColumn = 0;

            try
            {
                foreach (string verb in cmd.Verbs)
                {
                    if (verb == "show" || verb == "cut" || verb == "head" || verb == "tail")
                    {
                        switch (verb)
                        {
                            case "show":
                                offset = 0;
                                length = 0;
                                break;
                            case "cut":
                                offset = cmd[verb].Longs[0];
                                length = cmd[verb].Longs[1];
                                break;
                            case "head":
                                offset = 0;
                                length = defaultLength;
                                break;
                            case "tail":
                                offset = defaultLength * -1;
                                length = defaultLength;
                                break;

                        }

                        if (Console.IsInputRedirected)
                        {
                            using (Stream s = Console.OpenStandardInput())
                            {
                                data = ReadByteStream(s);
                            }
                        }
                        else
                        {
                            if (cmd["file"].Strings.Length > 0 && cmd["file"].Strings[0] != null)
                            {
                                string path = cmd["file"].Strings[0];
                                data = ReadFile(path, offset, length);
                            }
                                
                        }

                        if (cmd.HasFlag("bin"))
                        {
                            int binLineLength = Console.WindowWidth - (Console.WindowWidth % 2) - 1;
                            BinDump(data, binLineLength);
                        }
                        else
                        {
                            WriteHexDump(data, bytesPerLine);
                        }

                    }
                }

                
                
            }
            catch(Exception ex)
            {
                WriteError(ex.Message);
            }

            if (cmd.HasFlag("debug"))
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
        static byte[] ReadFile(string path, long offset = 0, long length = 0)
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

                    startOffset = offset;

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
            else
                throw new Exception($"File \"{path}\" not found!");

            return result;
        }

        static void WriteHexDump(byte[] data, int BytesPerLine)
        {

            if (BytesPerLine == 0)  // dynamic
            {
                int dynWidth = Console.WindowWidth;
                int minWidth = 32;
                //dynWidth = 31;

                // Hex

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


            HexDump(data);
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
                        string color = GetColor(b, j % 2 == 0);

                        line += ("" + (b < 32 ? nonPrintableChar : (char)b)).Pastel(color); ;
                    }
                    else
                    {
                        line += spaceChar;
                    }
                }
                Console.WriteLine(line);
            }
        }

        public static void HexDump(byte[] bytes, bool header = false)
        {
            if (bytes == null) return;
            string spacer = "   ";
            string offsetPrefix = "0x";
            int bytesLength = bytes.Length;
            int offsetLength = (startOffset + bytes.Length - 1).ToString("X").Length; // todo offset length by parameter
            if (offsetLength % 2 != 0) offsetLength++;
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
                //char[] offsetPart = new char[offsetLength];
                string offsetPart = string.Empty;
                string hexPart = string.Empty;
                string asciiPart = string.Empty;

                int offsetShift = 0;

                /*
                for (int o = (offsetLength - 1); o >= 0; o--)
                {
                    offsetPart[o] = HexChars[((i + startOffset) >> offsetShift) & 0xF];
                    offsetShift += 4;
                }
                */

                offsetPart = (i + startOffset).ToString("X").ToLower().PadLeft(offsetLength, '0');

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
                        byte b = bytes[i + j];

                        string newHexPart = string.Empty;

                        newHexPart += (HexChars[(b >> 4) & 0xF]);
                        newHexPart += (HexChars[b & 0xF]);

                        string color = GetColor(b, j % 2 == 0);


                        hexPart += newHexPart.Pastel(color);
                        hexPart += spaceChar;

                        asciiPart += ("" + (b < 32 ? nonPrintableChar : (char)b)).Pastel(color);

                    }
                }
                //Console.WriteLine((noOffset ? string.Empty : "0x" + new string(offsetPart).Pastel("DCDCDC") + "   ") + hexPart + (noAscii ? string.Empty : "   " + asciiPart)); 
                Console.WriteLine((noOffset ? string.Empty : offsetPrefix + offsetPart.Pastel("DCDCDC") + spacer) + hexPart + (noAscii ? string.Empty : spacer + asciiPart));
            }
        }

        public static string GetColor(byte b, bool isOdd)
        {
            string color = "";
            if (b == 0x00)
                color = isOdd ? "D7DDEB" : "B0BAD7";
            else if (b == 0x10 || b == 0x13)    // CR LF
                color = isOdd ? "FFE39D" : "FEB80A";
            else if (b < 32)
                color = isOdd ? "EBA7A8" : "E17B7C";
            else
                color = isOdd ? "9CDCFE" : "569CD6";

            return color;
        }

        public static void WriteError(string message)
        {
            string doggo = "            \\\n             \\\n            /^-----^\\\n            V  o o  V\n             |  Y  |\n              \\ Q /\n              / - \\\n              |    \\\n              |     \\     )\n              || (___\\====";
            string msg = message.Length < 12 ? message.PadLeft(11) : message;
            Console.Write($"\n   {msg.Pastel(Color.Salmon)}\n{doggo.Pastel(Color.White)}\n\n"); // TODO STDERR
        }

        public static void Die(string msg, int errorcode)
        {
            WriteError(msg);
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
    }
}
