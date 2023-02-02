using Pastel;
using SevenZipExtractor;
using System;
using System.IO;
using System.Linq;


namespace unpack
{
    internal class Program
    {
        static CmdParser cmd;

        static void Main(string[] args)
        {
            cmd = new CmdParser(args)
            {
                { "help", "", CmdCommandTypes.FLAG, "Show this help." },
                { "list", "l", CmdCommandTypes.VERB, "List entry in archive." },
                { "extract", "x", CmdCommandTypes.VERB, "List entry in archive." },
                //{ "undo", "u", CmdCommandTypes.VERB, "Remove every file that is in the archive (dangerous)" },
                { "file", "f", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Download URL" },
                { "outdir", "O", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, "." }
                    }, "Output folder" },
            };

            cmd.DefaultParameter = "file";
            cmd.DefaultVerb = "list";
            cmd.Parse();

            string relativePath = cmd["file"].String;

            if (cmd.HasFlag("help") || relativePath == null)
                ShowHelp();

            string fullPath = Path.GetFullPath(relativePath);
            Console.WriteLine(fullPath);

            Console.WriteLine($"{"Size"}\t{"PackedSize"}\t{"Filename"}");
            if (cmd.HasVerb("list"))
            {
                using (ArchiveFile file = new ArchiveFile(fullPath))
                {
                    foreach (var entry in file.Entries)
                    {
                        string size = UnitHelper.CalculateHumanReadableSize(entry.Size);
                        string packedsize = UnitHelper.CalculateHumanReadableSize(entry.PackedSize);
                        //string LastWriteTime = entry.LastWriteTime
                        if (!entry.IsFolder)
                        {
                            var extension = GetExtension(entry.FileName);
                            var color = ColorTheme.GetColorByExtension(extension);
                            Console.WriteLine($"{size}\t{packedsize}\t\t{entry.FileName.Pastel(color)}");
                        }
                        else
                        {
                            Console.WriteLine($"{"    -"}\t{"     -"}\t\t{entry.FileName.Pastel(ColorTheme.Directory)}");
                        }

                    }
                }
            }
            else if (cmd.HasVerb("extract"))
            {
                using (ArchiveFile file = new ArchiveFile(fullPath))
                {
                    file.Extract(cmd["outdir"].String);
                }
            }


            Exit(0);
        }



        private static string GetExtension(string filename)
        {
            if (!filename.Contains("."))
                return filename;
            return filename.Substring(filename.IndexOf("."));
        }
        static void ShowHelp()
        {
            Console.WriteLine($"unpack, {ConsoleHelper.GetVersionString()}");
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [Verb] [Options] {{[--file|-f] file}}");

            Console.WriteLine($"Verbs:");
            foreach (CmdOption c in cmd.SelectVerbs.OrderBy(x => x.Name))
            {
                string l = $"  {c.Name}".Pastel("9CDCFE") + (!string.IsNullOrEmpty(c.ShortName) ? $", {("" + c.ShortName).Pastel("9CDCFE")}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel("569CD6")).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
            Console.WriteLine($"Options:");
            foreach (CmdOption c in cmd.SelectOptions.OrderBy(x => x.Name))
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
