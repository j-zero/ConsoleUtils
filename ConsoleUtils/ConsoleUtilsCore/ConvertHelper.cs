using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ConvertHelper
{
    // https://stackoverflow.com/a/321404
    public static byte[] StringToByteArray(string input) // slow as fuck, but works
    {
        string hex = input.Replace(" ", "").Replace("0x", "").Replace(",", "");

        return Enumerable.Range(0, hex.Length)
                            .Where(x => x % 2 == 0)
                            .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                            .ToArray();
    }
}

