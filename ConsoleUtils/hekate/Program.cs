using Pastel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace hekate
{
    internal class Program
    {
        static CmdParser cmd;
        static void Main(string[] args)
        {
            cmd = new CmdParser(args)
            {
                { "help", "", CmdCommandTypes.FLAG, "Show this help." },
                { "name", "n", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Download URL" },
                { "list", "", CmdCommandTypes.FLAG, "List all credentials" },
                { "dump", "", CmdCommandTypes.FLAG, "Dump all credentials" },
            };

            cmd.DefaultParameter = "name";
            cmd.Parse();

            if (cmd.HasFlag("help"))
                ShowHelp();

            string name = cmd["name"].String;

            //var success = CredentialManager.WriteCredential("hekate", "user", Encoding.UTF8.GetBytes("pässword"), CredentialManager.CredentialPersistence.Enterprise);
            //var credentials = CredentialManager.ReadCredential("hekate");
            if (name == null || cmd.HasFlag("dump") || cmd.HasFlag("list"))
            {
                foreach (var cre in CredentialManager.EnumerateCrendentials())
                {
                    if (cre != null)
                    {
                        if (cmd.HasFlag("dump"))
                        {
                            Console.WriteLine($"{"Name".Pastel(ColorTheme.Default1)}:     {cre.ApplicationName}");
                            DumpCred(cre);
                            Console.WriteLine("---");
                        }
                        else
                        {
                            Console.WriteLine($"{cre.ApplicationName}");
                        }
                    }
                }
            }
            else
            {
                var cre = CredentialManager.ReadCredential(name);
                DumpCred(cre);
            }
            Exit(0);
        }

        static void DumpCred(Credential cre, bool hex = true)
        {
            
            Console.WriteLine($"{"Username".Pastel(ColorTheme.Default1)}: {cre.UserName}");
            Console.WriteLine($"{"Content".Pastel(ColorTheme.Default1)}:");
            ConsoleHelper.SimpleHexDump(cre.RawPassword);
        }

        static void ShowHelp()
        {
            Console.WriteLine($"download, {ConsoleHelper.GetVersionString()}");
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [Options] {{[--url|-u] URL}}");
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

        public static string Unprotect(string encryptedString, string optionalEntropy, DataProtectionScope scope)
        {
            return Encoding.UTF8.GetString(
                ProtectedData.Unprotect(
                    Convert.FromBase64String(encryptedString)
                    , optionalEntropy != null ? Encoding.UTF8.GetBytes(optionalEntropy) : null
                    , scope));
        }

        public static string Protect(string stringToEncrypt, string optionalEntropy, DataProtectionScope scope)
        {
            return Convert.ToBase64String(
                ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(stringToEncrypt)
                    , optionalEntropy != null ? Encoding.UTF8.GetBytes(optionalEntropy) : null
                    , scope));
        }
    }
}
