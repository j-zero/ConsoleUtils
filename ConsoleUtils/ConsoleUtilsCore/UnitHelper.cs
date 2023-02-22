using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

    public class UnitHelper
    {
    public static readonly string[] SizeSuffixes = { "B", "k", "M", "G", "T", "P", "E", "Z", "Y" };

    // based on https://stackoverflow.com/a/14488941
    public static void _CalculateHumanReadableSize(Int64 value, out string _humanReadbleSize, out string _humanReadbleSizeSuffix, int factor = 1024, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            //if (value < 0) { return "-" + CalculateHumanReadableSize(-value, decimalPlaces); }
            if (value == 0)
            {
                _humanReadbleSize = "0";
                _humanReadbleSizeSuffix = "";
                return;
            }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, factor);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= factor;
            }

            _humanReadbleSize = string.Format("{0:n" + decimalPlaces + "}", adjustedSize);
            _humanReadbleSizeSuffix = SizeSuffixes[mag];
            /*
            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
            */
        }

    /*
    public static string CalculateHumanReadableSize(Int64 value, int factor = 1024, int decimalPlaces = 1)
    {
        ulong v = (ulong)Math.Abs(value);
        bool negativ = value < 0;
        string result = CalculateHumanReadableSize(value, factor, decimalPlaces);

        if (negativ)
            result = "-" + result;

        return result;
    }
    */

    // based on https://stackoverflow.com/a/14488941
    public static (string, string) GetHumanReadableSize(Int64 value, int factor = 1024, int decimalPlaces = 1, bool showByteSuffix = false)
    {
        if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }

        if (value == 0)
            return ("0", "");

        // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
        int mag = (int)Math.Log(value, factor);

        double val = value;

        for (int i = 0; i < mag; i++)
            val /= factor;

        if (mag == 0) decimalPlaces = 0; // no decimal points on bytes

        return (string.Format("{0:n" + decimalPlaces + "}", val), ((mag == 0 && !showByteSuffix) ? "" : SizeSuffixes[mag]));
    }

    public static (string, string) GetHumanReadableSize2(Int64 value, int factor = 1024, int decimalPlaces = 1, bool showByteSuffix = false)
    {
        if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }

        if (value == 0)
            return ("0", "");

        // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
        int mag = (int)Math.Log(value, factor);

        // 1L << (mag * 10) == 2 ^ (10 * mag) 
        // [i.e. the number of bytes in the unit corresponding to mag]
        decimal adjustedSize = (decimal)value / (1L << (mag * 10));

        // make adjustment when the value is large enough that
        // it would round up to 1000 or more
        if (Math.Round(adjustedSize, decimalPlaces) >= factor)
        {
            mag += 1;
            adjustedSize /= factor;
        }

        if (mag == 0) decimalPlaces = 0; // no decimal points on bytes

        return (string.Format("{0:n" + decimalPlaces + "}", adjustedSize), ((mag == 0 && !showByteSuffix) ? "" : SizeSuffixes[mag]));
    }

    public static string CalculateHumanReadableSize(UInt64 value, int factor = 1024, int decimalPlaces = 1, bool showByteSuffix = false)
    {
        string _humanReadbleSize, _humanReadbleSizeSuffix = string.Empty;

        if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
        //if (value < 0) { return "-" + CalculateHumanReadableSize(-value, decimalPlaces); }
        if (value == 0)
        {
            _humanReadbleSize = "0";
            _humanReadbleSizeSuffix = "";
            return "0";
        }

        // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
        int mag = (int)Math.Log(value, factor);

        // 1L << (mag * 10) == 2 ^ (10 * mag) 
        // [i.e. the number of bytes in the unit corresponding to mag]
        decimal adjustedSize = (decimal)value / (1 << (mag * 10));

        // make adjustment when the value is large enough that
        // it would round up to 1000 or more
        if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
        {
            mag += 1;
            adjustedSize /= factor;
        }

        _humanReadbleSize = string.Format("{0:n" + decimalPlaces + "}", adjustedSize);
        _humanReadbleSizeSuffix = (mag == 0 && !showByteSuffix) ? SizeSuffixes[mag] : "";

        return String.Format(CultureInfo.InvariantCulture,"{0:n" + decimalPlaces + "}{1}", adjustedSize, SizeSuffixes[mag]);

    }
 }

