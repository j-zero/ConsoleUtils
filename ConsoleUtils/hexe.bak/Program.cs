﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pastel;

namespace hexe
{
    // TODO cut to binary, patch
    internal class Program
    {
        static int firstHexColumn = 12; // 8 characters for the address +  3 spaces
        static int firstCharColumn = 0;
        static int lineLength = 0;
        static int bytesPerLine = 0;
        static int dynamicSteps = 8;

        static char nonPrintableChar = '·';
        static char spaceChar = ' ';

        static char[] HexChars = "0123456789abcdef".ToCharArray();
        static bool noOffset = false;
        static long startOffset = 0;
        static long defaultLength = 256;


        static void Main(string[] args)
        {

            CmdParser cmd = new CmdParser(args)
            {
                { "cut", "c", CmdCommandTypes.VERB, new CmdParameters() {
                    { CmdParameterTypes.INT, 0},
                    { CmdParameterTypes.INT, defaultLength},
                }, "Cut from here to there" },

                { "head", "h", CmdCommandTypes.VERB, $"Show first {defaultLength} bytes" },
                { "tail", "t", CmdCommandTypes.VERB, $"Show least {defaultLength} bytes" },
                { "bin", "b", CmdCommandTypes.FLAG, "Binary mode" },
                { "file", "f", CmdCommandTypes.UNNAMED, "File to read" },

            };

            cmd.DefaultParameter = "file";

            if (noOffset)
                firstHexColumn = 0;

            if (args.Length == 0)
            {
                // Read STDIN
                if (Console.IsInputRedirected)
                {
                    using (Stream s = Console.OpenStandardInput())
                    {
                        byte[] data = ReadByteStream(s);
                        WriteHexDump(data, 16);
                    }
                }
                else
                {
                    Console.WriteLine("No input given."); // line input?
                    Environment.Exit(255);
                }
            }
            else
            {
                cmd.Parse();
                byte[] data = new byte[0];
                long offset = 0;
                long length = 0;

                // Get Data
                if (Console.IsInputRedirected)
                {
                    using (Stream s = Console.OpenStandardInput())
                    {
                        data = ReadByteStream(s);
                    }
                }
                else
                {
                    foreach (string path in cmd["file"].Strings)
                    {
                        // todo file not found
                        foreach (string verb in cmd.Verbs)
                        {
                            if (verb == "cut" || verb == "head" || verb == "tail")
                            {
                                switch (verb)
                                {
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
                                data = ReadFile(path, offset, length);
                                if (cmd.HasFlag("bin"))
                                {
                                    int binLineLength = Console.WindowWidth - (Console.WindowWidth % 2) - 1;
                                    BinDump(data, binLineLength);
                                }
                                else
                                {
                                    WriteHexDump(data, 16);
                                }
                                
                            }


                            
                        }
                    }
                }

                /*
                if (command == "bin")
                {
                    int binLineLength = Console.WindowWidth - (Console.WindowWidth % 2) - 1;
                    BinDump(data, binLineLength);
                }
                else
                {
                    WriteHexDump(data, 16);
                }
                */

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

        public static void HexDump(byte[] bytes)
        {
            if (bytes == null) return;

            int bytesLength = bytes.Length;
            int offsetLength = 8;

            // header
            for (int i = 0; i < offsetLength; i++)
                Console.Write(spaceChar);


            for (int i = 0; i < bytesLength; i += bytesPerLine)
            {
                char[] offsetPart = new char[offsetLength];
                string hexPart = string.Empty;
                string asciiPart = string.Empty;

                int offsetShift = 0;

                for (int o = (offsetLength - 1); o >= 0; o--)
                {
                    offsetPart[o] = HexChars[((i + startOffset) >> offsetShift) & 0xF];
                    offsetShift += 4;
                }



                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & (dynamicSteps - 1)) == 0)
                        hexPart += spaceChar;

                    if (i + j >= bytesLength)
                    {
                        hexPart += spaceChar;
                    }
                    else
                    {
                        byte b = bytes[i + j];

                        string newHexPart = string.Empty;

                        newHexPart += (HexChars[(b >> 4) & 0xF]);
                        newHexPart += (HexChars[b & 0xF]);

                        string color = "";

                        bool isOdd = j % 2 == 0;


                        if (b == 0x00)
                            color = isOdd ? "D7DDEB" : "B0BAD7";
                        else if (b == 0x10 || b == 0x13)    // CR LF
                            color = isOdd ? "FFE39D" : "FEB80A";
                        else if (b < 32)
                            color = isOdd ? "E17B7C" : "EBA7A8";
                        else
                            color = isOdd ? "9CDCFE" : "569CD6";

                        hexPart += newHexPart.Pastel(color);
                        hexPart += spaceChar;

                        asciiPart += ("" + (b < 32 ? nonPrintableChar : (char)b)).Pastel(color);

                    }
                }
                Console.WriteLine(new string(offsetPart).Pastel("DCDCDC") + "   " + hexPart + "   " + asciiPart); ;
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