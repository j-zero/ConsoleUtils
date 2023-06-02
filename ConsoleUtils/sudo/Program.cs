using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace sudo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var fileName = "";
            var parameters = "";
            if(args.Length == 0)
            {
                //Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} {{command line}}\n  If no parameter is given, the parent process will be startet as administrator");
                var pid = ConsoleUtilsCore.ParentProcessUtilities.GetParentProcess().Id;
                string arguments = GetCommandLine(pid);

                var parts = Regex.Matches(arguments, @"[\""].+?[\""]|[^ ]+")
                    .Cast<Match>()
                    .Select(m => m.Value)
                    .ToList();

                fileName = parts[0];
                parameters = string.Join(" ", args.Skip(1).ToArray());


                //return;
            }
            else if(args.Length > 0)
            {
                fileName = args[0];

                if (args.Length > 1)
                    parameters = string.Join(" ", args.Skip(1).ToArray());

            }


            if (RunAsAdmin(fileName, parameters))
                Environment.Exit(0);
            else
                Environment.Exit(255);



        }

        private static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static bool RunAsAdmin(string file, string arguments)
        {
            var proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Environment.CurrentDirectory;
            proc.FileName = file;
            proc.Arguments = arguments;
            proc.Verb = "runas";

            try
            {
                Process.Start(proc);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string GetCommandLine(int pid)
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + pid.ToString()))
            using (ManagementObjectCollection objects = searcher.Get())
            {
                return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
            }
        }

    }
}
