using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Pastel;

[assembly: System.Reflection.AssemblyVersion("0.3.*")]
namespace coffee
{
    internal class coffee
    {
        [FlagsAttribute]
        public enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
            // Legacy flag, should not be used.
            // ES_USER_PRESENT = 0x00000004
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        static string color1 = "#e7bc91";
        static string color2 = "#bc8a5f";
        static CmdParser cmd;


        static void Main(string[] args)
        {

            Console.CancelKeyPress += Console_CancelKeyPress;

            cmd = new CmdParser(args)
            {
                { "help", "", CmdCommandTypes.FLAG, "Show this help." },
                { "no-sleep", "", CmdCommandTypes.FLAG, "Prevent Idle-to-Sleep" },
                { "awake", "", CmdCommandTypes.FLAG, "Prevent idle (default)" },
                { "start", "s", CmdCommandTypes.MULTIPE_PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Start Process and keep display alive" },
                { "use-shell-execute", "", CmdCommandTypes.FLAG, "use shell execute on --start" },
                { "topmost", "", CmdCommandTypes.FLAG, "Set window topmost" },
                { "no-topmost", "", CmdCommandTypes.FLAG, "Set window notopmost" },

            };

            cmd.DefaultParameter = "start";
            cmd.Parse();

            if (cmd.HasFlag("help"))
                ShowHelp();
            else
                ShowVersion();

            if (cmd.HasFlag("topmost"))
            {
                WindowHelper.SetCurrentWindowTopMost(true);
            }
            else if (cmd.HasFlag("no-topmost"))
            {
                WindowHelper.SetCurrentWindowTopMost(false);
            }

            if (cmd.HasFlag("no-sleep")){
                SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_AWAYMODE_REQUIRED);
                //Console.Error.Write($"Prventing Idle-to-sleep ... ");
                Wait();
            }
            else if (cmd["start"].Strings.Length > 0 && cmd["start"].Strings[0] != null)
            {

                string command = cmd["start"].Strings[0];
                
                string[] arguments = cmd["start"].Strings.Skip(1).ToArray();

                Console.Error.WriteLine($"staying awake ...");
                SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_AWAYMODE_REQUIRED);
                
                
                var psi = new ProcessStartInfo(command, string.Join(" ", arguments));
                psi.UseShellExecute = cmd.HasFlag("use-shell-execute");

                var proc = Process.Start(psi);
                proc.WaitForExit();
                if (cmd.HasFlag("topmost"))
                    WindowHelper.SetCurrentWindowTopMost(false);

                //Console.Error.Write($"Staying awake ... ");

            }
            else if (cmd.Empty || cmd.HasFlag("awake"))
            {
                SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_DISPLAY_REQUIRED );
                Console.Error.Write($"staying awake ... ");
                Wait();
            }



            




        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            //Console.Error.Write($"Have a nive day!");
            if (cmd.HasFlag("topmost"))
                WindowHelper.SetCurrentWindowTopMost(false);
        }

        static void Wait()
        {
            Console.WriteLine($"press Enter to exit.");
            ConsoleKey key = ConsoleKey.NoName;
            while (key != ConsoleKey.Enter)
            {
                key = Console.ReadKey().Key;
            }
        }

        static void ShowHelp()
        {
            ShowVersion();
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName.Pastel(color1)} [{"Options".Pastel(color2)}] [[{"--start".Pastel(color2)}] [{"command".Pastel(color2)}] [{"arguments".Pastel(color2)}]]");
            Console.WriteLine($"Example: {AppDomain.CurrentDomain.FriendlyName.Pastel(color1)} {"--topmost".Pastel(color2)} {"--start".Pastel(color2)} ping -t 192.168.0.1");
            Console.WriteLine($"{"Options".Pastel(color2)}:");
            foreach (CmdOption c in cmd.OrderBy(x => x.Name))
            {
                string l = $"  --{c.Name}".Pastel(color1) + (!string.IsNullOrEmpty(c.ShortName) ? $", {("-" + c.ShortName).Pastel(color1)}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel(color2)).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
            //WriteError("Usage: subnet [ip/cidr|ip/mask|ip number_of_hosts]");
            Exit(0);
        }

        static void ShowVersion()
        {
            string version_string = ("v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + "").PadLeft(0);
            /*
            Console.WriteLine(@"          __  __         ".Pastel("#d6ab7d"));
            Console.WriteLine(@"  __ ___ / _|/ _|___ ___ ".Pastel("#b3895d"));
            Console.WriteLine(@" / _/ _ \  _|  _/ -_) -_)".Pastel("#9b744a"));
            Console.WriteLine(@" \__\___/_| |_| \___\___| ".Pastel("#81583a"));
            */
            Console.WriteLine(@"▄█▄    ████▄ ▄████  ▄████  ▄███▄   ▄███▄   ".Pastel("#e7bc91"));
            Console.WriteLine(@"█▀ ▀▄  █   █ █▀   ▀ █▀   ▀ █▀   ▀  █▀   ▀  ".Pastel("#d4a276"));
            Console.WriteLine(@"█   ▀  █   █ █▀▀    █▀▀    ██▄▄    ██▄▄    ".Pastel("#bc8a5f"));
            Console.WriteLine(@"█▄  ▄▀ ▀████ █      █      █▄   ▄▀ █▄   ▄▀ ".Pastel("#a47148"));
            Console.WriteLine(@"▀███▀         █      █     ▀███▀   ▀███▀   ".Pastel("#8b5e34"));
            Console.WriteLine((@"               ▀      ▀ " + version_string.Pastel("#8b5e34")).Pastel("#6f4518"));

            Console.WriteLine($"{"coffee".Pastel(color1)} is part of " + ConsoleHelper.GetVersionString(color2, color2));
        }


        static void Exit(int exitCode)
        {
            string parrentProcess = windows.core.ParentProcessUtilities.GetParentProcess().ProcessName;
            //Console.WriteLine(parrentProcess);

            if (System.Diagnostics.Debugger.IsAttached || parrentProcess.ToLower().Contains("explorer")) // is debugger attached or started by double-click/file-drag
            {
                Console.WriteLine("\nPress any key to exit.");
                Console.ReadKey();
            }

            Environment.Exit(exitCode);
        }
    }
}
