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

namespace coffee
{
    internal class Program
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

        static CmdParser cmd;

        void PreventSleep()
        {
            // Prevent Idle-to-Sleep (monitor not affected) (see note above)
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_AWAYMODE_REQUIRED);
        }

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

            bool somethingSet = false;

            if (cmd.HasFlag("help"))
                ShowHelp();

            if (cmd.HasFlag("topmost"))
            {
                somethingSet = true;
                //Console.Error.WriteLine($"Set window always on top.");
                WindowHelper.SetCurrentWindowTopMost(true);
            }
            else if (cmd.HasFlag("no-topmost"))
            {
                somethingSet = true;
                //Console.Error.WriteLine($"Set window not always on top.");
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
            Console.WriteLine($"coffee, {ConsoleHelper.GetVersionString()}");
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [Options] [[--start] [command] [arguments]]");
            Console.WriteLine($"Options:");
            foreach (CmdOption c in cmd.OrderBy(x => x.Name))
            {
                string l = $"  --{c.Name}".Pastel("9CDCFE") + (!string.IsNullOrEmpty(c.ShortName) ? $", {("-" + c.ShortName).Pastel("9CDCFE")}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel("569CD6")).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
            //WriteError("Usage: subnet [ip/cidr|ip/mask|ip number_of_hosts]");
            Exit(0);
        }


        static void Exit(int exitCode)
        {
            string parrentProcess = ConsoleUtilsCore.ParentProcessUtilities.GetParentProcess().ProcessName;
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
