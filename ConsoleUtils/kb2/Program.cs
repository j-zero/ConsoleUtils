using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using Pastel;

namespace klemmbrett
{
    /// <summary>Error that is shown to the user without a stack trace.</summary>
    internal class UserError : Exception
    {
        public UserError(string message) : base(message) { }
    }

    internal class Program
    {
        static CmdParser cmd;
        static bool force, quiet, all, trim;

        static readonly string color1 = ColorTheme.Default1;
        static readonly string color2 = ColorTheme.Default2;

        enum Kind { Text, Html, Rtf, Csv }

        [STAThread] // the clipboard needs a single threaded apartment
        static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            cmd = new CmdParser(args)
            {
                { "help", "h", CmdCommandTypes.VERB, "Show this help" },
                { "show", "", CmdCommandTypes.VERB, "Show what is on the clipboard" },
                { "info", "", CmdCommandTypes.VERB, "List all clipboard formats with their size" },
                { "get", "", CmdCommandTypes.VERB, "Write the clipboard to stdout (text, html, rtf, csv, files)" },
                { "string", "s", CmdCommandTypes.VERB, "Copy the arguments as text" },
                { "text", "t", CmdCommandTypes.VERB, "Copy text from a file or stdin / write the text" },
                { "unicode", "u", CmdCommandTypes.VERB, "Same as text" },
                { "html", "", CmdCommandTypes.VERB, "Copy HTML from a file or stdin / write the HTML" },
                { "rtf", "", CmdCommandTypes.VERB, "Copy RTF from a file or stdin / write the RTF" },
                { "csv", "", CmdCommandTypes.VERB, "Copy CSV from a file or stdin / write the CSV" },
                { "image", "i", CmdCommandTypes.VERB, "Copy an image file / show image information" },
                { "path", "P", CmdCommandTypes.VERB, "Copy the full path of files (or the current directory)" },
                { "copy", "cp", CmdCommandTypes.VERB, "Copy files and directories to the clipboard" },
                { "cut", "", CmdCommandTypes.VERB, "Cut files and directories to the clipboard" },
                { "paste", "p", CmdCommandTypes.VERB, "Paste files into a directory (or write the text)" },
                { "move", "mv", CmdCommandTypes.VERB, "Paste files and remove the originals" },
                { "save", "S", CmdCommandTypes.VERB, "Save the clipboard to a file" },
                { "load", "L", CmdCommandTypes.VERB, "Copy a file, the type is chosen by its extension" },
                { "raw", "", CmdCommandTypes.VERB, "Write the raw data of one format" },
                { "dump", "", CmdCommandTypes.VERB, "Hex dump of all formats" },
                { "clear", "", CmdCommandTypes.VERB, "Empty the clipboard" },

                { "force", "f", CmdCommandTypes.FLAG, "Overwrite files, create directories" },
                { "quiet", "q", CmdCommandTypes.FLAG, "No messages" },
                { "all", "a", CmdCommandTypes.FLAG, "show: also HTML/RTF/CSV, get html: with CF_HTML header" },
                { "trim", "", CmdCommandTypes.FLAG, "Remove the trailing line break of the input" },
                { "as", "", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, null }
                }, "raw: hex, bin, ascii, utf8, utf16 or bytes" },

                { "args", "", CmdCommandTypes.MULTIPE_PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, null }
                }, "Arguments" },
            };
            cmd.DefaultParameter = "args";

            try
            {
                cmd.Parse();
                force = cmd.HasFlag("force");
                quiet = cmd.HasFlag("quiet");
                all = cmd.HasFlag("all");
                trim = cmd.HasFlag("trim");

                return Run(Arguments());
            }
            catch (UserError ex)
            {
                ConsoleHelper.WriteError(ex.Message);
                return 1;
            }
            catch (ArgumentException ex)
            {
                ConsoleHelper.WriteError(ex.Message);
                return 2;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception
                                          or System.Runtime.InteropServices.ExternalException or OutOfMemoryException)
            {
                ConsoleHelper.WriteError(ex.Message);
                return 1;
            }
        }

        static string[] Arguments() => cmd["args"].Strings.Where(s => s != null).ToArray();

        static int Run(string[] a)
        {
            switch (cmd.FirstVerb)
            {
                case null:
                    if (a.Length > 0)
                        return Load(a);                                   // kb2 file.png
                    if (Console.IsInputRedirected)
                        return TextVerb(Kind.Text, a);                    // echo hello | kb2
                    return Show();
                case "help": return Help();
                case "show": return Show();
                case "info": return Info();
                case "get": return Get(a);
                case "string": return CopyString(a);
                case "text":
                case "unicode": return TextVerb(Kind.Text, a);
                case "html": return TextVerb(Kind.Html, a);
                case "rtf": return TextVerb(Kind.Rtf, a);
                case "csv": return TextVerb(Kind.Csv, a);
                case "image": return ImageVerb(a);
                case "path": return CopyPath(a);
                case "copy": return CopyFiles(a, false);
                case "cut": return CopyFiles(a, true);
                case "paste": return Paste(a, false);
                case "move": return Paste(a, true);
                case "save": return Save(a);
                case "load": return Load(a);
                case "raw": return Raw(a);
                case "dump": return Dump();
                case "clear": Clip.Clear(); Message("Clipboard cleared."); return 0;
                default: throw new UserError($"Unknown command \"{cmd.FirstVerb}\".");
            }
        }

        // ---- output helpers ------------------------------------------------------------------

        static void Message(string text)
        {
            if (!quiet)
                Console.WriteLine(text);
        }

        static void Header(string text)
        {
            if (!Console.IsOutputRedirected)
                Console.WriteLine(text.Pastel(color1));
        }

        static void WriteContent(string text)
        {
            Console.Out.Write(text);
            if (!Console.IsOutputRedirected && !text.EndsWith('\n'))
                Console.WriteLine();
        }

        static string Size(long bytes) => bytes < 0 ? "-" : UnitHelper.CalculateHumanReadableSize((ulong)bytes);

        // ---- input helpers -------------------------------------------------------------------

        static string ReadFileText(string path)
        {
            var bytes = File.ReadAllBytes(path);
            try
            {
                return new UTF8Encoding(false, true).GetString(bytes).TrimStart('﻿');
            }
            catch (DecoderFallbackException)
            {
                return Encoding.Latin1.GetString(bytes); // legacy ANSI file
            }
        }

        static string ReadStdin()
        {
            using var reader = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false));
            return reader.ReadToEnd();
        }

        /// <summary>Text from the file argument or from stdin; null if there is no input at all.</summary>
        static string ReadInput(string[] a)
        {
            string text = null;
            if (a.Length > 0)
            {
                var path = Path.GetFullPath(a[0]);
                if (!File.Exists(path))
                    throw new UserError($"File \"{path}\" not found. (To copy text from the command line use: kb2 string \"...\")");
                text = ReadFileText(path);
            }
            else if (Console.IsInputRedirected)
            {
                text = ReadStdin();
            }

            if (text != null && trim)
                text = text.TrimEnd('\r', '\n');
            return text;
        }

        /// <summary>Resolves files, directories and wildcards to full paths.</summary>
        static List<string> ResolvePaths(string[] patterns, out List<string> missing)
        {
            var found = new List<string>();
            missing = new List<string>();
            foreach (var pattern in patterns)
            {
                var full = Path.GetFullPath(pattern);
                if (pattern.Contains('*') || pattern.Contains('?'))
                {
                    var dir = Path.GetDirectoryName(full) ?? Environment.CurrentDirectory;
                    var hits = Directory.Exists(dir)
                        ? Directory.EnumerateFileSystemEntries(dir, Path.GetFileName(full)).ToList()
                        : new List<string>();
                    if (hits.Count == 0)
                        missing.Add(pattern);
                    found.AddRange(hits);
                }
                else if (File.Exists(full) || Directory.Exists(full))
                {
                    found.Add(full.Length > 3 ? full.TrimEnd('\\', '/') : full);
                }
                else
                {
                    missing.Add(pattern);
                }
            }
            return found.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        // ---- commands: read ------------------------------------------------------------------

        static int Show()
        {
            var files = Clip.GetFiles(out bool cut);
            bool shown = false;

            if (files.Count > 0)
            {
                Header(cut ? "Files/Directories (cut):" : "Files/Directories (copied):");
                foreach (var f in files)
                {
                    bool isFile = File.Exists(f), isDir = Directory.Exists(f);
                    string marker = isFile ? "f" : isDir ? "d" : "?";
                    string color = isDir ? ColorTheme.Directory : isFile ? ColorTheme.File : ColorTheme.Error1;
                    Console.WriteLine($"{marker.Pastel(ColorTheme.Comment)} {f.Pastel(color)}");
                }
                shown = true;
            }
            else if (Clip.HasText)
            {
                Header("Text:");
                WriteContent(Clip.GetText());
                shown = true;
            }

            if (Clip.HasImage)
            {
                using var image = Clip.GetImage();
                Header(image == null ? "Image" : $"Image ({image.Width}x{image.Height}, {image.HorizontalResolution:0}dpi):");
                shown = true;
            }

            if (all)
            {
                if (Clip.HasHtml) { Header("HTML:"); WriteContent(Clip.GetHtmlFragment()); shown = true; }
                if (Clip.HasRtf) { Header("RTF:"); WriteContent(Clip.GetRtf()); shown = true; }
                if (Clip.HasCsv) { Header("CSV:"); WriteContent(Clip.GetCsv()); shown = true; }
            }

            if (!shown)
                Message(Clip.IsEmpty ? "The clipboard is empty." : "The clipboard has no text, image or files. Use \"kb2 info\" to see its formats.");
            return 0;
        }

        static int Info()
        {
            var formats = NativeClipboard.List(true);
            if (formats.Count == 0)
            {
                Message("The clipboard is empty.");
                return 0;
            }

            Console.WriteLine($"{"ID".PadRight(8)}{"Size".PadLeft(10)}  Format".Pastel(ColorTheme.Comment));
            foreach (var f in formats)
                Console.WriteLine($"{("0x" + f.Id.ToString("X4")).PadRight(8).Pastel(color2)}{Size(f.Size).PadLeft(10)}  {f.Name.Pastel(color1)}");
            return 0;
        }

        static int Get(string[] a)
        {
            string what = a.Length > 0 ? a[0].ToLowerInvariant() : "text";
            switch (what)
            {
                case "text":
                case "unicode": return GetText(Kind.Text);
                case "html": return GetText(Kind.Html);
                case "rtf": return GetText(Kind.Rtf);
                case "csv": return GetText(Kind.Csv);
                case "files":
                case "file":
                    var files = Clip.GetFiles(out _);
                    if (files.Count == 0)
                        throw new UserError("The clipboard contains no files.");
                    foreach (var f in files)
                        Console.WriteLine(f);
                    return 0;
                default:
                    throw new UserError($"Unknown format \"{a[0]}\". Use text, html, rtf, csv or files (for other formats see: kb2 raw).");
            }
        }

        static int GetText(Kind kind)
        {
            switch (kind)
            {
                case Kind.Text:
                    if (!Clip.HasText) throw new UserError("The clipboard contains no text.");
                    WriteContent(Clip.GetText());
                    break;
                case Kind.Html:
                    if (!Clip.HasHtml) throw new UserError("The clipboard contains no HTML.");
                    WriteContent(all ? Clip.GetHtmlRaw() : Clip.GetHtmlFragment());
                    break;
                case Kind.Rtf:
                    if (!Clip.HasRtf) throw new UserError("The clipboard contains no RTF.");
                    WriteContent(Clip.GetRtf());
                    break;
                case Kind.Csv:
                    if (!Clip.HasCsv) throw new UserError("The clipboard contains no CSV.");
                    WriteContent(Clip.GetCsv());
                    break;
            }
            return 0;
        }

        static int Raw(string[] a)
        {
            if (a.Length == 0)
                throw new UserError("Which format? Give the id or the name, see \"kb2 info\". Example: kb2 raw 0x0D --as utf16");

            uint id = NativeClipboard.FindFormat(a[0])
                ?? throw new UserError($"Format \"{a[0]}\" is not on the clipboard. See \"kb2 info\".");
            byte[] bytes = NativeClipboard.GetBytes(id)
                ?? throw new UserError($"Format {NativeClipboard.GetFormatName(id)} has no readable data (it is a handle).");

            string mode = cmd["as"].String?.ToLowerInvariant() ?? (Console.IsOutputRedirected ? "bytes" : "hex");
            switch (mode)
            {
                case "hex": ConsoleHelper.HexDump(bytes); break;
                case "bin": ConsoleHelper.BinDump(bytes); break;
                case "ascii": WriteContent(Encoding.ASCII.GetString(bytes).TrimEnd('\0')); break;
                case "utf8": WriteContent(Encoding.UTF8.GetString(bytes).TrimEnd('\0')); break;
                case "utf16":
                case "unicode": WriteContent(Encoding.Unicode.GetString(bytes).TrimEnd('\0')); break;
                case "bytes":
                    using (var stdout = Console.OpenStandardOutput())
                        stdout.Write(bytes, 0, bytes.Length);
                    break;
                default: throw new UserError($"Unknown output \"{mode}\", use hex, bin, ascii, utf8, utf16 or bytes.");
            }
            return 0;
        }

        static int Dump()
        {
            var formats = NativeClipboard.List(true);
            if (formats.Count == 0)
            {
                Message("The clipboard is empty.");
                return 0;
            }
            foreach (var f in formats)
            {
                Console.WriteLine($"0x{f.Id:X4}: {f.Name} ({Size(f.Size)})".Pastel(color1));
                var bytes = NativeClipboard.GetBytes(f.Id);
                if (bytes == null)
                    Console.WriteLine("  (no data, handle)".Pastel(ColorTheme.Comment));
                else
                    ConsoleHelper.HexDump(bytes);
                Console.WriteLine();
            }
            return 0;
        }

        // ---- commands: write -----------------------------------------------------------------

        static int CopyString(string[] a)
        {
            if (a.Length == 0)
                throw new UserError("Nothing to copy. Usage: kb2 string \"some text\"");
            string text = string.Join(" ", a);
            Clip.SetText(text);
            Message($"Copied {text.Length} characters.");
            return 0;
        }

        static int TextVerb(Kind kind, string[] a)
        {
            string input = ReadInput(a);
            if (input == null)
                return GetText(kind); // nothing to copy, so show what is there
            if (input.Length == 0)
                throw new UserError("The input is empty, the clipboard was not changed.");

            switch (kind)
            {
                case Kind.Text: Clip.SetText(input); break;
                case Kind.Html: Clip.SetHtml(input); break;
                case Kind.Rtf: Clip.SetRtf(input); break;
                case Kind.Csv: Clip.SetCsv(input); break;
            }
            Message($"Copied {input.Length} characters as {kind.ToString().ToUpperInvariant()}.");
            return 0;
        }

        static void CopyImageFile(string path)
        {
            if (!File.Exists(path))
                throw new UserError($"File \"{path}\" not found.");
            try
            {
                using var file = Image.FromFile(path);
                using var copy = new Bitmap(file); // the file stays unlocked
                Clip.SetImage(copy);
                Message($"Copied image {copy.Width}x{copy.Height}.");
            }
            catch (OutOfMemoryException)
            {
                throw new UserError($"\"{path}\" is not a supported image.");
            }
        }

        static int ImageVerb(string[] a)
        {
            if (a.Length > 0)
            {
                CopyImageFile(Path.GetFullPath(a[0]));
                return 0;
            }

            if (!Clip.HasImage)
                throw new UserError("The clipboard contains no image. Usage: kb2 image file.png");
            using var image = Clip.GetImage();
            Console.WriteLine($"Image {image.Width}x{image.Height}, {image.HorizontalResolution:0}dpi. Save it with: kb2 save file.png");
            return 0;
        }

        static int CopyPath(string[] a)
        {
            string text = a.Length == 0
                ? Environment.CurrentDirectory
                : string.Join(Environment.NewLine, a.Select(Path.GetFullPath));
            Clip.SetText(text);
            Message($"Copied {(a.Length == 0 ? "current directory" : a.Length + " path(s)")}.");
            return 0;
        }

        static int CopyFiles(string[] a, bool cut)
        {
            if (a.Length == 0)
                throw new UserError($"Which files? Usage: kb2 {(cut ? "cut" : "copy")} file [file ...]  (wildcards are possible)");

            var files = ResolvePaths(a, out var missing);
            foreach (var m in missing)
                ConsoleHelper.WriteError($"\"{m}\" not found.");
            if (files.Count == 0)
                return 1;

            Clip.SetFiles(files, cut);
            Message($"{files.Count} {(files.Count == 1 ? "entry" : "entries")} {(cut ? "cut" : "copied")}.");
            return missing.Count > 0 ? 1 : 0;
        }

        static int Load(string[] a)
        {
            if (a.Length != 1)
                throw new UserError("Usage: kb2 load <file>");

            var path = Path.GetFullPath(a[0]);
            if (!File.Exists(path))
                throw new UserError($"\"{a[0]}\" is neither a command nor an existing file.");

            switch (Path.GetExtension(path).TrimStart('.').ToLowerInvariant())
            {
                case "html":
                case "htm": return TextVerb(Kind.Html, new[] { path });
                case "rtf": return TextVerb(Kind.Rtf, new[] { path });
                case "csv": return TextVerb(Kind.Csv, new[] { path });
                case "png":
                case "jpg":
                case "jpeg":
                case "gif":
                case "bmp":
                case "tif":
                case "tiff":
                case "webp":
                case "ico": CopyImageFile(path); return 0;
                default: return TextVerb(Kind.Text, new[] { path });
            }
        }

        // ---- save ----------------------------------------------------------------------------

        static int Save(string[] a)
        {
            if (a.Length != 1)
                throw new UserError("Usage: kb2 save <file>   (png, jpg, gif, bmp, tif for images; html, rtf, csv; anything else is text)");

            var path = Path.GetFullPath(a[0]);
            if (File.Exists(path) && !force)
                throw new UserError($"\"{path}\" already exists. Use --force to overwrite it.");

            string ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
            ImageFormat imageFormat = ext switch
            {
                "png" => ImageFormat.Png,
                "jpg" or "jpeg" => ImageFormat.Jpeg,
                "gif" => ImageFormat.Gif,
                "bmp" => ImageFormat.Bmp,
                "tif" or "tiff" => ImageFormat.Tiff,
                _ => null,
            };

            // an unknown extension of an image-only clipboard is saved as PNG
            if (imageFormat == null && !Clip.HasText && !Clip.HasHtml && Clip.HasImage)
                imageFormat = ImageFormat.Png;

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                throw new UserError($"Directory \"{dir}\" does not exist.");

            if (imageFormat != null)
            {
                if (!Clip.HasImage)
                    throw new UserError("The clipboard contains no image.");
                using var image = Clip.GetImage();
                image.Save(path, imageFormat);
                Message($"Saved {imageFormat} image to \"{path}\".");
                return 0;
            }

            string content = ext switch
            {
                "html" or "htm" => Clip.HasHtml ? Clip.GetHtmlFragment() : throw new UserError("The clipboard contains no HTML."),
                "rtf" => Clip.HasRtf ? Clip.GetRtf() : throw new UserError("The clipboard contains no RTF."),
                "csv" => Clip.HasCsv ? Clip.GetCsv() : throw new UserError("The clipboard contains no CSV."),
                _ => Clip.HasText ? Clip.GetText() : throw new UserError("The clipboard contains no text or image."),
            };
            File.WriteAllText(path, content, new UTF8Encoding(false));
            Message($"Saved {content.Length} characters to \"{path}\".");
            return 0;
        }

        // ---- paste ---------------------------------------------------------------------------

        static int Paste(string[] a, bool forceMove)
        {
            var files = Clip.GetFiles(out bool cut);
            if (files.Count == 0)
            {
                if (!forceMove && Clip.HasText)
                    return GetText(Kind.Text); // nothing to paste as file, so write the text
                throw new UserError("The clipboard contains no files to paste.");
            }

            string destination = a.Length > 0 ? Path.GetFullPath(a[0]) : Environment.CurrentDirectory;
            if (!Directory.Exists(destination))
            {
                if (!force)
                    throw new UserError($"Directory \"{destination}\" does not exist. Use --force to create it.");
                Directory.CreateDirectory(destination);
            }

            bool move = forceMove || cut;
            int errors = 0;
            foreach (var source in files)
            {
                try
                {
                    string name = Path.GetFileName(source.TrimEnd('\\', '/'));
                    string target = Path.Combine(destination, name);
                    bool isDir = Directory.Exists(source);

                    if (!isDir && !File.Exists(source))
                        throw new UserError($"\"{source}\" does not exist anymore.");
                    if (string.Equals(Path.GetFullPath(source), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
                        throw new UserError($"\"{name}\" is already in that directory.");
                    if (isDir && target.StartsWith(source.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase))
                        throw new UserError($"Cannot paste \"{name}\" into itself.");
                    if ((File.Exists(target) || Directory.Exists(target)) && !force)
                        throw new UserError($"\"{target}\" already exists. Use --force to overwrite it.");

                    if (isDir)
                    {
                        CopyDirectory(source, target);
                        if (move)
                            Directory.Delete(source, true);
                    }
                    else if (move)
                    {
                        File.Move(source, target, force);
                    }
                    else
                    {
                        File.Copy(source, target, force);
                    }
                    Message($"{source} -> {target}");
                }
                catch (Exception ex) when (ex is UserError or IOException or UnauthorizedAccessException)
                {
                    ConsoleHelper.WriteError(ex.Message);
                    errors++;
                }
            }

            if (cut && errors == 0)
                Clip.Clear(); // like Explorer: files can only be moved once
            return errors == 0 ? 0 : 1;
        }

        static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (var file in Directory.EnumerateFiles(source))
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), force);
            foreach (var dir in Directory.EnumerateDirectories(source))
                CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
        }

        // ---- help ----------------------------------------------------------------------------

        static int Help()
        {
            string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";
            Console.WriteLine($"kb2 {version} – clipboard for the console, part of {ConsoleHelper.GetVersionString()}");
            Console.WriteLine();
            Console.WriteLine($"Usage: kb2 [{"command".Pastel(color2)}] [{"options".Pastel(color2)}] [arguments]");
            Console.WriteLine("       command | kb2        copy stdin as text");
            Console.WriteLine("       kb2                  show the clipboard");
            Console.WriteLine("       kb2 file.png         same as: kb2 load file.png");

            Console.WriteLine($"\n{"Commands".Pastel(color2)}:");
            foreach (var c in cmd.SelectVerbs)
            {
                string names = c.Name + (string.IsNullOrEmpty(c.ShortName) ? "" : ", " + c.ShortName);
                Console.WriteLine($"  {names.PadRight(14).Pastel(color1)} {c.Description}");
            }

            Console.WriteLine($"\n{"Options".Pastel(color2)}:");
            foreach (var c in cmd.SelectOptions.Where(o => o.Name != "args"))
            {
                string names = "--" + c.Name + (string.IsNullOrEmpty(c.ShortName) ? "" : ", -" + c.ShortName);
                Console.WriteLine($"  {names.PadRight(14).Pastel(color1)} {c.Description}");
            }

            Console.WriteLine($"\n{"Examples".Pastel(color2)}:");
            Console.WriteLine("  kb2 string \"hello\"            copy text");
            Console.WriteLine("  git diff | kb2                copy the output of a command");
            Console.WriteLine("  kb2 get | sort                use the clipboard text in a pipeline");
            Console.WriteLine("  kb2 copy *.txt                copy files (then paste in Explorer or: kb2 paste)");
            Console.WriteLine("  kb2 cut old.log; kb2 move .   move a file");
            Console.WriteLine("  kb2 image logo.png            copy an image;  kb2 save out.png  saves it");
            Console.WriteLine("  kb2 html page.html            copy HTML with a plain text variant");
            Console.WriteLine("  kb2 raw HTML --as utf8        raw data of one format (see: kb2 info)");
            return 0;
        }
    }
}
