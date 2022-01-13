using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hexe
{
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
        static int startOffset = 0;

        static void Main(string[] args)
        {
            

            if (noOffset)
            {
                firstHexColumn = 0;
            }

            if (args.Length > 1)
            {
                string command = args[0];
                string[] parameters = args.Skip(1).ToArray();

                if(command == "bin")
                {
                    string path = args[1];
                    byte[] data = ReadFile(path);

                    int binLineLength = Console.WindowWidth - (Console.WindowWidth % 2);
                    BinDump(data, binLineLength - 4);
                }
                else if(command == "hex")
                {
                    string path = null;
                    int offset = 0;
                    int length = 0;

                    if (parameters.Length == 1)
                    {
                        path = parameters[0];
                    }
                    if (parameters.Length == 2)
                    {
                        offset = int.Parse(parameters[0], System.Globalization.NumberStyles.HexNumber);
                        path = parameters[1];
                    }
                    if (parameters.Length == 3)
                    {
                        offset = int.Parse(parameters[0], System.Globalization.NumberStyles.HexNumber);
                        length = int.Parse(parameters[1]);
                        path = parameters[2];
                    }
                    byte[] data = ReadFile(path, offset, length);
                    WriteHexDump(data, 16);
                }
 

            }
            else if (args.Length == 1)
            {
                string path = args[0];
                byte[] data = ReadFile(path);
                WriteHexDump(data,16);
            }
            else   // no parameters
            {
                Console.WriteLine("No file given.");
            }

#if DEBUG
            //Console.WriteLine("DEBUG:");
            //Console.WriteLine($"Console.WindowWidth = {Console.WindowWidth}");
            //Console.ReadLine();
#endif
        }

        /*
        static void WriteBinDump(byte[] data, int length)
        {
            // Bin
            int binLineLength = Console.WindowWidth - 1;
            BinDump(data, binLineLength);
        }
        */

        static byte[] ReadFile(string path, int offset = 0, int length = 0)
        {
            startOffset = offset;
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
                    if (length == 0)
                    {
                        length = (int)size - offset;
                    }

                    if(offset > size)
                        throw new Exception("Offset out of range.");
                    if (offset + length > size)
                        length = (int)size - offset;

                    result = new byte[length];
                    using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
                    {
                        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                        reader.Read(result, 0, length);
                    }
                }
            }
            else
            {
                throw new Exception("File not found!");
            }
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

        /// Based on https://www.codeproject.com/Articles/36747/Quick-and-Dirty-HexDump-of-a-Byte-Array
        public static void HexDump(byte[] bytes)
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
