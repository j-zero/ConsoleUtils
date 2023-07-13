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
        static Encoding stringEncoding = Encoding.Unicode;

        static void Main(string[] args)
        {
            cmd = new CmdParser(args)
            {
                { "help", "", CmdCommandTypes.FLAG, "Show this help." },
                { "name", "n", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Credential name" },
                { "user", "u", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Credential user" },
                { "password", "p", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Credential password string" },
                { "password-hex", "h", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Credential password hex-string" },
                { "file", "f", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Filename for dump/input" },
                { "read", "", CmdCommandTypes.VERB, "Read credentials" },
                { "write", "", CmdCommandTypes.VERB, "Write credentials" },
                { "list", "", CmdCommandTypes.VERB, "List all credentials" },
                { "dump", "", CmdCommandTypes.VERB, "Dump all credentials" },
                { "delete", "", CmdCommandTypes.VERB, "Delete credentials" },
                { "string", "S", CmdCommandTypes.FLAG, "Output as string" },
            };

            cmd.DefaultParameter = "name";
            cmd.DefaultVerb = "read";

            cmd.Parse();

            if (cmd.HasFlag("help"))
                ShowHelp();

            string name = cmd["name"].String;

            //var success = CredentialManager.WriteCredential("hekate", "user", Encoding.UTF8.GetBytes("pässword"), CredentialManager.CredentialPersistence.Enterprise);
            //var credentials = CredentialManager.ReadCredential("hekate");
            if (name == null || cmd.HasVerb("dump") || cmd.HasVerb("list"))
            {
                Console.WriteLine($"Credentials:");
                var creds = CredentialManager.EnumerateCrendentials();



                foreach (var cre in creds)
                {
                    if (cre != null)
                    {
                        if (cmd.HasVerb("dump"))
                        {
                            Console.WriteLine($"{"Name".Pastel(ColorTheme.Default1)}:      {cre.ApplicationName}");
                            DumpCred(cre);
                            Console.WriteLine("---");
                        }
                        else
                        {
                            Console.WriteLine($"   {cre.ApplicationName.Pastel(ColorTheme.Default1)}");
                        }
                    }
                }
            }
            else if (cmd.HasVerb("write"))
            {
                string username = "";
                if (cmd["user"].WasUserSet)
                    username = cmd["user"].String;
                byte[] password = null;
                if (cmd["password"].WasUserSet)
                    password = stringEncoding.GetBytes(cmd["password"].String);
                else if (cmd["password-hex"].WasUserSet)
                {
                    password = ConvertHelper.HexStringToByteArray(cmd["password"].String);
                }
                int result = CredentialManager.WriteCredential(name, username, password, CredentialManager.CredentialPersistence.Enterprise);
                if (result == 0)
                {
                    Console.WriteLine($"Succes!");
                    var cre = CredentialManager.ReadCredential(name);
                    DumpCred(cre);
                }
            }
            else if (cmd.HasVerb("read"))
            {
                var cre = CredentialManager.ReadCredential(name);
                if (!DumpCred(cre,!cmd.HasFlag("string")))
                {
                    ConsoleHelper.WriteError($"Credential \"{name}\" not found.");
                }
            }
            else if (cmd.HasVerb("delete"))
            {
                if (CredentialManager.DeleteCredential(name))
                    Console.Write($"Credentials \"{name.Pastel(ColorTheme.Default1)}\" deleted successful!");
                else
                    ConsoleHelper.WriteError($"Cannot delete credentials \"{name.Pastel(ColorTheme.Default1)}\"");

            }
            else
            {
                ConsoleHelper.WriteErrorDog("Wat?");
            }

            Exit(0);
        }

        static bool DumpCred(Credential cre, bool hex = true)
        {
            if (cre == null)
            {
                return false;
            }
            Console.WriteLine($"{"Username".Pastel(ColorTheme.Default1)}:  {cre.UserName}");
            Console.WriteLine($"{"Type".Pastel(ColorTheme.Default1)}:      {cre.CredentialType}");
            Console.WriteLine($"{"Content".Pastel(ColorTheme.Default1)}:   ");
            if (hex)
                ConsoleHelper.SimpleHexDump(cre.RawPassword);
            else
                Console.WriteLine(Encoding.UTF8.GetString(cre.RawPassword));
            return true;
        }

        static void ShowHelp()
        {
            Console.WriteLine($"hekate, {ConsoleHelper.GetVersionString()}");
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [Verb] [Options]");

            Console.WriteLine($"\nVerbs:");
            foreach (CmdOption c in cmd.SelectVerbs.OrderBy(x => x.Name))
            {
                string l = $"  {c.Name}".Pastel("9CDCFE") + (!string.IsNullOrEmpty(c.ShortName) ? $", {("" + c.ShortName).Pastel("9CDCFE")}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel("569CD6")).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }

            Console.WriteLine($"\nOptions:");
            foreach (CmdOption c in cmd.SelectOptions.OrderBy(x => x.Name))
            {
                string l = $"  --{c.Name}".Pastel("9CDCFE") + (!string.IsNullOrEmpty(c.ShortName) ? $", {("-" + c.ShortName).Pastel("9CDCFE")}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel("569CD6")).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
            Environment.Exit(0);
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
