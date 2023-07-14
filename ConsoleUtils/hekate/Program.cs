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
                { "string", "s", CmdCommandTypes.FLAG, "Output as string" },
            };

            cmd.DefaultParameter = "name";
            cmd.DefaultVerb = "list";

            cmd.Parse();

            if (cmd.HasFlag("help"))
                ShowLongHelp();

            string name = cmd["name"].String;

            //var success = CredentialManager.WriteCredential("hekate", "user", Encoding.UTF8.GetBytes("pässword"), CredentialManager.CredentialPersistence.Enterprise);
            //var credentials = CredentialManager.ReadCredential("hekate");
            if (name == null && !cmd["dump"].WasUserSet && !cmd["list"].WasUserSet)
            {
                ShowHelp();
                Environment.Exit(0);

            }

            if (cmd.HasVerb("dump") || cmd.HasVerb("list"))
            {
                Console.WriteLine($"Credentials:");
                var creds = CredentialManager.EnumerateCrendentials();



                foreach (var cre in creds)
                {
                    if (cre != null)
                    {
                        string credType = cre.CredentialType.ToString();
                        Console.WriteLine($"{cre.ApplicationName.Pastel(ColorTheme.Default2)} ({credType.Pastel(ColorTheme.DarkText)})");
                        //ConsoleHelper.HexDump(cre.ApplicationName);
                        if (cmd.HasVerb("dump"))
                        {
                            DumpCred(cre);
                            Console.WriteLine("---".Pastel(ColorTheme.Default1));
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
            Console.Write(    $"{"Content".Pastel(ColorTheme.Default1)}:   ");
            if (hex)
            {

                if (cre.RawPassword.Length > 0)
                {
                    Console.WriteLine();
                    ConsoleHelper.HexDump(cre.RawPassword);
                }
                else
                    Console.WriteLine($"<{"null".Pastel(ColorTheme.Default1)}>");
            }
            else
                Console.WriteLine(Encoding.UTF8.GetString(cre.RawPassword));
            return true;
        }

        static void ShowVersion()
        {
            Console.WriteLine(@" ▄  █ ▄███▄   █  █▀ ██     ▄▄▄▄▀ ▄███▄   ".Pastel("#64b5f6"));
            Console.WriteLine(@"█   █ █▀   ▀  █▄█   █ █ ▀▀▀ █    █▀   ▀  ".Pastel("#42a5f5"));
            Console.WriteLine(@"██▀▀█ ██▄▄    █▀▄   █▄▄█    █    ██▄▄    ".Pastel("#2196f3"));
            Console.WriteLine(@"█   █ █▄   ▄▀ █  █  █  █   █     █▄   ▄▀ ".Pastel("#1e88e5"));
            Console.WriteLine(@"   █  ▀███▀     █      █  ▀      ▀███▀   ".Pastel("#1976d2"));
            Console.WriteLine(@"  ▀            ▀      █                  ".Pastel("#1565c0"));
            Console.WriteLine(@"                     ▀ ".Pastel("#0d47a1") +("v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()).Pastel("#0d47a1"));
            Console.WriteLine("hekate is part of " + ConsoleHelper.GetVersionString());
        }
        static void ShowHelp(bool more = true)
        {
            ShowVersion();
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName.Pastel("64b5f6")} [{"Verb".Pastel("1976d2")}] [{"Options".Pastel("1976d2")}]\n");
            if(more)
                Console.WriteLine($"For more options, use {"--help".Pastel("64b5f6")}");
            Console.WriteLine($"{"Verb".Pastel("1976d2")}s:");
            foreach (CmdOption c in cmd.SelectVerbs.OrderBy(x => x.Name))
            {
                string l = $"  {c.Name}".Pastel("64b5f6") + (!string.IsNullOrEmpty(c.ShortName) ? $", {("" + c.ShortName).Pastel("64b5f6")}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel("1976d2")).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
        }
 
        static void ShowLongHelp()
        {
            ShowHelp(false);
            
            Console.WriteLine($"\n{"Options".Pastel("1976d2")}:");
            foreach (CmdOption c in cmd.SelectOptions.OrderBy(x => x.Name))
            {
                string l = $"  --{c.Name}".Pastel("64b5f6") + (!string.IsNullOrEmpty(c.ShortName) ? $", {("-" + c.ShortName).Pastel("64b5f6")}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel("1976d2")).ToArray()) + ">" : "") + ": " + c.Description;
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
