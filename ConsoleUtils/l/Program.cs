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
        static Dictionary<string, List<EntryInfo>> entries;

        static void Main(string[] args)
        {
            entries = new Dictionary<string, List<EntryInfo>>();
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


            if (path == null)
                path = Environment.CurrentDirectory;


            string[] found = FileAndDirectoryFilter.GetFilesFromFilter(path);

            foreach (string e in found)
            {
                EntryInfo ei = new EntryInfo(e);
                if (!entries.ContainsKey(ei.BaseDirectory))
                    entries.Add(ei.BaseDirectory, new List<EntryInfo>());
                entries[ei.BaseDirectory].Add(ei);
            }

            foreach (KeyValuePair<string, List<EntryInfo>> ei in entries)
            {
                Console.WriteLine(ei.Key);
                LongList(ei.Value);
            }
            //LongList();
            ;
        }


        static void SimpleList(List<EntryInfo> ei)
        {
            int width = Console.WindowWidth;
            //foreach(KeyValuePair<string, List<EntryInfo>> ei in entries)
                foreach(EntryInfo e in ei.OrderBy(o => o.Name).ToList())
                {
                    if (!e.HasHiddenAttribute || true)
                    {
                        if (Console.CursorLeft + e.Name.Length > width) Console.WriteLine(); // line break to not cut file names

                        Console.Write(e.Name.Pastel(e.ColorString));
                        /*
                        if (e.LinkTarget != null)
                            Console.Write(" -> " + PathHelper.GetRelativePath(path,e.LinkTarget));
                        */
                        Console.Write("\n");
                    }
                }
        }
        static void LongList(List<EntryInfo> ei)
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
                foreach (EntryInfo e in ei.OrderBy(o => o.Name).ToList())
                {
                    if (!e.HasHiddenAttribute || true)
                    {
                        if (Console.CursorLeft + e.Name.Length > width) Console.WriteLine(); // line break to not cut file names


                        //char[] mode = "------".ToCharArray();

                        string mode = "";
                        string minus = "-".Pastel("#606060");

                        mode += e.IsDirectory ? "d".Pastel(ColorTheme.Directory) : ".".Pastel(e.ColorString);
                        mode += e.HasArchiveAttribute ? "a".Pastel("#ffd700") : minus;
                        mode += !e.CanWrite ? "R".Pastel("#ff4500") : (e.HasReadOnlyAttribute ? "r".Pastel("#ff4500") : minus);
                        mode += e.HasHiddenAttribute ? "h".Pastel("#606060") : minus;
                        mode += e.HasSystemAttribute ? "s".Pastel("#ff8c00") : minus;
                        mode += e.IsLink ? "l".Pastel(ColorTheme.Symlink) : minus;

                        Console.Write(mode + " ");
                        Console.Write(e.Owner.Pastel("#ffd700") + "\t");
                        Console.Write(e.Name.Pastel(e.ColorString));

                        if (e.LinkTarget != null)
                            Console.Write(" -> " + PathHelper.GetRelativePath(path, e.LinkTarget));

                        Console.Write("\n");
                    }
                }
        }
    }
}
