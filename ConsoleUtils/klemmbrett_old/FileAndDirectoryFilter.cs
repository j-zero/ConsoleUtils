using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

namespace klemmbrett
{
    internal class FileAndDirectoryFilter
    {
        public enum FileAndDirectoryMode
        {
            Files = 1,
            Directories = 2
        }

        public static StringCollection Get(string[] paths, FileAndDirectoryMode mode)
        {
            StringCollection result = new StringCollection();
            foreach (string path in paths)
            {

                if (File.Exists(path) && mode.HasFlag(FileAndDirectoryMode.Files))
                {
                    result.Add(Path.GetFullPath(path));
                    return result;
                }
                else if (Directory.Exists(path) && mode.HasFlag(FileAndDirectoryMode.Directories))
                {
                    result.Add(Path.GetFullPath(path));
                    return result;
                }
                else
                {
                    string[] pathArr = path.Split('\\');

                    var dir = string.Join(@"\", pathArr.Take(pathArr.Count() - 1).ToArray());

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

                        var filter = pathArr.Last();

                        string[] f = Directory.GetFiles(dir, filter, SearchOption.TopDirectoryOnly);
                        string[] d = Directory.GetDirectories(dir, filter, SearchOption.TopDirectoryOnly);

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
}
