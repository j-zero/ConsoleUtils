using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pastel;
using System.Drawing;


public class ConsoleHelper
{
    static char nonPrintableChar = '·';
    static char spaceChar = ' ';

    public static void WriteError(string message)
    {
            Console.Error.Write($"{message.Pastel(Color.OrangeRed)}\n");
    }

    public static void WriteError(Exception ex)
    {
        Console.Error.Write($"{ex.Message.Pastel(Color.OrangeRed)}\n{ex.StackTrace}");
    }

    public static void WriteErrorDog(string message)
    {
        string doggo = "            \\\n             \\\n            /^-----^\\\n            V  o o  V\n             |  Y  |\n              \\ Q /\n              / - \\\n              |    \\\n              |     \\     )\n              || (___\\====";
        string msg = message.Length < 12 ? message.PadLeft(11) : message;
        Console.Error.Write($"\n   {msg.Pastel(Color.OrangeRed)}\n{doggo.Pastel(Color.White)}\n\n");
    }

    public static string GetVersionString()
    {
        return "ConsoleUtils (https://github.com/j-zero/ConsoleUtils)";
    }

    public static void BinDump(byte[] bytes, int lineLength = 0)
    {   if(lineLength == 0)
            lineLength = Console.WindowWidth - (Console.WindowWidth % 2) - 1;
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


    public static void SimpleHexDump(byte[] bytes, int bytesPerLine = 16)
    {
        if (bytes == null)
        {
            Console.WriteLine("<null>");
            return;
        }
        int bytesLength = bytes.Length;

        char[] HexChars = "0123456789ABCDEF".ToCharArray();

        int firstHexColumn =
              8                   // 8 characters for the address
            + 3;                  // 3 spaces

        int firstCharColumn = firstHexColumn
            + bytesPerLine * 3       // - 2 digit for the hexadecimal value and 1 space
            + (bytesPerLine - 1) / 8 // - 1 extra space every 8 characters from the 9th
            + 2;                  // 2 spaces 

        int lineLength = firstCharColumn
            + bytesPerLine           // - characters to show the ascii value
            + Environment.NewLine.Length; // Carriage return and line feed (should normally be 2)

        char[] line = (new String(' ', lineLength - Environment.NewLine.Length) + Environment.NewLine).ToCharArray();
        int expectedLines = (bytesLength + bytesPerLine - 1) / bytesPerLine;
        StringBuilder result = new StringBuilder(expectedLines * lineLength);

        for (int i = 0; i < bytesLength; i += bytesPerLine)
        {
            line[0] = HexChars[(i >> 28) & 0xF];
            line[1] = HexChars[(i >> 24) & 0xF];
            line[2] = HexChars[(i >> 20) & 0xF];
            line[3] = HexChars[(i >> 16) & 0xF];
            line[4] = HexChars[(i >> 12) & 0xF];
            line[5] = HexChars[(i >> 8) & 0xF];
            line[6] = HexChars[(i >> 4) & 0xF];
            line[7] = HexChars[(i >> 0) & 0xF];

            int hexColumn = firstHexColumn;
            int charColumn = firstCharColumn;

            for (int j = 0; j < bytesPerLine; j++)
            {
                if (j > 0 && (j & 7) == 0) hexColumn++;
                if (i + j >= bytesLength)
                {
                    line[hexColumn] = ' ';
                    line[hexColumn + 1] = ' ';
                    line[charColumn] = ' ';
                }
                else
                {
                    byte b = bytes[i + j];
                    line[hexColumn] = HexChars[(b >> 4) & 0xF];
                    line[hexColumn + 1] = HexChars[b & 0xF];
                    line[charColumn] = (b < 32 ? '·' : (char)b);
                }
                hexColumn += 3;
                charColumn++;
            }
            result.Append(line);
        }
        Console.WriteLine(result.ToString());
    }
}

