using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

public class FileAndDirectoryFilter
{
    public enum FileAndDirectoryMode
    {
        Files = 1,
        Directories = 2,
        ListDirectoryEntries = 4
    }

    public static StringCollection Get(string[] paths, FileAndDirectoryMode mode)
    {
        StringCollection result = new StringCollection();
        string filter = null;
        foreach (string path in paths)
        {

            if (File.Exists(path) && (mode.HasFlag(FileAndDirectoryMode.Files) || mode.HasFlag(FileAndDirectoryMode.ListDirectoryEntries)))
            {
                result.Add(Path.GetFullPath(path));
                return result;
            }
            else if (Directory.Exists(path) && mode.HasFlag(FileAndDirectoryMode.Directories) && !mode.HasFlag(FileAndDirectoryMode.ListDirectoryEntries))
            {
                result.Add(Path.GetFullPath(path));
                return result;
            }
            else
            {
                string[] pathArr = path.Split('\\');
                string dir = "";
                if (!Directory.Exists(path))
                {
                    dir = string.Join(@"\", pathArr.Take(pathArr.Count() - 1).ToArray());
                    filter = pathArr.Last();
                }
                else
                {
                    dir = path;
                    filter = null;
                }
                if (dir == String.Empty)
                    dir = Path.GetFullPath(Environment.CurrentDirectory);
                else if (dir.Length == 2 && dir[1] == ':')      // driverletter
                    dir += "\\*";
                else
                {
                    try
                    {
                        dir = Path.GetFullPath(dir);
                    }
                    catch (Exception ex)
                    {
                        ;
                    }
                }

                if (Directory.Exists(dir))
                {
                    //dir += @"\";

                    string[] f;
                    string[] d;
                    if (filter != null)
                    {
                        f = Directory.GetFiles(dir, filter, SearchOption.TopDirectoryOnly);
                        d = Directory.GetDirectories(dir, filter, SearchOption.TopDirectoryOnly);
                    }
                    else
                    {
                        f = Directory.GetFiles(dir);
                        d = Directory.GetDirectories(dir);
                    }
                    result.AddRange(d);
                    result.AddRange(f);
                }
                else
                {
                    // do nothing
                }


            }
        }
        return result;
    }
}
