using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace sudo
{
    internal class Program
    {

        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32", SetLastError = true)]
        public static extern bool DuplicateToken(IntPtr ExistingTokenHandle, int SECURITY_IMPERSONATION_LEVEL, ref IntPtr DuplicateTokenHandle);

        [DllImport("advapi32", SetLastError = true)]
        public static extern bool ImpersonateLoggedOnUser(IntPtr hToken);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

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

        public static void GetSystem()
        {
            if (IsAdministrator())
            {
                IntPtr hToken = IntPtr.Zero;

                Process[] processes = Process.GetProcessesByName("winlogon");
                IntPtr handle = processes[0].Handle;
                Console.WriteLine("[+] WinLogon handle: " + handle.ToString());
                bool success = OpenProcessToken(handle, 0x0002, out hToken);
                Console.WriteLine("[+] OpenProcessToken: " + success.ToString());
                IntPtr hDupToken = IntPtr.Zero;
                success = DuplicateToken(hToken, 2, ref hDupToken);
                Console.WriteLine("[+] DuplicateToken: " + success.ToString());
                success = ImpersonateLoggedOnUser(hDupToken);
                Console.WriteLine("[+] ImpersonateLoggedOnUser: " + success.ToString());
                CloseHandle(hToken);
                CloseHandle(hDupToken);

            }
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
