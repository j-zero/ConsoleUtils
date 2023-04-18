﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public class PathHelper
{
    public static string GetSpecialFolder(Environment.SpecialFolder Folder, string FolderName, string prefix = "")
    {
        string folder = GuaranteeBackslash(Path.Combine(Environment.GetFolderPath(Folder), prefix, FolderName));
        Directory.CreateDirectory(folder);
        return folder;
    }

    public static string GuaranteeBackslash(string Path)
    {
        return Path.EndsWith("\\") ? Path : Path + "\\";
    }
    public static string CleanFileNameFromString(string input)
    {
        string result = input;
        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
        {
            result = result.Replace(c, '_');
        }
        return result;
    }
    public static string GetRelativePath(string fromPath, string toPath)
    {
        int fromAttr = GetPathAttribute(fromPath);
        int toAttr = GetPathAttribute(toPath);

        StringBuilder path = new StringBuilder(260); // MAX_PATH
        if (PathRelativePathTo(
            path,
            fromPath,
            fromAttr,
            toPath,
            toAttr) == 0)
        {
            throw new ArgumentException("Paths must have a common prefix \"" + fromPath + "\" -> \"" + toPath + "\"");
        }
        return path.ToString();
    }

    private static int GetPathAttribute(string path)
    {
        DirectoryInfo di = new DirectoryInfo(path);
        if (di.Exists)
        {
            return FILE_ATTRIBUTE_DIRECTORY;
        }

        FileInfo fi = new FileInfo(path);
        if (fi.Exists)
        {
            return FILE_ATTRIBUTE_NORMAL;
        }

        throw new FileNotFoundException();
    }

    private const int FILE_ATTRIBUTE_DIRECTORY = 0x10;
    private const int FILE_ATTRIBUTE_NORMAL = 0x80;

    [DllImport("shlwapi.dll", SetLastError = true)]
    private static extern int PathRelativePathTo(StringBuilder pszPath,
        string pszFrom, int dwAttrFrom, string pszTo, int dwAttrTo);
}

