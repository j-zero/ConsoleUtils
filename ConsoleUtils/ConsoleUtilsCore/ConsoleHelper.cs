using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pastel;
using System.Drawing;


public class ConsoleHelper
{
    public static void WriteError(string message)
    {
        string doggo = "            \\\n             \\\n            /^-----^\\\n            V  o o  V\n             |  Y  |\n              \\ Q /\n              / - \\\n              |    \\\n              |     \\     )\n              || (___\\====";
        string msg = message.Length < 12 ? message.PadLeft(11) : message;
        Console.Error.Write($"\n   {msg.Pastel(Color.Salmon)}\n{doggo.Pastel(Color.White)}\n\n");
    }
    public static string GetVersionString()
    {
        return "ConsoleUtils (https://github.com/j-zero/ConsoleUtils)";
    }
}

