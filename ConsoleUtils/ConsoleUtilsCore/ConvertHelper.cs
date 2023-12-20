using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ConvertHelper
{
    // https://stackoverflow.com/a/321404
    public static byte[] HexStringToByteArray(string input) // slow as fuck, but works
    {

        string hex = input.Replace(" ", "").Replace("0x", "").Replace("%", "");

        if (hex.Length % 2 != 0)
        {
            //Console.Error.WriteLine("Warning: Length not divisible by 2, not a valid hex string. Adding leading 0 to fix!\n".Pastel(ColorTheme.HighLight2));
            hex = "0" + hex;
        }

        return Enumerable.Range(0, hex.Length)
                         .Where(x => x % 2 == 0)
                         .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                         .ToArray();
        ;
    }

    public static byte[] CharToByteArray(char c)
    {
        if (c == 0x00)
            return new byte[] { 0x00 };
        List<byte> bytes = new List<byte>();
        while (c != 0)
        {
            bytes.Add((byte)(c & 0xff));
            c >>= 8;
        }
        return bytes.ToArray();
    }
}

