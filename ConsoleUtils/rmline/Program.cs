using System.Text;
using System.Text.RegularExpressions;

// rmline file.ext:LINE [file.ext:LINE ...]
// Deletes the given line(s) from a file, e.g. for "known_hosts:125" as reported by ssh.

const int ExitOk = 0, ExitError = 1;

if (args.Length == 0 || args.Any(a => a is "-h" or "--help" or "/?"))
{
    Console.WriteLine("Usage: rmline <file>:<line> [<file>:<line> ...]");
    Console.WriteLine();
    Console.WriteLine("Deletes the given line from a file (line numbers start at 1).");
    Console.WriteLine("Several lines of the same file are deleted in one go, the numbers refer to the original file.");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine(@"  rmline ~/.ssh/known_hosts:125");
    Console.WriteLine(@"  rmline C:\Users\me\.ssh\known_hosts:125 notes.txt:3");
    return args.Length == 0 ? ExitError : ExitOk;
}

// "<path>:<line>", the path itself may contain colons (C:\...), so split at the last one
var spec = new Regex(@"^(?<path>.+):(?<line>\d+)$", RegexOptions.Compiled);
var requests = new Dictionary<string, SortedSet<int>>(StringComparer.OrdinalIgnoreCase);

foreach (var arg in args)
{
    var m = spec.Match(arg.Trim().Trim('"'));
    if (!m.Success || !int.TryParse(m.Groups["line"].Value, out int lineNo) || lineNo < 1)
    {
        Console.Error.WriteLine($"Invalid argument \"{arg}\", expected <file>:<line> with a line number >= 1.");
        return ExitError;
    }

    var path = m.Groups["path"].Value;
    if (path == "~" || path.StartsWith("~/") || path.StartsWith(@"~\"))
        path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path.Length > 2 ? path[2..] : "");

    path = Path.GetFullPath(path);
    if (!requests.TryGetValue(path, out var set))
        requests[path] = set = new SortedSet<int>();
    set.Add(lineNo);
}

int result = ExitOk;
foreach (var (path, lineNumbers) in requests)
{
    try
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("File not found.");

        var removed = RemoveLines(path, lineNumbers);
        foreach (var (number, text) in removed)
            Console.WriteLine($"{path}:{number}: removed \"{text}\"");
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidDataException)
    {
        Console.Error.WriteLine($"{path}: {ex.Message}");
        result = ExitError;
    }
}
return result;


// Removes the lines and replaces the file atomically. Returns the removed lines with their numbers.
// ASCII-compatible encodings (UTF-8, ANSI, ...) are processed as bytes, so nothing is re-encoded and
// the line endings (CRLF/LF) of all other lines stay exactly as they are.
static List<(int Number, string Text)> RemoveLines(string path, SortedSet<int> lineNumbers)
{
    byte[] data = File.ReadAllBytes(path);
    Encoding encoding = DetectWideEncoding(data, out int bomLength) ?? new UTF8Encoding(false);
    bool wide = encoding is UnicodeEncoding or UTF32Encoding;

    // split into lines, keeping the terminators
    var lines = wide ? SplitWide(data, bomLength, encoding) : SplitBytes(data);

    if (lineNumbers.Max > lines.Count)
        throw new InvalidDataException($"File has only {lines.Count} line(s), cannot delete line {lineNumbers.Max}.");

    var removed = new List<(int, string)>();
    using var output = new MemoryStream(data.Length);
    output.Write(data, 0, wide ? bomLength : 0);
    if (!wide && data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
        output.Write(data, 0, 3); // UTF-8 BOM stays in front of the first line

    int skip = !wide && data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF ? 3 : 0;

    for (int i = 0; i < lines.Count; i++)
    {
        var (start, length) = lines[i];
        if (lineNumbers.Contains(i + 1))
        {
            var seg = data.AsSpan(start, length);
            var text = (wide ? encoding : Encoding.UTF8).GetString(seg).TrimStart('﻿').TrimEnd('\r', '\n', '\0');
            removed.Add((i + 1, text.Length > 80 ? text[..77] + "..." : text));
            continue;
        }
        // the first line of a UTF-8 file starts after the BOM that was written above
        int from = start, count = length;
        if (i == 0 && skip > 0) { from += skip; count -= skip; }
        output.Write(data, from, count);
    }

    // the last remaining line may have lost its terminator when the removed line was the last one – that is fine
    var tmp = path + ".rmline.tmp";
    try
    {
        File.WriteAllBytes(tmp, output.ToArray());
        File.Replace(tmp, path, null, ignoreMetadataErrors: true);
    }
    finally
    {
        if (File.Exists(tmp))
            File.Delete(tmp);
    }
    return removed;
}

static Encoding? DetectWideEncoding(byte[] d, out int bomLength)
{
    bomLength = 0;
    if (d.Length >= 4 && d[0] == 0xFF && d[1] == 0xFE && d[2] == 0 && d[3] == 0) { bomLength = 4; return new UTF32Encoding(false, true); }
    if (d.Length >= 4 && d[0] == 0 && d[1] == 0 && d[2] == 0xFE && d[3] == 0xFF) { bomLength = 4; return new UTF32Encoding(true, true); }
    if (d.Length >= 2 && d[0] == 0xFF && d[1] == 0xFE) { bomLength = 2; return new UnicodeEncoding(false, true); }
    if (d.Length >= 2 && d[0] == 0xFE && d[1] == 0xFF) { bomLength = 2; return new UnicodeEncoding(true, true); }
    return null;
}

static List<(int Start, int Length)> SplitBytes(byte[] data)
{
    var list = new List<(int, int)>();
    int start = 0;
    for (int i = 0; i < data.Length; i++)
    {
        if (data[i] == (byte)'\n')
        {
            list.Add((start, i - start + 1));
            start = i + 1;
        }
    }
    if (start < data.Length)
        list.Add((start, data.Length - start)); // last line without terminator
    return list;
}

static List<(int Start, int Length)> SplitWide(byte[] data, int bomLength, Encoding encoding)
{
    int unit = encoding is UTF32Encoding ? 4 : 2;
    bool bigEndian = encoding is UnicodeEncoding u ? u.GetPreamble()[0] == 0xFE : encoding.GetPreamble()[0] == 0;
    var list = new List<(int, int)>();
    int start = bomLength;
    for (int i = bomLength; i + unit <= data.Length; i += unit)
    {
        // '\n' is 0x0A in the low byte of the code unit
        bool isLf = bigEndian
            ? data[i + unit - 1] == 0x0A && data.Skip(i).Take(unit - 1).All(b => b == 0)
            : data[i] == 0x0A && data.Skip(i + 1).Take(unit - 1).All(b => b == 0);
        if (isLf)
        {
            list.Add((start, i + unit - start));
            start = i + unit;
        }
    }
    if (start < data.Length)
        list.Add((start, data.Length - start));
    return list;
}
