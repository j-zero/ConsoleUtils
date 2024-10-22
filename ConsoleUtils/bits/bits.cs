using Pastel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Buffers.Binary;
using static System.Runtime.InteropServices.JavaScript.JSType;


[assembly: System.Reflection.AssemblyVersion("0.3.*")]
namespace bits
{
    internal class bits
    {
        // static long firstTick = 621355968000000000;

        static double value = 0;
        //static long int_value = 0;

        static bool isBits = false;
        static string color1 = "#2dc653";
        static string color2 = "#208b3a";
        static int padRight = 16;

        enum DataTypes
        {
            Decimal,
            Hexadecimal,
            Octal,
            Dual
        }

        static CmdParser cmd;
        static void Main(string[] args)
        {


            cmd = new CmdParser(args)
            {
                { "help", "", CmdCommandTypes.FLAG, "Show this help." },
                { "data", "d", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, null }
                }, "input data" }
            };

            cmd.DefaultParameter = "data";
            try
            {
                cmd.Parse();
            }
            catch (ArgumentException ex)
            {
                ConsoleHelper.WriteError(ex.Message);
                Exit(1);
            }

            if (cmd.HasFlag("help"))
                ShowLongHelp();

            string data = null;
            


            if (cmd["data"].Strings.Length > 0 && cmd["data"].Strings[0] != null)
                data = cmd["data"].Strings[0];

            if (data == null)
            {
                ShowVersion();
                while (true)
                {
                    Console.Write("> ".Pastel(color1));
                    data = Console.ReadLine();
                    Parse(data);

                }
            }
            else
            {
                Parse(data);
            }

            Exit(0);
        }

        public static bool Parse(string data)
        {
            bool success = false;
            if (data.ToLower().StartsWith("0x") || data.ToLower().StartsWith("#") || (IsHex(data) && data.ToLower().Any(c => new char[] { 'a', 'b', 'c', 'd', 'e', 'f' }.Contains(c))))
            {
                // hex

                if ((data.ToLower().StartsWith("0b") && data.Substring(2).All(c => c == '0' || c == '1')))
                {
                    if (data.ToLower().StartsWith("0b"))
                        data = data.Substring(2);
                    // dual
                    Console.WriteLine($"{"interpretation".PadRight(padRight).Pastel(color1)}: binary number");
                    try
                    {
                        value = Convert.ToInt64(data, 2);
                        success = true;
                    }
                    catch
                    {
                        success = false;
                    }
                }
                else if (data.ToLower().StartsWith("0x"))
                {
                    string hexdata = data.Substring(2);
                    ulong va = 0;
                    success = ulong.TryParse(hexdata, System.Globalization.NumberStyles.HexNumber, null, out va);
                    if (success)
                        Console.WriteLine($"{"interpretation".PadRight(padRight).Pastel(color1)}: hex number");
                    value = (double)va;
                }
                else if (data.ToLower().StartsWith("#"))
                {
                    string hexdata = data.Substring(2);
                    ulong va = 0;
                    success = ulong.TryParse(hexdata, System.Globalization.NumberStyles.HexNumber, null, out va);
                    if (success)
                        Console.WriteLine($"{"interpretation".PadRight(padRight).Pastel(color1)}: hex number");
                    value = (double)va;
                }
                else
                {
                    long long_Val;
                    success = long.TryParse(data, System.Globalization.NumberStyles.HexNumber, null, out long_Val);
                    value = (double)long_Val;
                }
            }

            else if (HasSISuffix(data, out double si_interpret_val, out string suffix))
            {
                try
                {
                    int l = Array.FindIndex(UnitHelper.SizeSuffixes, x => x.ToLower() == suffix.ToLower());

                    string unit_name = UnitHelper.SISuffixNames[suffix.ToLower()];
                    Console.WriteLine($"{"interpretation".PadRight(padRight).Pastel(color1)}: {si_interpret_val} {unit_name}, Number with SI Prefix");

                    for (int i = 0; i < l; i++)
                        si_interpret_val *= 1000;

                    value = si_interpret_val;
                    success = true;
                }
                catch { success = false; }
            }

            else if (HasByteSuffix(data, out double byte_interpret_value, out string byte_suffix, out isBits))
            {
                try
                {
                    string s = byte_suffix.ToLower().Replace("i", "").Replace("b", "");
                    int l = Array.FindIndex(UnitHelper.SizeSuffixes, x => x.ToLower() == s);


                    string unit = l == -1 ? "" : UnitHelper.ByteSuffixes[l];


                    //bool isBin = false;

                    string unit_name = "";


                    int c = 1000;
                    if (byte_suffix.ToLower().Contains("i"))
                    {
                        c = 1024;
                        //isBin = true;
                        unit_name = UnitHelper.BinSuffixNames[unit.ToLower()];
                        Console.WriteLine($"{"interpretation".PadRight(padRight).Pastel(color1)}: {byte_interpret_value} {unit_name}" + (isBits ? "bits" : "bytes") + " (binary unit prefix, base 2)");
                    }
                    else
                    {
                        unit_name = UnitHelper.SISuffixNames[unit.ToLower()];
                        Console.WriteLine($"{"interpretation".PadRight(padRight).Pastel(color1)}: {byte_interpret_value} {unit_name}" + (isBits ? "bits" : "bytes") + " (SI unit prefix, base 10)");
                    }

                    //Console.WriteLine($"Assuming {val} {unit_name}" + (isBits ? "bits" : "Bytes"));

                    for (int i = 0; i < l; i++)
                        byte_interpret_value *= c;


                    value = byte_interpret_value;

                    success = true;
                }
                catch { success = false; }
            }


            else if (data.All(Char.IsDigit) && data.StartsWith("0") && data.All(c => c >= '0' && c <= '7'))
            {
                Console.WriteLine($"{"interpretation".PadRight(padRight).Pastel(color1)}: octal number");
                try
                {
                    value = Convert.ToInt64(data, 8);
                    success = true;
                }
                catch
                {
                    success = false;
                }
            }
            
            else if ((data.ToLower().StartsWith("0i") && data.Substring(2).All(Char.IsDigit)) || data.All(Char.IsDigit) || (data.StartsWith("-") && data.Substring(1).All(Char.IsDigit)))
            {
                if(data.ToLower().StartsWith("0i"))
                    data = data.Substring(2);

                // decimal/octal
                Console.WriteLine($"{"interpretation".PadRight(padRight).Pastel(color1)}: decimal number");
                try
                {
                    value = Convert.ToInt64(data, 10);
                    success = true;
                }
                catch
                {
                    success = false;
                }
            }
            else
            {
                try
                {
                    byte[] bytes = ConvertHelper.HexStringToByteArray(data);
                }
                catch
                {
                    ParseString(data);
                    success = true;
                }

                
                //Console.WriteLine("???".Pastel(color2));

                return false;
            }


            if (!success)
                Die("Can't parse input.",255);

            long int_value = (long)value;

            double bits = Math.Ceiling(Math.Log((double)value, 2));
            double maxBits = Math.Pow(2, bits);

            // string hexValue = StringHelper.AddSeperator(value.ToString("X").ToLower(), " ", 2);


            //string binaryValue = StringHelper.AddSeperator(Convert.ToString(value, 2), " ", 4);

            string decimalValue = StringHelper.AddSeperator(value.ToString(), ".", 3);
            string bitsValue = bits.ToString();

            Console.WriteLine();

            Console.WriteLine($"{"decimal".PadRight(padRight).Pastel(color1)}: {value} ({decimalValue})");

            if (value % 1 == 0)
            {
                string hexValue = int_value.ToString("X").ToLower();
                string hexString = StringHelper.PadLeftToBlocks(hexValue, 2, '0', " ");
                string revEndian = BinaryPrimitives.ReverseEndianness(int_value).ToString("X").ToLower();
                string revEndhexString = StringHelper.PadLeftToBlocks(revEndian, 2, '0', " ");

                string binaryValue = StringHelper.PadLeftToBlocks(Convert.ToString(int_value, 2), 4, '0', " ");
                string octalValue = Convert.ToString(int_value, 8);
                Console.WriteLine($"{"hex".PadRight(padRight).Pastel(color1)}: {hexString} ({hexValue})");
                Console.WriteLine($"{"hex rev. endian".PadRight(padRight).Pastel(color1)}: {revEndhexString} ({revEndian})");
                Console.WriteLine($"{"octal".PadRight(padRight).Pastel(color1)}: {octalValue}");
                Console.WriteLine($"{"binary".PadRight(padRight).Pastel(color1)}: {binaryValue}");
            }


            Console.WriteLine($"{"bits".PadRight(padRight).Pastel(color1)}: {bitsValue} (2^{bitsValue} = {maxBits}, +{maxBits - (double)value})");



            if (value % 1 == 0 && value <= 0xffffffff && value > 0)
            {

                Byte
                    a = (byte)((int_value >> 24) & 0xFF),
                    r = (byte)((int_value >> 16) & 0xFF),
                    g = (byte)((int_value >> 8) & 0xFF),
                    b = (byte)((int_value >> 0) & 0xFF);

                if (data.Length == 7) // wenn kein alpha angegeben 255
                    a = 0xff;

                string hexRGB = int_value.ToString("X").ToLower().PadLeft((value > 0xffffff ? 8 : 6), '0');

                Console.WriteLine($"{"color".PadRight(padRight).Pastel(color1)}: {hexRGB}; RGB {r}, {g}, {b} (Alpha {a}); {"██████".Pastel(Color.FromArgb(r, g, b))}");
            }

            if (value % 1 == 0 && value > 0 && (value > DateTime.MinValue.Ticks && value < DateTime.MaxValue.Ticks))
            {
                Console.Write($"{"ticks".PadRight(padRight).Pastel(color1)}: ");

                DateTime dt = new DateTime(int_value);
                TimeSpan ts = new TimeSpan(int_value);


                Console.WriteLine($"{dt.ToShortDateString()} {dt.ToLongTimeString()} ({ToReadableString(ts)})");


                DateTime unixTime = UnixTimeStampToDateTime(int_value);
                if (unixTime.Ticks != 0)
                {
                    Console.Write($"{"unix".PadRight(padRight).Pastel(color1)}: ");
                    Console.WriteLine($"{unixTime.ToShortDateString()} {unixTime.ToLongTimeString()}");
                }



            }

            // TODO SI niut prefixes

            // Size
            Console.WriteLine();

            var maxSuffixB = (int)Math.Log(int_value, 1024) + 1;
            var maxSuffixSI = (int)Math.Log(int_value, 1000) + 1;

            long bit_value = 0;
            long byte_value = 0;




            if (isBits)
            {
                bit_value = int_value;
                byte_value = (long)value / 8;
            }
            else
            {
                bit_value = int_value * 8;
                byte_value = (long)value;
            }


            for (int i = 0; i < maxSuffixSI; i++)
            {

                (string sizeIValue, string sizeISuffix) = UnitHelper.GetHumanReadableSize(byte_value, 1024, 2, false, i);
                (string sizeValue, string sizeSuffix) = UnitHelper.GetHumanReadableSize(byte_value, 1000, 2, false, i);
                (string bit_sizeIValue, string bit_sizeISuffix) = UnitHelper.GetHumanReadableSize(bit_value, 1024, 2, false, i);
                (string bit_sizeValue, string bit_sizeSuffix) = UnitHelper.GetHumanReadableSize(bit_value, 1000, 2, false, i);
                //Console.WriteLine($"size   : {sizeValue} {sizeSuffix}B, {sizeIValue} {sizeISuffix}iB");

                //unit_name = UnitHelper.BinSuffixNames[unit.ToLower()];

                if (i == 0)
                {
                    Console.WriteLine($"{"size".PadRight(padRight).Pastel(color1)}: {sizeValue} Bytes, {bit_sizeValue} bits");
                }
                else
                {

                    Console.WriteLine($"{"".PadRight(padRight)}  {sizeValue} {UnitHelper.SISuffixNames[sizeSuffix.ToLower()]}bytes, {sizeIValue} {UnitHelper.BinSuffixNames[sizeISuffix.ToLower()]}bytes, {bit_sizeValue} {UnitHelper.SISuffixNames[bit_sizeSuffix.ToLower()]}bits, {bit_sizeIValue} {UnitHelper.BinSuffixNames[bit_sizeSuffix.ToLower()]}bits");
                }
            }
            return success;
        }

        public static bool HasByteSuffix(string val, out double out_val, out string suffix, out bool bits)
        {
            out_val = 0;
            suffix = null;
            bits = false;

            //Regex regex = new Regex(@"(\d+(?:[\.,]\d+)?)((?:[kKMGTPEZY]i?)(b(?:its)?|B(?:yte))?)");
            Regex regex = new Regex(@"(\d+(?:[\.,]\d+)?)([kKMGTPEZY]i?)(([Bb])(?:it|yte)?s?)$");
            Match match = regex.Match(val);
            if (match.Success)
            {
                var one = match.Groups[1].Value;
                var bits_bytes = match.Groups[3].Value;
                if(bits_bytes.Contains("b") || bits_bytes.ToLower().Contains("bit"))
                    bits = true;
                if (!double.TryParse(one, NumberStyles.Any, CultureInfo.InvariantCulture, out out_val))
                    return false;

                suffix = match.Groups[2].Value;
                return true;
            }
            return false;
        }

        public static bool HasSISuffix(string val, out double out_val, out string suffix)
        {
            out_val = 0;
            suffix = null;

            Regex regex = new Regex(@"(\d+(?:[\.,]\d+)?)([kKMGTPEZYRQ])$");
            Match match = regex.Match(val);
            if (match.Success)
            {
                var one = match.Groups[1].Value;

                if (!double.TryParse(one, NumberStyles.Any, CultureInfo.InvariantCulture, out out_val))
                    return false;

                suffix = match.Groups[2].Value;
                return true;
            }
            return false;
        }

        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            try
            {
                DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
                return dateTime;
            }
            catch
            {
                return new DateTime(0);
            }
        }

        public static string ToReadableString(TimeSpan span)
        {
            string formatted = string.Format("{0}{1}{2}{3}{4}",
                span.Duration().Days > 0 ? string.Format("{0:0} day{1} ", span.Days, span.Days == 1 ? string.Empty : "s") : string.Empty,
                span.Duration().Hours > 0 ? string.Format("{0:0} hour{1} ", span.Hours, span.Hours == 1 ? string.Empty : "s") : string.Empty,
                span.Duration().Minutes > 0 ? string.Format("{0:0} minute{1} ", span.Minutes, span.Minutes == 1 ? string.Empty : "s") : string.Empty,
                span.Duration().Seconds > 0 ? string.Format("{0:0} second{1} ", span.Seconds, span.Seconds == 1 ? string.Empty : "s") : string.Empty,
                span.Duration().Milliseconds > 0 ? string.Format("{0:0} millisecond{1}", span.Milliseconds, span.Milliseconds == 1 ? string.Empty : "s") : string.Empty);

            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

            if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";

            return formatted;
        }

        private static bool IsHex(IEnumerable<char> chars)
        {
            bool isHex;
            foreach (var c in chars)
            {
                isHex = ((c >= '0' && c <= '9') ||
                         (c >= 'a' && c <= 'f') ||
                         (c >= 'A' && c <= 'F'));

                if (!isHex)
                    return false;
            }
            return true;
        }

        public static string ToRoman(int number)
        {
            if ((number < 0) || (number > 3999)) return "n/a";
            if (number < 1) return string.Empty;
            if (number >= 1000) return "M" + ToRoman(number - 1000);
            if (number >= 900) return "CM" + ToRoman(number - 900);
            if (number >= 500) return "D" + ToRoman(number - 500);
            if (number >= 400) return "CD" + ToRoman(number - 400);
            if (number >= 100) return "C" + ToRoman(number - 100);
            if (number >= 90) return "XC" + ToRoman(number - 90);
            if (number >= 50) return "L" + ToRoman(number - 50);
            if (number >= 40) return "XL" + ToRoman(number - 40);
            if (number >= 10) return "X" + ToRoman(number - 10);
            if (number >= 9) return "IX" + ToRoman(number - 9);
            if (number >= 5) return "V" + ToRoman(number - 5);
            if (number >= 4) return "IV" + ToRoman(number - 4);
            if (number >= 1) return "I" + ToRoman(number - 1);
            return "n/a";
        }

        public static void ParseString(string data)
        {

            Console.WriteLine($"{"interpretation".Pastel(color1)}: string");
            Console.WriteLine($"{"chars".Pastel(color1)}: {data.Length}");
            Console.WriteLine($"{"words".Pastel(color1)}: {StringHelper.CountWords(data)}");
            Console.WriteLine($"{"rot13".Pastel(color1)}: {Rot13(data)}");
            Console.WriteLine();

            try
            {

                byte[] bse64Data = FormatAndDecodeMalformedBase64(data, out string fixed_input);
                if (bse64Data.Length > 0)
                {
                    Console.WriteLine($"{"interpretation".Pastel(color1)}: base64 encoded data");
                    if (fixed_input != null)
                    {
                        Console.WriteLine($"{"info".Pastel(color1)}: base64 had bad padding, fixed to: ");
                        Console.WriteLine($"{fixed_input.Pastel(ColorTheme.OffsetColor)}");
                    }
                    Console.WriteLine($"{"content length".Pastel(color1)}: {bse64Data.Length}");
                    Console.WriteLine($"{"content".Pastel(color1)}: ");
                    ConsoleHelper.HexDump(bse64Data);

                }

            }
            catch
            {
                
            }



        }

        public static byte[] FormatAndDecodeMalformedBase64(string input, out string fixed_input)
        {
            char pad = '=';
            int padLength = 0;
            fixed_input = null;

            if (input.Length % 4 != 0) // try fix padding
            {
                padLength = input.Length + (input.Length % 4);
                input = input.PadRight(padLength, pad);
                fixed_input = input;
            }

            try
            {

                byte[] bse64Data = Convert.FromBase64String(input);

                return bse64Data;
            }
            catch
            {
                return new byte[0];
            }

        }

        // https://stackoverflow.com/a/18739120
        public static string Rot13(string input) => Regex.Replace(input, "[a-zA-Z]", new MatchEvaluator(c => ((char)(c.Value[0] + (Char.ToLower(c.Value[0]) >= 'n' ? -13 : 13))).ToString()));

        static void ShowHelp(bool more = true)
        {
            ShowVersion();
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName.Pastel(color1)} [{"Options".Pastel(color2)}] {{\"file\"}}\n");
            if (more)
                Console.WriteLine($"For more options, use {"--help".Pastel(color1)}");
        }
        static void ShowLongHelp()
        {
            ShowHelp(false);
            Console.WriteLine($"\n{"Options".Pastel(color2)}:");
            foreach (CmdOption c in cmd.OrderBy(x => x.Name))
            {
                string l = $"  --{c.Name}".Pastel(color1) + (!string.IsNullOrEmpty(c.ShortName) ? $", {("-" + c.ShortName).Pastel(color1)}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel(color2)).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
            //WriteError("Usage: subnet [ip/cidr|ip/mask|ip number_of_hosts]");
            Environment.Exit(0);
        }
        static void ShowVersion()
        {
            string version_string = ("v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " ").PadLeft(0).Pastel(color2);
            Console.WriteLine(@"▄▄▄▄   ▀ ▄▄▄▄▄▄ ▄▄   ".Pastel("#2dc653"));
            Console.WriteLine(@"▐█ ▀█  ██  ██  ▐█ ▀  ".Pastel("#25a244"));
            Console.WriteLine(@"▐█▀▀█▄ ▐█  ▐█  ▄▀▀▀█▄".Pastel("#208b3a"));
            Console.WriteLine(@"██▄ ▐█ ▐█▌ ▐█▌ ▐█▄ ▐█".Pastel("#1a7431"));
            Console.WriteLine(@"·▀▀▀▀  ▀▀▀ ▀▀▀  ▀▀▀▀ ".Pastel("#155d27")+(version_string + @"").Pastel("#1a7431"));

            Console.WriteLine($"{"bits".Pastel(color1)} is part of " + ConsoleHelper.GetVersionString(color2, color2));
        }

        static void Exit(int exitCode)
        {

            /*
             * #if Linux
                Console.WriteLine("Built on Linux!"); 
                #elif Windows
                Console.WriteLine("Built in Windows!"); 
                #endif
            string parrentProcess = ConsoleUtilsCore.ParentProcessUtilities.GetParentProcess().ProcessName;
            if (System.Diagnostics.Debugger.IsAttached || parrentProcess.ToLower().Contains("explorer")) // is debugger attached or started by double-click/file-drag
            {
                Console.WriteLine("\nPress any key to exit.");
                Console.ReadKey();
            }
            */
            Environment.Exit(exitCode);
        }

        public static void Die(string msg, int errorcode)
        {
            //ShowVersion();
            ConsoleHelper.WriteError(msg);
            Environment.Exit(errorcode);
        }

    }
}
