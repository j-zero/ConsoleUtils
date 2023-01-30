using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pastel;

namespace list
{
    internal class Program
    {
        
        public enum OrderBy
        {
            name, Name, dotname, Dotname, size, extension, modified, accessed, created, type, none
        }
        
        static CmdParser cmd;
        static string path = null;
        static Dictionary<string, List<FilesystemEntryInfo>> entries;

        static bool ShowInfo = false;
        static bool ShowHidden = false;
        static bool ShowEncoding = false;
        static bool ShowHeader = false;
        static bool ShowStreams = false;

        static bool isStartedFromExplorer = false;

        static bool ShowLongOwner = false;

        static void Main(string[] args)
        {
            isStartedFromExplorer = System.Diagnostics.Debugger.IsAttached || ConsoleUtilsCore.ParentProcessUtilities.GetParentProcess().ProcessName.ToLower().Contains("explorer"); // is debugger attached or started by double-click/file-drag
            entries = new Dictionary<string, List<FilesystemEntryInfo>>();


            cmd = new CmdParser()
            {
                {"help", "", CmdCommandTypes.FLAG, "show list of command-line options" },
                {"long","l", CmdCommandTypes.FLAG, "display extended file metadata as a table" },
                {"all","a", CmdCommandTypes.FLAG, "show hidden files" },
                {"header","h", CmdCommandTypes.FLAG, "show mime type" },
                {"encoding","", CmdCommandTypes.FLAG, "show encoding type" },
                {"oneline","1", CmdCommandTypes.FLAG, "display one entry per line" },
                {"reverse","r", CmdCommandTypes.FLAG, "reverses the sort order" },

                {"info","i", CmdCommandTypes.FLAG, "show magic file description"},
                {"streams","R", CmdCommandTypes.FLAG, "show NTFS alternate file streams"},

                {"full","F", CmdCommandTypes.FLAG, "show all available files and informations"},

                {"group-directories-first", "d", CmdCommandTypes.FLAG, "list directories before other files" },
                {"only-dirs", "D", CmdCommandTypes.FLAG, "list directories before other files" },

                { "sort", "s", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, "Name" }
                }, "sort field" },

                { "path", "p", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, null }
                }, "Path" }
            };

            cmd.DefaultParameter = "path";
            
            cmd.Parse();

            if (cmd.HasFlag("help"))
                ShowHelp();

            if (cmd["path"].Strings.Length > 0 && cmd["path"].Strings[0] != null)
                path = cmd["path"].Strings[0];

            ShowInfo = cmd.HasFlag("info") || cmd.HasFlag("full");
            ShowHidden = cmd.HasFlag("all") || cmd.HasFlag("full");

            ShowEncoding = cmd.HasFlag("encoding");
            ShowHeader = cmd.HasFlag("header");
            ShowStreams = cmd.HasFlag("streams") || cmd.HasFlag("full");

            if (path == null)
                path = Environment.CurrentDirectory;

            FilesystemEntryInfo[] found = null;

            try
            {
                found = FileAndDirectoryFilter.GetFilesFromFilter(path);
            }
            catch(Exception ex)
            {
                Die("Guru Meditation: " + ex.Message, 1);
            }

            var notHiddenCount = found.Where(file => !file.HasHiddenAttribute).ToArray().Length;
            
            if (found.Length > 0 && (ShowHidden ||(notHiddenCount > 0)))
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
                    //FilesystemEntryInfo d = new FilesystemEntryInfo(ei.Key);
                    var list = ShowHidden ? ei.Value : ei.Value.Where(e => e.HasHiddenAttribute == false).ToList();

                    if (cmd.HasFlag("only-dirs"))
                        list = list.Where(e => e.IsDirectory).ToList();

                    if (cmd.HasFlag("long") || cmd.HasFlag("info") || cmd.HasFlag("full") || ShowStreams || ShowHeader || ShowEncoding)
                    {
                        LongList(list, showRelativePath);
                        if (entries.Count > 1)
                            Console.WriteLine();
                    }
                    else if (cmd.HasFlag("oneline"))
                    {
                        OneLinerList(list, showRelativePath);
                    }
                    else
                    {
                        GridList(list, showRelativePath);
                    }
                }
                //LongList();
            }
            else
            {
                
                try
                {
                    string filepath = path;
                    if(!path.StartsWith("\\\\"))
                        filepath = Path.GetFullPath(path);

                    if(found.Length == 0)
                        ConsoleHelper.WriteError($"No files found in \"{filepath}\"");
                    else
                        ConsoleHelper.WriteError($"No files found in \"{filepath}\", but there are hidden files! Use -a to show them.");

                }
                catch
                {
                    ConsoleHelper.WriteError($"\"{path}\" does not exist.");
                }
                


            }
            ;

            if (isStartedFromExplorer)
            {
                Console.WriteLine("\nPress any key to exit.");
                Console.ReadKey();
            }

        }
        public static void Die(string msg, int errorcode)
        {
            ConsoleHelper.WriteError(msg);
            Environment.Exit(errorcode);
        }
        static void ShowHelp()
        {
            Console.WriteLine($"list, {ConsoleHelper.GetVersionString()}");
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName.Replace(".exe","")} [Options] [[--path|-p] path]");
            Console.WriteLine($"Options:");
            foreach (CmdOption c in cmd.OrderBy(x => x.Name))
            {
                string l = $"  --{c.Name}".Pastel("9CDCFE") + (!string.IsNullOrEmpty(c.ShortName) ? $", {("-" + c.ShortName).Pastel("9CDCFE")}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel("569CD6")).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
            //WriteError("Usage: subnet [ip/cidr|ip/mask|ip number_of_hosts]");
            Exit(0);
        }

        static string ShortenString(string str, int maxchars, string color1, string color2, string color3)
        {
            if (str.Length <= maxchars)
                return str;

            int half = maxchars / 2;
            return $"{str.Substring(0, half - 2).Pastel(color1)}{"...".Pastel(color2)}{str.Substring(str.Length - half, half).Pastel(color3)}";
        }

        static string ShortenMIMEType(string input, int maxchars, string color1, string color2, string color3)
        {
            string str = input.Replace("application/", "app/").Replace("inode/","");
            
            if (str.Length <= maxchars)
                return str;

            int half = maxchars / 2;
            return $"{str.Substring(0, half - 2).Pastel(color1)}{"...".Pastel(color2)}{str.Substring(str.Length - half, half).Pastel(color3)}";
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

        static List<FilesystemEntryInfo> SortFileSystemList(List<FilesystemEntryInfo> ei)
        {
            List<FilesystemEntryInfo> result = new List<FilesystemEntryInfo>();

            string cmdSortString = cmd["sort"].Strings[0];

            if (cmdSortString == ".name")
                cmdSortString = "dotname";
            else if (cmdSortString == ".Name")
                cmdSortString = "Dotname";

            OrderBy orderBy = (OrderBy)Enum.Parse(typeof(OrderBy), cmdSortString);

            if (cmd.HasFlag("group-directories-first"))
            {
                var dirs = SortFileSystemList(ei.Where(o => o.IsDirectory).ToList(), orderBy);
                var files = SortFileSystemList(ei.Where(o => !o.IsDirectory).ToList(), orderBy);

                result.AddRange(dirs);
                result.AddRange(files);
            }
            else
            {
                result = SortFileSystemList(ei, orderBy);
            }

            if (cmd.HasFlag("reverse"))
                result.Reverse();

            return result;
        }

        static List<FilesystemEntryInfo> SortFileSystemList(List<FilesystemEntryInfo> ei, OrderBy order)
        {
            List<FilesystemEntryInfo> result = null;
            switch (order)
            {
                case OrderBy.name:
                    result = ei.OrderBy(o => o.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
                    break;
                case OrderBy.Name:
                    result = ei.OrderBy(o => o.Name, StringComparer.CurrentCulture).ToList();
                    break;
                case OrderBy.dotname:
                    result = ei.OrderBy(o => o.Name, new WithoutDotComparer(StringComparer.CurrentCultureIgnoreCase)).ToList();
                    break;
                case OrderBy.Dotname:
                    result = ei.OrderBy(o => o.Name, new WithoutDotComparer(StringComparer.CurrentCulture)).ToList();
                    break;
                case OrderBy.size:
                    result = ei.OrderBy(o => o.Length).ToList();
                    break;
                case OrderBy.extension:
                    result = ei.OrderBy(o => o.Extension).ToList();
                    break;
                case OrderBy.type:
                    result = ei.OrderBy(o => o.FileType).ToList();
                    break;
                case OrderBy.modified:
                    result = ei.OrderBy(o => o.LastWriteTime).ToList();
                    break;
                case OrderBy.created:
                    result = ei.OrderBy(o => o.CreationTime).ToList();
                    break;
                case OrderBy.accessed:
                    result = ei.OrderBy(o => o.LastAccessTime).ToList();
                    break;
                default:
                    result = ei.ToList();
                    break;
            }
            return result;
        }

        static void OneLinerList(List<FilesystemEntryInfo> ei, bool printParentDiretory = false)
        {

            var longest_parent_dir = ei.Max(r => (r.GetRelativeParent(Environment.CurrentDirectory)).Length + 3);
            int width = Console.WindowWidth;
            var longest_name = ei.Max(r => r.Name.Length) + 2;
            int columns = width / longest_name;

            int chunk_size = (int)Math.Ceiling(((double)ei.Count / (double)columns));

            var chunks = SortFileSystemList(ei).ChunkBy(chunk_size);

            foreach (FilesystemEntryInfo e in SortFileSystemList(ei))
            {
                if (!e.HasHiddenAttribute || ShowHidden)
                {


                    var parent_directory = "." + Path.DirectorySeparatorChar + e.GetRelativeParent(Environment.CurrentDirectory) + Path.DirectorySeparatorChar;
                    var name = string.Empty;

                    name = (printParentDiretory ? (parent_directory).Pastel(ColorTheme.Directory) : "") + e.Name.Pastel(e.ColorString) + Environment.NewLine;

                    Console.Write(name);
                }
                
            }
        }
        

        static void GridList(List<FilesystemEntryInfo> ei, bool printParentDiretory = false)
        {
            
            var longest_parent_dir = ei.Max(r => (r.GetRelativeParent(Environment.CurrentDirectory)).Length+3);
            int width = Console.WindowWidth;
            var longest_name = ei.Max(r => r.Name.Length)+2;
            int columns = width / longest_name;

            int chunk_size = (int)Math.Ceiling(((double)ei.Count / (double)columns));

            var chunks = SortFileSystemList(ei).ChunkBy(chunk_size);

            for (int i = 0; i < chunk_size; i++) 
            {
                for(int c = 0; c < columns; c++)

                    if(c < chunks.Count && i < chunks[c].Count)
                    {
                        var e = chunks[c][i];
                        var parent_directory = "." + Path.DirectorySeparatorChar + e.GetRelativeParent(Environment.CurrentDirectory) + Path.DirectorySeparatorChar;
                        var name = (printParentDiretory ? (parent_directory).Pastel(ColorTheme.Directory) : "") + e.Name.PadRight(longest_name).Pastel(e.ColorString);

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
                l - Reparse point, symlink, etc.ls 
            */
            int width = Console.WindowWidth;
            //foreach (KeyValuePair<string, List<EntryInfo>> ei in entries)

            int longestName = ei.Max(r => r.Name.Length);
            int longestShortOwner = ei.Max(r => r.ShortOwner.Length);
            int longestLongOwner = ei.Max(r => r.Owner.Length);
            int longestSize = ei.Max(r => r.HumanReadbleSize.Length);
            int longestEncoding = 0;
            int longestDesciption = 0;


            if (ShowEncoding)
                longestEncoding = ei.Max(r => r.Encoding.Length);

            if (ShowInfo)
                longestDesciption = ei.Max(r => r.FileTypeDescription.Length);

            int maxPos = 9 + longestSize + 1 + longestName + 1 + longestShortOwner + 1 + (longestDesciption != 0 ? longestDesciption + 1 : 0) + (longestEncoding != 0 ? longestEncoding + 1 : 0);

            foreach (FilesystemEntryInfo e in SortFileSystemList(ei))
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
                    mode += e.HasHiddenAttribute ? "h".Pastel(ColorTheme.DarkColor) : minus;
                    mode += e.HasSystemAttribute ? "s".Pastel("#ff8c00") : minus;
                    mode += e.IsLink ? "l".Pastel(ColorTheme.Symlink) : minus;
                    mode += e.FileType == FileTypes.Executable ? "x".Pastel(e.ColorString) : minus;

                    Console.Write(mode + " ");



                    string owner = "";
                    if(ShowLongOwner || !e.IsShare)
                        owner = (e.ShortOwner != string.Empty ? e.ShortOwner.PadRight(longestShortOwner).Pastel("#F9F1A5") : "?".PadRight(longestShortOwner).Pastel("#ff4500"));
                    else
                        owner = (e.Owner != string.Empty ? e.Owner.PadRight(longestLongOwner).Pastel("#F9F1A5") : "?".PadRight(longestLongOwner).Pastel("#ff4500"));

                    Console.Write($"{owner} ");
                    int sizepos = 0;
                    string size = "";
                    string encoding = "";
                    string lastWriteTime = "";

                    if (!e.IsShare)
                    {
                        lastWriteTime = $"{e.HumanReadableLastWriteTime.Pastel("#4169e1")} ";
                        Console.Write(lastWriteTime);

                        if (ShowEncoding)
                        {
                            encoding = $"{e.Encoding.PadRight(longestEncoding + 1).Pastel("#666666")}";
                            Console.Write(encoding);
                        }

                        size = e.IsDirectory ? "-".PadLeft(longestSize + 1).Pastel(ColorTheme.DarkColor) : e.HumanReadbleSize.PadLeft(e.Length == 0 || e.HumanReadbleSizeSuffix == string.Empty ? longestSize + 1 : longestSize).Pastel(ColorTheme.Default1) + e.HumanReadbleSizeSuffix.Pastel(ColorTheme.Default2);
                        sizepos = Console.CursorLeft;
                        Console.Write($"{size} ");
                    }

                    string parent_dir = (e.GetRelativeParent(Environment.CurrentDirectory) + Path.DirectorySeparatorChar).Pastel(ColorTheme.Directory);

                    string name = e.Name.Pastel(e.ColorString);
                    if(e.IsShare)
                    {
                        if (e.ShareName.EndsWith("$"))
                            name = e.ShareName.Substring(0, e.ShareName.Length - 1).Pastel(ColorTheme.DarkColor) + "$".Pastel(ColorTheme.HighLight1);
                        else 
                            name = e.ShareName.Pastel(ColorTheme.Directory);
                    }

                    string output_name = printParentDiretory ? parent_dir + name : name;



                    int filepos = Console.CursorLeft;
                    int maxDescLength = Console.WindowWidth - Console.CursorLeft - 8;

                    /*
                    string spaces = "";
                    for (int i = 0; i < filepos; i++)
                        spaces += " ";
                    */

                    Console.Write(output_name);



                    if (e.LinkTarget != null)
                    {
                        string target = (" -> " + PathHelper.GetRelativePath(path, e.LinkTarget));
                        Console.Write(target);
                    }



                    if (ShowInfo && !e.IsDirectory && !string.IsNullOrWhiteSpace(e.FileTypeDescription))
                    {
                        Console.WriteLine();
                        ConsoleHelper.WriteSplittedText(e.FileTypeDescription, maxDescLength, "  ", filepos, "#666666");
                    }


                    Console.WriteLine();

                    if (ShowStreams)
                    {
                        if (e.AlternateDataStreams != null)
                        {
                            var spaceSpaces = "";
                            for (int i = 0; i < sizepos; i++)
                                spaceSpaces += " ";

                            foreach (var s in e.AlternateDataStreams)
                            {
                                if (s.Name != String.Empty)
                                {
                                    string streamName = s.Name.Replace(":$DATA", "");
                                    (string streamSize, string streamSizeSuffix) = FilesystemEntryInfo.GetHumanReadableSize(s.Length);
                                    Console.WriteLine($"{spaceSpaces}" +
                                                        $"{streamSize.PadLeft(s.Length == 0 || streamSizeSuffix == string.Empty ? longestSize + 1 : longestSize).Pastel(ColorTheme.Default1)}{streamSizeSuffix.Pastel(ColorTheme.Default2)}" +
                                                        $" :{streamName.Pastel("#808080")} ");
                                    if (ShowInfo)
                                    {
                                        ConsoleHelper.WriteSplittedText(MIMEHelper.GetDescription(s.OpenRead()), maxDescLength, "  ", filepos, "#666666");
                                        Console.WriteLine();
                                    }

                                }

                            }
                        }
                    }

                }
            }


        }


    }

    class WithoutDotComparer : IComparer<string>
    {
        private readonly IComparer<string> _baseComparer;
        public WithoutDotComparer(IComparer<string> baseComparer)
        {
            _baseComparer = baseComparer;
        }

        public int Compare(string x, string y)
        {
            /*
                // "b" comes before everything else
                if (_baseComparer.Compare(x, "b") == 0)
                    return -1;
                if (_baseComparer.Compare(y, "b") == 0)
                    return 1;

                // "c" comes next
                if (_baseComparer.Compare(x, "c") == 0)
                    return -1;
                if (_baseComparer.Compare(y, "c") == 0)
                    return 1;
            */

            string a = x;
            string b = y;

            if(a.StartsWith("."))
                a = a.Substring(1);
            if (b.StartsWith("."))
                b = b.Substring(1);

            return _baseComparer.Compare(a, b);
        }
    }
}
