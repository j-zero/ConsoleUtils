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

        //

        if (!isUNCServer)
            return GetMatchingItems(base_pat[1].Split(Path.DirectorySeparatorChar), base_pat[0]);
        else
            return GetShares(base_pat[0]);




    }

    private static string[] SplitBaseDirAndPattern(string InputPath)
    {
        isUNCServer = false;
        string[] path_arr = InputPath.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

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

        if(!Directory.Exists(base_directory))
            return new string[] { Environment.CurrentDirectory, InputPath };

        

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

    //private static FilesystemEntryInfo[] GetShares(string[] pattern, string base_directory)
    private static FilesystemEntryInfo[] GetShares(string server)
    {
        // todo pattern
        List<FilesystemEntryInfo> entries = new List<FilesystemEntryInfo>();
        
        var shares = NetworkShareHelper.EnumNetShares(server);
        // Todo exception
        foreach (var share in shares)
        {
            var fei = new FilesystemEntryInfo(server, share.Name, share.Description, share.Type);
            entries.Add(fei);
        }

        return entries.ToArray();
    }

    private static FilesystemEntryInfo[] GetMatchingItems(string[] pattern, string base_directory)
    {
        string current_pattern = pattern[0];
        List<FilesystemEntryInfo> entries = new List<FilesystemEntryInfo>();

        if (pattern.Length > 1) // mitten im pfad können keinen dateien stehen (außer archive)
        {
            SearchOption so = SearchOption.TopDirectoryOnly;
            if (current_pattern == "**")
                so = SearchOption.AllDirectories;

            string[] new_pattern = pattern.Skip(1).ToArray();
            string[] dirs = new string[] { };

                dirs = Directory.GetDirectories(base_directory, "*", so).Where(d => FileOrDirNameIsMatching(d, current_pattern)).ToArray();
            foreach (string d in dirs)
            {
                entries.AddRange(GetMatchingItems(new_pattern, d));
            }
        }
        else
        {
            string[] dirs = new string[] {};
            string[] files = new string[] { };
            try
            {
                    dirs = Directory.GetDirectories(base_directory).Where(d => FileOrDirNameIsMatching(d, current_pattern)).ToArray();
                    files = Directory.GetFiles(base_directory).Where(d => FileOrDirNameIsMatching(d, current_pattern)).ToArray();
            }
            catch (Exception ex)
            {
                FilesystemEntryInfo fei = new FilesystemEntryInfo(base_directory);
                fei.LastException = ex;
                fei.Error = true;
                entries.Add(fei);
            }
            


            foreach (string d in dirs)
            {  // todo, do something??
                FilesystemEntryInfo fei = new FilesystemEntryInfo(d);

                if (current_pattern == "*")
                {
                    //entries.AddRange(GetMatchingItems(new string[] { current_pattern }, d));
                    try
                    {
                        string[] sub_dirs = Directory.GetDirectories(d).Where(sd => FileOrDirNameIsMatching(sd, current_pattern)).ToArray();
                        string[] sub_files = Directory.GetFiles(d).Where(sd => FileOrDirNameIsMatching(sd, current_pattern)).ToArray();

                        foreach (string ssd in sub_dirs)
                            entries.Add(new FilesystemEntryInfo(ssd));
                        foreach (string ssf in sub_files)
                            entries.Add(new FilesystemEntryInfo(ssf));

                    }
                    catch (UnauthorizedAccessException uaae)
                    {
                        fei.LastException = uaae;
                        fei.Error = true;
                    }
                    catch (Exception ex)
                    {
                        fei.LastException = ex;
                        fei.Error = true;
                    }
                }
                else if (current_pattern == "**")
                {
                    //entries.AddRange(GetMatchingItems(new string[] { current_pattern }, d));
                    try
                    {
                        string[] sub_dirs = Directory.GetDirectories(d).Where(sd => FileOrDirNameIsMatching(sd, current_pattern)).ToArray();
                        string[] sub_files = Directory.GetFiles(d).Where(sd => FileOrDirNameIsMatching(sd, current_pattern)).ToArray();

                        foreach (string ssd in sub_dirs)
                            entries.Add(new FilesystemEntryInfo(ssd));
                        foreach (string ssf in sub_files)
                            entries.Add(new FilesystemEntryInfo(ssf));

                    }
                    catch (UnauthorizedAccessException uaae)
                    {
                        fei.LastException = uaae;
                        fei.Error = true;
                    }
                    catch (Exception ex)
                    {
                        fei.LastException = ex;
                        fei.Error = true;
                    }
                }
                else
                {

                    entries.Add(fei);
                }
            }
            foreach (string f in files) // todo, do something??{
                entries.Add(new FilesystemEntryInfo(f));
            

        


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
