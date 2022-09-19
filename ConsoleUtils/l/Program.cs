using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pastel;

namespace list
{
    internal class Program
    {
        static CmdParser cmd;
        static string path = null;
        static Dictionary<string, List<FilesystemEntryInfo>> entries;

        static bool ShowHidden = false;
        

        static void Main(string[] args)
        {
            entries = new Dictionary<string, List<FilesystemEntryInfo>>();
            cmd = new CmdParser(args)
            {
                {"list","l", CmdCommandTypes.FLAG, "List" },
                {"all","a", CmdCommandTypes.FLAG, "All" },
                { "path", "p", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, null }
                }, "Path" }
            };

            cmd.DefaultParameter = "path";
            
            cmd.Parse();

            if (cmd["path"].Strings.Length > 0 && cmd["path"].Strings[0] != null)
                path = cmd["path"].Strings[0];

            ShowHidden = cmd.HasFlag("all");


            if (path == null)
                path = Environment.CurrentDirectory;




            FilesystemEntryInfo[] found = FileAndDirectoryFilter.GetFilesFromFilter(path);
            
            if (found.Length > 0)
            {
                if(found.Length == 1 && found[0].Error)
                {
                    ConsoleHelper.WriteError($"Access denied: \"{path}\"");
                    return;
                }

                foreach (FilesystemEntryInfo e in found)
                {
                    if (!entries.ContainsKey(e.BaseDirectory))
                        entries.Add(e.BaseDirectory, new List<FilesystemEntryInfo>());
                    entries[e.BaseDirectory].Add(e);
                }

                bool showRelativePath = false;

                if (entries.Count > 1)
                    showRelativePath = true;

                foreach (KeyValuePair<string, List<FilesystemEntryInfo>> ei in entries)
                {
                    FilesystemEntryInfo d = new FilesystemEntryInfo(ei.Key);
                    var list = ShowHidden ? ei.Value : ei.Value.Where(e => e.HasHiddenAttribute == false).ToList();
                    if (cmd.HasFlag("list"))
                    {
                        LongList(list, showRelativePath);
                        if (entries.Count > 1)
                            Console.WriteLine();
                    }
                    else
                    {
                        ShortList(list, showRelativePath);
                    }
                }
                //LongList();
            }
            else
            {
                
                try
                {
                    string filepath = null;
                    filepath = Path.GetFullPath(path);
                    ConsoleHelper.WriteError($"No files found in \"{filepath}\"");
                }
                catch
                {
                    ConsoleHelper.WriteError($"\"{path}\" does not exist.");
                }
                


            }
            ;
        }


        static void ShortList(List<FilesystemEntryInfo> ei, bool printParentDiretory = false)
        {
            var longest_parent_dir = ei.Max(r => (r.GetRelativeParent(Environment.CurrentDirectory)).Length+3);
            int width = Console.WindowWidth;
            var longest_name = ei.Max(r => r.Name.Length)+2;
            int columns = width / longest_name;

            int chunk_size = (int)Math.Ceiling(((double)ei.Count / (double)columns));

            var chunks = ei.OrderBy(o => o.Name).ToList().ChunkBy(chunk_size);

            for (int i = 0; i < chunk_size; i++) 
            {
                for(int c = 0; c < columns; c++)
                    if(c < chunks.Count && i < chunks[c].Count)
                    {
                        var e = chunks[c][i];
                        var parent_directory = "." + Path.DirectorySeparatorChar + e.GetRelativeParent(Environment.CurrentDirectory) + Path.DirectorySeparatorChar;
                        var name = (printParentDiretory ? (parent_directory).Pastel(ColorTheme.Directory)  : "") + e.Name.PadRight(longest_name).Pastel(e.ColorString);
                        Console.Write(name);
                    }
                Console.WriteLine("");
            }
            
            
        }
        static void LongList(List<FilesystemEntryInfo> ei, bool printParentDiretory = false)
        {
            /*
                d - Directory
                a - Archive
                r - Read-only
                h - Hidden
                s - System
                l - Reparse point, symlink, etc.
            */
            int width = Console.WindowWidth;
            //foreach (KeyValuePair<string, List<EntryInfo>> ei in entries)

            var longestOwner = ei.Max(r => r.ShortOwner.Length);
            var longestSize = ei.Max(r => r.HumanReadbleSize.Length);

            foreach (FilesystemEntryInfo e in ei.OrderBy(o => o.Name).ToList())
            {
                if (!e.HasHiddenAttribute || ShowHidden)
                {
                    if (Console.CursorLeft + e.Name.Length > width) Console.WriteLine(); // line break to not cut file names

                    /*
                    if(e.IsDirectory && !e.CanRead) { 
                        Console.WriteLine("Access to '".Pastel("#ff4500") + $"{e.FullPath}".Pastel(ColorTheme.Default1) + "' is denied!".Pastel("#ff4500"));
                        continue;
                    }
                    */

                    //char[] mode = "------".ToCharArray();

                    string mode = "";
                    string minus = "-".Pastel("#606060");

                    mode += e.IsDirectory ? "d".Pastel(ColorTheme.Directory) : ".".Pastel(e.ColorString);
                    mode += e.HasArchiveAttribute ? "a".Pastel("#ffd700") : minus;
                    mode += e.Owner == string.Empty ? "!".PastelBg("#ff4500").Pastel("#ffffff") : minus;
                    mode += !e.CanWrite ? "W".Pastel("#ff4500") : (e.HasReadOnlyAttribute ? "r".Pastel("#ff4500") : minus);
                    mode += e.HasHiddenAttribute ? "h".Pastel("#606060") : minus;
                    mode += e.HasSystemAttribute ? "s".Pastel("#ff8c00") : minus;
                    mode += e.IsLink ? "l".Pastel(ColorTheme.Symlink) : minus;

                    mode += e.FileType == FileTypes.Executable ? "x".Pastel(e.ColorString) : minus;

                    // Console.WriteLine("Longest Owner: " + longestOwner.ToString());

                    string size = e.IsDirectory ? "-".PadLeft(longestSize+1).Pastel(ColorTheme.DarkColor) : e.HumanReadbleSize.PadLeft(e.Length == 0 || e.HumanReadbleSizeSuffix == string.Empty ? longestSize + 1 : longestSize).Pastel(ColorTheme.Default1) + e.HumanReadbleSizeSuffix.Pastel(ColorTheme.Default2);
                    string owner = (e.ShortOwner != string.Empty ? e.ShortOwner.PadRight(longestOwner).Pastel("#F9F1A5") : "???".PadRight(longestOwner).Pastel("#ff4500"));
                    string lastWriteTime = e.HumanReadableLastWriteTime.Pastel("#008FFF");


                    Console.Write(mode + " ");
                    Console.Write($"{size} ");
                    Console.Write($"{owner} ");
                    Console.Write($"{lastWriteTime} ");

                    int pos = Console.CursorLeft;
                    string parent_dir = (e.GetRelativeParent(Environment.CurrentDirectory) + Path.DirectorySeparatorChar).Pastel(ColorTheme.Directory);
                    string name = printParentDiretory ? parent_dir + e.Name.Pastel(e.ColorString) : e.Name.Pastel(e.ColorString);

                    /* TODO
                    if(name.Length + pos > Console.WindowWidth)
                        name = name.Substring(0, Console.WindowWidth - pos - 4) + "...";
                    */

                    Console.Write(name);

                    if (e.LinkTarget != null)
                        Console.Write(" -> " + PathHelper.GetRelativePath(path, e.LinkTarget));

                    Console.Write("\n");
                }
            }
        }
    }
}
