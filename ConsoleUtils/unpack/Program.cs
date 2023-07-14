using Pastel;
using SevenZipExtractor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace unpack
{
    internal class Program
    {
        static CmdParser cmd;
        static string color1 = "ac46a1";
        static string color2 = "6d23b6";

        static ArchiveFile file;
        static int currentIndex = -1;
        static string fullPath = "";
        static string relativePath = "";
        static void Main(string[] args)
        {
            cmd = new CmdParser(args)
            {
                { "help", "", CmdCommandTypes.FLAG, "Show this help." },
                { "verbose", "v", CmdCommandTypes.FLAG, "Verbose output" },
                { "overwrite", "O", CmdCommandTypes.FLAG, "Overwrite existing files" },
                { "list", "l", CmdCommandTypes.VERB, "List entry in archive." },
                { "extract", "x", CmdCommandTypes.VERB, "List entry in archive." },
                //{ "undo", "u", CmdCommandTypes.VERB, "Remove every file that is in the archive (dangerous)" },
                { "file", "f", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Download URL" },
                { "outdir", "o", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, "." }
                    }, "Output folder" },
            };

            cmd.DefaultParameter = "file";
            cmd.DefaultVerb = "list";
            cmd.Parse();

            relativePath = cmd["file"].String;

            if (cmd.HasFlag("help") || relativePath == null)
                ShowHelp();

            fullPath = Path.GetFullPath(relativePath);
            //Console.WriteLine(fullPath);
            file = new ArchiveFile(fullPath);
            
            try
            {
                if (cmd.HasVerb("list"))
                {

                        try
                        {
                            var b = file.Entries; // check if archive can be unpacked
                        }
                        catch (Exception ex)
                        {
                            ConsoleHelper.WriteErrorDog(ex.Message);
                            return;
                        }

                        if (cmd.HasFlag("verbose"))
                            Console.WriteLine($"{"Size"}\t{"PackedSize"}\t{"Filename"}");

                        foreach (var entry in file.Entries)
                        {
 
                        PrintEntry(entry);
                        
                        }


                }
                else if (cmd.HasVerb("extract"))
                {

                        file.Extract(cmd["outdir"].String, cmd.HasFlag("overwrite"), HandleEvent);

                    
                }
            }
            catch(Exception ex)
            {
                ConsoleHelper.WriteErrorDog(ex.Message);
            }
            file.Dispose();
            Exit(0);
        }

        static void PrintEntry(Entry entry)
        {
            string size = UnitHelper.CalculateHumanReadableSize(entry.Size);
            string packedsize = UnitHelper.CalculateHumanReadableSize(entry.PackedSize);
            //string LastWriteTime = entry.LastWriteTime
            if (!entry.IsFolder)
            {
                var extension = GetExtension(entry.FileName);
                var color = ColorTheme.GetColorByExtension(extension);

                string fName = entry.FileName;

                if (fName == null)
                    fName = Path.GetFileNameWithoutExtension(fullPath);

                string[] p = fName.Split('\\');
                List<string> fNameColorParts = new List<string>();

                for (int i = 0; i < p.Length; i++)
                {
                    if (i == p.Length - 1)
                        fNameColorParts.Add(p[i].Pastel(color));
                    else
                        fNameColorParts.Add(p[i].Pastel(ColorTheme.Directory));
                }

                if (cmd.HasFlag("verbose"))
                    Console.WriteLine($"{size}\t{packedsize}\t\t{string.Join("\\", fNameColorParts)}");
                else
                    Console.WriteLine($"{string.Join("\\", fNameColorParts)}");
            }
            else // foobar
            {
                if (cmd.HasFlag("verbose"))
                    Console.WriteLine($"{"-     "}\t{"-     "}\t\t{entry.FileName.Pastel(ColorTheme.Directory)}");
                else
                    Console.WriteLine($"{entry.FileName.Pastel(ColorTheme.Directory)}");

            }
        }

        static void HandleEvent(object o, ArchiveExtractionProgressEventArgs e)
        {
            int index = (int)e.EntryIndex;
            var entry = file.Entries[index];

            if (currentIndex != index)
            {
                currentIndex = index;
                PrintEntry(entry);
            }
            else
            {
                // todo percent
            }
        }


        private static string GetExtension(string filename)
        {
            if (filename == null || !filename.Contains("."))
                return filename;
            return filename.Substring(filename.IndexOf("."));
        }
        static void ShowHelp()
        {
            ShowVersion();
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName.Replace(".exe","").Pastel(color1)} [{"Verb".Pastel(color1)}] [{"Options".Pastel(color1)}] {{[{"--file".Pastel(color1)}|{"-f".Pastel(color1)}] {"file".Pastel(color2)}}}");

            Console.WriteLine($"Verbs:");
            foreach (CmdOption c in cmd.SelectVerbs.OrderBy(x => x.Name))
            {
                string l = $"  {c.Name}".Pastel(color1) + (!string.IsNullOrEmpty(c.ShortName) ? $", {("" + c.ShortName).Pastel(color1)}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel(color2)).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
            Console.WriteLine($"Options:");
            foreach (CmdOption c in cmd.SelectOptions.OrderBy(x => x.Name))
            {
                string l = $"  --{c.Name}".Pastel(color1) + (!string.IsNullOrEmpty(c.ShortName) ? $", {("-" + c.ShortName).Pastel(color1)}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel(color2)).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
            //WriteError("Usage: subnet [ip/cidr|ip/mask|ip number_of_hosts]");
            Exit(0);
        }

        static void ShowVersion()
        {
            string version_string = ("v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " ").PadLeft(20);

            Console.WriteLine(@"  ▄      ▄   █ ▄▄  ██   ▄█▄    █  █▀ ".Pastel("#6411ad"));
            Console.WriteLine(@"   █      █  █   █ █ █  █▀ ▀▄  █▄█   ".Pastel("#6d23b6"));
            Console.WriteLine(@"█   █ ██   █ █▀▀▀  █▄▄█ █   ▀  █▀▄   ".Pastel("#822faf"));
            Console.WriteLine(@"█   █ █ █  █ █     █  █ █▄  ▄▀ █  █  ".Pastel("#973aa8"));
            Console.WriteLine(@"█▄ ▄█ █  █ █  █       █ ▀███▀    █   ".Pastel("#ac46a1"));
            Console.WriteLine(@" ▀▀▀  █   ██   ▀     █          ▀    ".Pastel("#c05299"));
            Console.WriteLine((version_string +    @"▀                ").Pastel("#d55d92"));
            Console.WriteLine($"{"unpack".Pastel(color1)} is part of " + ConsoleHelper.GetVersionString(color2, color2));
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
