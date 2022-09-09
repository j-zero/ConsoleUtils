using Pastel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bits
{
    internal class Program
    {

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

            if (data.ToLower().StartsWith("0x") || data.ToLower().Any(c => new char[] {'a','b','c','d','e','f'}.Contains(c)))
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
            else if (data.All(Char.IsDigit))
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



            string hexValue = value.ToString("X").ToLower();
            string binaryValue = Convert.ToString(value, 2);
            string octalValue = Convert.ToString(value, 8);
            string decimalValue = value.ToString();

            Console.WriteLine($"decimal: {value}");
            Console.WriteLine($"hex    : {hexValue}");
            Console.WriteLine($"octal  : {octalValue}");
            Console.WriteLine($"binary : {binaryValue}");
        }


        static void ShowHelp()
        {
            Console.WriteLine($"bits, {ConsoleHelper.GetVersionString()}");
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} data");
            Console.WriteLine($"Options:");
            foreach (CmdOption c in cmd.OrderBy(x => x.Name))
            {
                string l = $"  --{c.Name}".Pastel("9CDCFE") + (!string.IsNullOrEmpty(c.ShortName) ? $", {("-" + c.ShortName).Pastel("9CDCFE")}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel("569CD6")).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
            //WriteError("Usage: subnet [ip/cidr|ip/mask|ip number_of_hosts]");
            Environment.Exit(0);
        }
    }
}
