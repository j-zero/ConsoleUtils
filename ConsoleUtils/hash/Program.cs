using Pastel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace hash
{
    internal class Program
    {
        static CmdParser cmd;
        static void Main(string[] args)
        {
            cmd = new CmdParser(args)
            {
                { "help", "", CmdCommandTypes.FLAG, "Show this help." },
                { "file", "f", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "File" },
                { "crc16", "", CmdCommandTypes.FLAG, "16-bit CRC hash algorithm" }, // TODO
                { "crc32", "", CmdCommandTypes.FLAG, "32-bit CRC hash algorithm" },
                { "crc64", "", CmdCommandTypes.FLAG, "64-bit CRC hash algorithm" },
                { "crc64iso", "", CmdCommandTypes.FLAG, "ISO 3309 compliant 64-bit CRC hash algorithm" },
                { "crc64ecma", "", CmdCommandTypes.FLAG, "ECMA 182 compliant 64-bit CRC hash algorithm" },
                //{ "crc32slice8", "", CmdCommandTypes.FLAG, "CRC32 hash" },
                //{ "crc32slice16", "", CmdCommandTypes.FLAG, "CRC32 hash" },
                { "elf32", "", CmdCommandTypes.FLAG, "32-bit ELF hash algorithm" },

                { "md5", "", CmdCommandTypes.FLAG, "MD5 hash algorithm" },
                { "sha1", "", CmdCommandTypes.FLAG, "SHA1 hash algorithm" },
                { "sha256", "", CmdCommandTypes.FLAG, "SHA256 hash algorithm (default)" },
                { "sha384", "", CmdCommandTypes.FLAG, "SHA384 hash algorithm" },
                { "sha512", "", CmdCommandTypes.FLAG, "SHA512 hash algorithm" },
            };

            cmd.DefaultParameter = "file";
            cmd.Parse();

            //string HashAlgo = "SHA256";
            HashAlgorithm HashAlgo = HashAlgorithm.Create("SHA1");

            if (cmd.HasFlag("sha1"))
                HashAlgo = HashAlgorithm.Create("SHA1");
            else if (cmd.HasFlag("sha256"))
                HashAlgo = HashAlgorithm.Create("SHA256");
            else if (cmd.HasFlag("sha384"))
                HashAlgo = HashAlgorithm.Create("SHA384");
            else if (cmd.HasFlag("sha512"))
                HashAlgo = HashAlgorithm.Create("SHA512");
            else if (cmd.HasFlag("md5"))
                HashAlgo = HashAlgorithm.Create("MD5");
            else if (cmd.HasFlag("crc32"))
                HashAlgo = DamienG.Security.Cryptography.Crc32.Create();
            /*
            else if (cmd.HasFlag("crc64"))
                HashAlgo = DamienG.Security.Cryptography.Crc64.Create(0xC96C5795D7870F42);
            else if (cmd.HasFlag("crc64iso"))
                HashAlgo = DamienG.Security.Cryptography.Crc64Iso.Create();
            
             else if (cmd.HasFlag("crc32slice8"))
                HashAlgo = DamienG.Security.Cryptography.Crc32Slice8.Create();
            else if (cmd.HasFlag("crc32slice16"))
                HashAlgo = DamienG.Security.Cryptography.Crc32Slice16.Create();
            */
            else if (cmd.HasFlag("elf32"))
                HashAlgo = DamienG.Security.Cryptography.Elf32.Create();

            try
            {

                cmd.Parse();

                // HELP
                if (cmd.HasFlag("help"))
                {
                    ShowHelp();
                }

                if (Console.IsInputRedirected)
                {
                    Pastel.ConsoleExtensions.Disable();
                    using (Stream s = Console.OpenStandardInput())
                    {

                            var hash = HashAlgo.ComputeHash(s);
                            Console.WriteLine(BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant());
                    }
                }
                else
                {
                    if (cmd["file"].Strings.Length > 0 && cmd["file"].Strings[0] != null)
                    {
                        string path = cmd["file"].Strings[0];
                        string value = CalculateHash(path, HashAlgo);
                        Console.WriteLine(value);
                    }
                    else
                    {
                        ShowHelp();
                        // Exit
                    }

                }

            }
            catch
            {

            }
        }

        static string CalculateHash(string filename, HashAlgorithm hashAlgorithm)
        {
            using (var stream = File.OpenRead(filename))
            {
                var hash = hashAlgorithm.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine($"{System.AppDomain.CurrentDomain.FriendlyName}, {ConsoleHelper.GetVersionString()}");
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} data");
            Console.WriteLine($"Options:");
            foreach (CmdOption c in cmd.OrderBy(x => x.Name))
            {
                string l = $"  --{c.Name}".Pastel("9CDCFE") + (!string.IsNullOrEmpty(c.ShortName) ? $", {("-" + c.ShortName).Pastel("9CDCFE")}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel("569CD6")).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
            Exit(0);
        }
        static void Exit(int exitCode)
        {
            string parrentProcess = ConsoleUtilsCore.ParentProcessUtilities.GetParentProcess().ProcessName;
            if (System.Diagnostics.Debugger.IsAttached || parrentProcess.ToLower().Contains("explorer")) // is debugger attached or started by double-click/file-drag
            {
                Console.WriteLine("\nPress any key to exit.");
                Console.ReadKey();
            }
            Environment.Exit(exitCode);
        }
    }
}
