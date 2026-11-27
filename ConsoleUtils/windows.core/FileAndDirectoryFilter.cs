#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class FileAndDirectoryFilter
{
    public static bool isUNCServer = false;

    public static FilesystemEntryInfo[] GetFilesFromFilter(string filter)
    {
        var base_pat = SplitBaseDirAndPattern(filter);

        if (!isUNCServer)
            return GetMatchingItems(base_pat[1].Split(Path.DirectorySeparatorChar), base_pat[0]);
        else
            return GetShares(base_pat[0]);
    }

    private static string[] SplitBaseDirAndPattern(string InputPath)
    {
        isUNCServer = false;
        InputPath = InputPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        string[] path_arr = InputPath.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

        if (path_arr.Length == 0) // "", "\" or "\\" => root of the current drive
            return new string[] { Path.GetPathRoot(Environment.CurrentDirectory), "" };

        string base_directory = "";
        int i = 1;

        if (InputPath.StartsWith("\\") && path_arr.Length > 1)
        {  // UNC
            base_directory = $"\\\\{path_arr[0]}\\{path_arr[1]}";
            i = 2;
        }
        else if (InputPath.StartsWith("\\") && path_arr.Length == 1)
        {
            isUNCServer = true;
            return new string[] { path_arr[0], "" };
        }
        else
            base_directory = path_arr[0];

        if (!Directory.Exists(base_directory))
            return new string[] { Environment.CurrentDirectory, InputPath };

        while (i < path_arr.Length)
        {
            string combined_path = base_directory + Path.DirectorySeparatorChar.ToString() + path_arr[i];
            if (!combined_path.Contains('*') && !combined_path.Contains('?') && Directory.Exists(combined_path))
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

    private static FilesystemEntryInfo[] GetShares(string server)
    {
        // todo pattern
        List<FilesystemEntryInfo> entries = new List<FilesystemEntryInfo>();

        try
        {
            var shares = NetworkShareHelper.EnumNetShares(server);
            foreach (var share in shares)
                entries.Add(new FilesystemEntryInfo(server, share.Name, share.Description, share.Type));
        }
        catch (Exception)
        {
            // server not reachable / access denied => no shares
        }

        return entries.ToArray();
    }

    private static FilesystemEntryInfo ErrorEntry(string path, Exception ex)
    {
        FilesystemEntryInfo fei = new FilesystemEntryInfo(path);
        fei.LastException = ex;
        fei.Error = true;
        return fei;
    }

    private static FilesystemEntryInfo[] GetMatchingItems(string[] pattern, string base_directory)
    {
        string current_pattern = pattern[0];
        Regex matcher = BuildMatcher(current_pattern); // null => everything matches
        List<FilesystemEntryInfo> entries = new List<FilesystemEntryInfo>();

        if (pattern.Length > 1) // mitten im pfad können keine dateien stehen (außer archive)
        {
            SearchOption so = current_pattern == "**" ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string[] new_pattern = pattern.Skip(1).ToArray();

            try
            {
                foreach (string d in Directory.EnumerateDirectories(base_directory, "*", so))
                {
                    if (!IsMatching(matcher, Path.GetFileName(d)))
                        continue;
                    entries.AddRange(GetMatchingItems(new_pattern, d));
                }
            }
            catch (Exception ex)
            {
                entries.Add(ErrorEntry(base_directory, ex));
            }
        }
        else
        {
            bool expandDirectories = current_pattern == "*" || current_pattern == "**";

            try
            {
                foreach (FileSystemInfo info in new DirectoryInfo(base_directory).EnumerateFileSystemInfos())
                {
                    if (!IsMatching(matcher, info.Name))
                        continue;

                    bool isDir = (info.Attributes & FileAttributes.Directory) != 0;

                    if (isDir && expandDirectories)
                    {
                        // "list *" shows the content of the directories instead of the directories itself
                        try
                        {
                            foreach (FileSystemInfo sub in ((DirectoryInfo)info).EnumerateFileSystemInfos())
                                if (IsMatching(matcher, sub.Name))
                                    entries.Add(new FilesystemEntryInfo(sub));
                        }
                        catch (Exception ex)
                        {
                            entries.Add(ErrorEntry(info.FullName, ex));
                        }
                    }
                    else
                    {
                        entries.Add(new FilesystemEntryInfo(info));
                    }
                }
            }
            catch (Exception ex)
            {
                entries.Add(ErrorEntry(base_directory, ex));
            }
        }

        return entries.ToArray();
    }

    private static bool IsMatching(Regex matcher, string name)
    {
        return matcher == null || matcher.IsMatch(name);
    }

    private static Regex BuildMatcher(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return null;

        string p = "^" + Regex.Escape(pattern).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        return new Regex(p, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);
    }
}
