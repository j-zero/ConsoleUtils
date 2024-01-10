using System;
using System.Collections.Generic;

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

    public static string PadLeftToBlocks(string value, int blocksize, char paddingchar, string seperator)
    {
        int pads = value.Length % blocksize;
        string result = (pads > 0 ? string.Empty.PadLeft(blocksize - pads, paddingchar) : string.Empty) + value;
        return AddSeperator(result, seperator, blocksize);
    }

    public static int CountWords(string text)
    {
        int wordCount = 0, index = 0;

        // skip whitespace until first word
        while (index < text.Length && char.IsWhiteSpace(text[index]))
            index++;

        while (index < text.Length)
        {
            // check if current char is part of a word
            while (index < text.Length && !char.IsWhiteSpace(text[index]))
                index++;

            wordCount++;

            // skip whitespace until next word
            while (index < text.Length && char.IsWhiteSpace(text[index]))
                index++;
        }

        return wordCount;
    }

    public static List<string> SplitInParts(String s, Int32 partLength)
    {
        List<string> result =   new List<string>();

        if (s == null)
            throw new ArgumentNullException(nameof(s));
        if (partLength <= 0)
            throw new ArgumentException("Part length has to be positive.", nameof(partLength));

        for (var i = 0; i < s.Length; i += partLength)
            result.Add(s.Substring(i, Math.Min(partLength, s.Length - i)));

        return result;
    }
}

