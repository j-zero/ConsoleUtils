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
                { "in", "", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, null }
                }, "input format" },
                { "out", "", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, null }
                }, "output format" },
                { "data", "d", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, null }
                }, "input data" }
            };
            cmd.DefaultParameter = "data";
            cmd.Parse();

            string data = null;

            if (cmd["data"].Strings.Length > 0 && cmd["data"].Strings[0] != null)
                data = cmd["data"].Strings[0].ToLower();

            if (data.StartsWith("0x"))
            {
                // hex
            }
            else if (data.All(Char.IsDigit))
            {
                // dual
            }
            else if(data.All(c => c == 0x30 || c == 0x31))
            {
                // dual
            }
            else
            {

            }
        }
    }
}
