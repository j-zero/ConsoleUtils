using Pastel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace bits
{
    internal class Program
    {
        static long firstTick = 621355968000000000;

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
                
            }

            if (cmd.HasFlag("help"))
                ShowHelp();

            string data = null;
            bool success = false;
            long value = 0;

            if (cmd["data"].Strings.Length > 0 && cmd["data"].Strings[0] != null)
                data = cmd["data"].Strings[0].ToLower();

            if(data == null)
                ShowHelp();

            if (data.ToLower().StartsWith("0x") || (IsHex(data) && data.ToLower().Any(c => new char[] { 'a', 'b', 'c', 'd', 'e', 'f' }.Contains(c))))
            {
                // hex
                Console.WriteLine("Format detected: hex");

                if (data.ToLower().StartsWith("0x"))
                    success = long.TryParse(data.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out value);
                else
                    success = long.TryParse(data, System.Globalization.NumberStyles.HexNumber, null, out value);
            }


            else if (data.All(Char.IsDigit) && data.StartsWith("0") && data.All(c => c >= '0' && c <= '7'))
            {
                Console.WriteLine("Format detected: octal");
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
            else if((data.ToLower().StartsWith("0b") && data.Substring(2).All(c => c == '0' || c == '1')) || data.Length >= 8 && data.All(c => c == '0' || c == '1') )
            {
                // dual
                Console.WriteLine("Format detected: binary");
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
            else if (data.All(Char.IsDigit) || (data.StartsWith("-") && data.Substring(1).All(Char.IsDigit)))
            {
                // decimal/octal
                Console.WriteLine("Format detected: decimal");
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
                Console.WriteLine("???");
            }

            double bits = Math.Ceiling(Math.Log(value, 2));
            double maxBits = Math.Pow(2,bits);

            string hexValue = StringHelper.AddSeperator(value.ToString("X").ToLower(), " ", 2);
            string binaryValue = StringHelper.AddSeperator(Convert.ToString(value, 2), " ", 4);
            string octalValue = Convert.ToString(value, 8);
            string decimalValue = StringHelper.AddSeperator(value.ToString(), ".", 3);
            string bitsValue = bits.ToString();

            Console.WriteLine();

            Console.WriteLine($"decimal: {decimalValue}");
            Console.WriteLine($"hex    : {hexValue}");
            Console.WriteLine($"octal  : {octalValue}");
            Console.WriteLine($"binary : {binaryValue}");


            Console.WriteLine($"bits   : {bitsValue} ({maxBits})");


            if (value <= 0xffffffff && value > 0)
            {
                Byte
                    a = (byte)((value >> 24) & 0xFF),
                    b = (byte)((value >> 16) & 0xFF),
                    g = (byte)((value >> 8) & 0xFF),
                    r = (byte)((value >> 0) & 0xFF);

                if (data.Length == 7) // wenn kein alpha angegeben 255
                    a = 0xff;

                Console.WriteLine($"RGB    : {r},{g},{b}; Alpha {a}  " + "██████".Pastel(Color.FromArgb(r, g, b)));
            }

            if (value > 0 && (value > DateTime.MinValue.Ticks && value < DateTime.MaxValue.Ticks))
            {
                Console.Write("ticks  : ");

                DateTime dt = new DateTime(value);
                TimeSpan ts = new TimeSpan(value);


                Console.WriteLine($"{dt.ToShortDateString()} {dt.ToLongTimeString()} ({ToReadableString(ts)})");


                DateTime unixTime = UnixTimeStampToDateTime(value);
                if (unixTime.Ticks != 0)
                {
                    Console.Write("unix   : ");
                    Console.WriteLine($"{unixTime.ToShortDateString()} {unixTime.ToLongTimeString()}");
                }



            }

            Exit(0);
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

        static void ShowHelp()
        {
            Console.WriteLine($"{System.AppDomain.CurrentDomain.FriendlyName}, {ConsoleHelper.GetVersionString()}");
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} data");
            Console.WriteLine($"Options:");
            foreach (CmdOption c in cmd.OrderBy(x => x.Name))
            {
                string l = $"  --{c.Name}".Pastel("9CDCFE") + (!string.IsNullOrEmpty(c.ShortName) ? $", {("-" + c.ShortName).Pastel("9CDCFE")}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel("569CD6")).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
            Exit(0);
        }
        static void Exit(int exitCode)
        {
            string parrentProcess = ConsoleUtilsCore.ParentProcessUtilities.GetParentProcess().ProcessName;
            if (System.Diagnostics.Debugger.IsAttached || parrentProcess.ToLower().Contains("explorer")) // is debugger attached or started by double-click/file-drag
            {
                Console.WriteLine("\nPress any key to exit.");
                Console.ReadKey();
            }
            Environment.Exit(exitCode);
        }
    }
}
