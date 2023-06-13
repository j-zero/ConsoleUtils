using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Pastel;

namespace tulpn
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var table = TCPTable.GetAllTcpConnections();
            int maxDescLength = Console.WindowWidth - Console.CursorLeft - 8;
            foreach (var t in table)
            {
                var cmdline = GetCommandLine(t.owningPid);

                if (t.state == TCPTable.TCP_STATE.MIB_TCP_STATE_LISTEN)
                {
                    Console.Write($"{t.LocalIP.Pastel(ColorTheme.Default1)}{":".Pastel(ColorTheme.Default2)}{t.LocalPort.ToString().PadRight(5, ' ').Pastel(ColorTheme.Default1)}");
                    
                    // REMOTE
                    //Console.Write($"> {t.RemoteIP.Pastel(ColorTheme.Default1)}{":".Pastel(ColorTheme.Default2)}{t.RemotePort.ToString().PadRight(5, ' ').Pastel(ColorTheme.Default1)}");
                    if (cmdline != null)
                    {

                        Console.Write($"\n{cmdline}".Pastel(ColorTheme.Comment));
                        ///ConsoleHelper.WriteSplittedText(cmdline, maxDescLength, "   ", 4, ColorTheme.Comment);

                    }
                    Console.WriteLine();
                }
            }
            
            Console.ReadLine();
        }

        public static string GetCommandLine(int pid)
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + pid.ToString()))
            using (ManagementObjectCollection objects = searcher.Get())
            {
                return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
            }
        }

        public static string GetShortLine(string text, string suffix = "...")
        {
            int length = Math.Min(text.Length, Console.WindowWidth - Console.CursorLeft - suffix.Length);
            return text.Substring(0, length) + (length < text.Length ? suffix : "");
        }


    }
}
