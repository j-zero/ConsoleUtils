using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class FileAndDirectoryFilter
{
    public static string[] GetFilesFromFilter(string filter)
    {
        var base_pat = SplitBaseDirAndPattern(filter);
        var items = GetMatchingItems(base_pat[1].Split(Path.DirectorySeparatorChar), base_pat[0]);
        return items;
    }

    private static string[] SplitBaseDirAndPattern(string InputPath)
    {
        string[] path_arr = InputPath.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        string base_directory = path_arr[0];
        int i = 1;
        while (i < path_arr.Length)
        {
            string combined_path = base_directory + Path.DirectorySeparatorChar.ToString() + path_arr[i];
            if (Directory.Exists(combined_path) && !combined_path.Contains('*') && !combined_path.Contains('?'))
                base_directory = combined_path;
            else
                break;
            i++;
        }
        string pattern = string.Join(Path.DirectorySeparatorChar.ToString(), path_arr.Skip(i).ToArray());

        if (!base_directory.EndsWith(Path.DirectorySeparatorChar.ToString()))
            base_directory += Path.DirectorySeparatorChar;

        return new string[] { base_directory, pattern };
    }

    private static string[] GetMatchingItems(string[] pattern, string base_directory)
    {
        string current_pattern = pattern[0];
        List<string> entries = new List<string>();

        if (pattern.Length > 1) // mitten im pfad können keinen dateien stehen (außer archive)
        {
            string[] new_pattern = pattern.Skip(1).ToArray();
            foreach (string d in Directory.GetDirectories(base_directory).Where(d => FileOrDirNameIsMatching(d, current_pattern)))
            {
                entries.AddRange(GetMatchingItems(new_pattern, d));
            }
        }
        else
        { 
            string[] dirs = Directory.GetDirectories(base_directory).Where(d => FileOrDirNameIsMatching(d, current_pattern)).ToArray();
            string[] files = Directory.GetFiles(base_directory).Where(d => FileOrDirNameIsMatching(d, current_pattern)).ToArray();

            foreach (string d in dirs)
            {  // todo, do something??
                if (current_pattern == "*")
                {
                    //entries.AddRange(GetMatchingItems(new string[] { current_pattern }, d));
                    string[] sub_dirs = Directory.GetDirectories(d).Where(sd => FileOrDirNameIsMatching(sd, current_pattern)).ToArray();
                    string[] sub_files = Directory.GetFiles(d).Where(sd => FileOrDirNameIsMatching(sd, current_pattern)).ToArray();
                    entries.AddRange(sub_dirs);
                    entries.AddRange(sub_files);
                }
                else
                    entries.Add(Path.GetFullPath(d));
            }
            foreach (string f in files) // todo, do something??
                entries.Add(f);

        }

        return entries.ToArray();
    }

    private static bool FileOrDirNameIsMatching(string FullPath, string Pattern)
    {
        bool match = false;
        string Name = null;
        if (Pattern == string.Empty)
            return true;


        DirectoryInfo directoryInfo = new DirectoryInfo(FullPath);
        if (directoryInfo.Exists)
            Name = directoryInfo.Name;
        FileInfo fileInfo = new FileInfo(FullPath);
        if (fileInfo.Exists)
            Name = fileInfo.Name;

        if (Name != null)
            match = MatchWildCard(Pattern, Name);

        return match;
    }

    private static bool MatchWildCard(string pattern, string value)
    {
        string p = "^" + Regex.Escape(pattern).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        return Regex.IsMatch(value, p, RegexOptions.IgnoreCase);
    }

}
