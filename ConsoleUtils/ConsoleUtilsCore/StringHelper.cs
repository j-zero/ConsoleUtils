using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class StringHelper
{
    public static string Reverse(string s)
    {
        char[] charArray = s.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }

    public static string AddSeperator(string input, string seperator, int count)
    {
        string result = "";
        string workStr = Reverse(input);

        int i = 0;
        while(workStr.Length> 0)
        {
            i++;
            result += workStr[0];
            workStr = workStr.Remove(0, 1);
            if (i == count && workStr.Length > 0)
            {
                result += seperator;
                i = 0;
            }
        }

        return Reverse(result);
    }
}

