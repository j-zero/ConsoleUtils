using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace klemmbrett
{
    internal class Program
    {

        static int _ErrorLevel = 0;
        static bool force = false;
        static bool silent = false;

        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                // Read STDIN
                if (Console.IsInputRedirected)
                {
                    using (Stream s = Console.OpenStandardInput())
                    {
                        using (StreamReader reader = new StreamReader(s))
                        {
                            Clipboard.SetText(reader.ReadToEnd());
                        }
                    }
                }
                else
                {
                    _Show(null, true);
                }
            }
            else
            {
                string command = args[0];
                string[] parameters = args.Skip(1).ToArray();

                if(command == "force")
                {
                    force = true;
                    if (args.Length > 1)
                    {
                        command = args[1];
                        parameters = args.Skip(2).ToArray();
                    }
                    else
                    {
                        Die($"May the force be with you!", 255);
                    }

                }

                if (command == "string" || command == "s")
                {
                    _String(parameters);
                }
                else if (command == "path" || command == "P")
                {
                    _Path(parameters);
                }
                else if (command == "copy" || command == "cp")
                {
                    _Copy(parameters);
                }
                else if (command == "text" || command == "t")
                {
                    _Text(parameters);
                }
                else if (command == "html")
                {
                    _Text(parameters, TextDataFormat.Html);
                }
                else if (command == "rtf")
                {
                    _Text(parameters, TextDataFormat.Rtf);
                }
                else if (command == "csv")
                {
                    _Text(parameters, TextDataFormat.CommaSeparatedValue);
                }
                else if (command == "unicode")
                {
                    _Text(parameters, TextDataFormat.UnicodeText);
                }
                else if (command == "image" || command == "i")
                {
                    _Image(parameters);
                }
                else if (command == "save" || command == "S")
                {
                    _Save(parameters);
                }
                else if (command == "load" || command == "L")
                {
                    _Load(parameters);
                }
                else if (command == "paste" || command == "p")
                {
                    _Paste(parameters, false);
                }
                else if (command == "move" || command == "mv")
                {
                    _Paste(parameters, true);
                }
                else if (command == "remove" || command == "rm" || command == "delete" || command == "del")
                {
                    _Delete(parameters);
                }
                else if (command == "show")
                {
                    _Show(parameters, false);
                }
                else if (command == "raw")
                {
                    _Raw(parameters);
                }
                else if (command == "help")
                {
                    // TODO print help!
                }
                else
                {
                    WriteError($"{command}? Wat?", 255);
                    //_Load(new string[] { command });  // load file per default
                }
            }
            return _ErrorLevel;
        }

        #region Functions

        static void _Load(string[] p)
        {
            string path = null;
            string ext = null;

            if (p.Length == 1)
            {
                path = Path.GetFullPath(p[0]);
                string dotExt = Path.GetExtension(path);
                if (dotExt != string.Empty)
                    ext = dotExt.Substring(1);

                switch (ext)
                {
                    case "html":
                    case "htm":
                        _Text(new string[] { path }, TextDataFormat.Html);
                        break;
                    case "rtf":
                        _Text(new string[] { path }, TextDataFormat.Rtf);
                        break;
                    case "csv":
                        _Text(new string[] { path }, TextDataFormat.CommaSeparatedValue);
                        break;
                    case "png":
                    case "jpg":
                    case "jpeg":
                    case "gif":
                    case "bmp":
                    case "tif":
                    case "tiff":
                        _Image(new string[] { path });
                        break;
                    default:
                        _Text(new string[] { path });
                        break;

                }
            }
        }

        static void _Save(string[] p)
        {
            string path = null;
            string ext = null;

            bool overwrite = false;


            if (p.Length == 1)
            {
                path = Path.GetFullPath(p[0]);
                string dotExt = Path.GetExtension(path);
                if (dotExt != string.Empty)
                    ext = dotExt.Substring(1);

            }
            else if(p.Length == 2)
            {
                path = Path.GetFullPath(p[1]);

                if ((p[0] == "force" || p[0] == "f"))
                {
                    string dotExt = Path.GetExtension(path);
                    if (dotExt != string.Empty)
                        ext = dotExt.Substring(1);

                    overwrite = true;
                }
                else
                    ext = p[0];

            }
            else if (p.Length == 3)
            {
                path = Path.GetFullPath(p[2]);
                ext = p[1];

                if ((p[0] == "force" || p[0] == "f"))
                    overwrite = true;
            }
            else
            {
                WriteError("Wat?", 255);
                return;
            }

            string data = null;

            if(File.Exists(path) && !overwrite)
            {
                WriteError("File already exists. Use the force!");
                return;
            }

            if (ClipboardHelper.ContainsText())
            {
                switch (ext)
                {
                    case "html":
                    case "htm":
                        data = Clipboard.GetText(TextDataFormat.Html);
                        File.WriteAllText(path, ExtractBetweenTwoStrings(data, "<!--StartFragment-->", "<!--EndFragment-->", false, false),Encoding.Unicode);
                        WriteLine($"Saved HTML to \"{path}\"");
                        break;
                    case "rtf":
                        data = Clipboard.GetText(TextDataFormat.UnicodeText);
                        File.WriteAllText(path, data, Encoding.Unicode);
                        WriteLine($"Saved {data.Length} bytes to \"{path}\"");
                        break;
                    case "csv":
                        data = Clipboard.GetText(TextDataFormat.CommaSeparatedValue);
                        File.WriteAllText(path, data, Encoding.Unicode);
                        WriteLine($"Saved {data.Length} bytes to \"{path}\"");
                        break;
                    default:
                        data = Clipboard.GetText(TextDataFormat.UnicodeText);
                        File.WriteAllText(path, data, Encoding.Unicode);
                        WriteLine($"Saved {data.Length} bytes to \"{path}\"");
                        break;
                }

            }
            else if (ClipboardHelper.ContainsImage())
            {
                Image i = Clipboard.GetImage();
                switch (ext)
                {
                    case "png":
                        i.Save(path, ImageFormat.Png);
                        WriteLine($"Saved PNG to \"{path}\"");
                        break;
                    case "jpg":
                    case "jpeg":
                        i.Save(path, ImageFormat.Jpeg);
                        WriteLine($"Saved JPEG to \"{path}\"");
                        break;
                    case "gif":
                        i.Save(path, ImageFormat.Gif);
                        WriteLine($"Saved GIF to \"{path}\"");
                        break;
                    case "bmp":
                        i.Save(path, ImageFormat.Bmp);
                        WriteLine($"Saved Bitmap to \"{path}\"");
                        break;
                    case "tif":
                    case "tiff":
                        i.Save(path, ImageFormat.Tiff);
                        WriteLine($"Saved TIFF to \"{path}\"");
                        break;
                    default:
                        i.Save(path, ImageFormat.Png);
                        WriteLine($"Saved PNG to \"{path}\"");
                        break;
                }
            }
            else
            {
                    Console.WriteLine("No data!");
            }
        }

        static void _Image(string[] p)
        {
            string path = Path.GetFullPath(p[0]);
            if (File.Exists(path))
            {
                Image i = new Bitmap(path);
                Clipboard.SetImage(i);
            }
            else
            {
                WriteError($"File \"{path}\" not found!");
            }
        }

        static void _Text(string[] p, TextDataFormat format = TextDataFormat.Text)
        {

            if(p == null || p.Length == 0)
            {
                if(format == TextDataFormat.Rtf && ClipboardHelper.ContainsText(TextDataFormat.Rtf))
                {
                    string content = Clipboard.GetText(format);
                    Write(content);
                }
                else if (format == TextDataFormat.Html && ClipboardHelper.ContainsText(TextDataFormat.Html))
                {
                    string content = Clipboard.GetText(format);
                    content = ExtractBetweenTwoStrings(content, "<!--StartFragment-->", "<!--EndFragment-->", false, false);
                    Write(content);
                }
                else
                {
                    string content = Clipboard.GetText();
                    Write(content);
                }
            }
            else if (p.Length == 1)
            {
                string path = Path.GetFullPath(p[0]);
                if (File.Exists(path))
                {
                    string fileStr = File.ReadAllText(path);
                    if (format == TextDataFormat.Html)
                    {
                        // TODO, copy plaintext from html
                        HTMLHelper.CopyToClipboard(fileStr, fileStr);
                    }
                    else
                    {
                        Clipboard.SetText(fileStr, format);
                    }
                }
                else
                {
                    WriteError($"File \"{path}\" not found!");
                }
            }
        }

        static void _Copy(string[] p)
        {
            // copy files to clipyboard

            try
            {
                StringCollection files = FileAndDirectoryFilter.Get(p, FileAndDirectoryFilter.FileAndDirectoryMode.Directories | FileAndDirectoryFilter.FileAndDirectoryMode.Files);
                
                if (files.Count > 0)
                {
                    
                    Clipboard.SetFileDropList(files);
                    WriteLine($"{files.Count} entries copied.");
                   
                }
                else
                {
                    foreach(string file in p)
                        WriteError($"\"{file}\" not found!");
                }
            
            }
            catch(Exception ex)
            {
                WriteError($"Error {ex.Message}");
            }

        }

        static void _Delete(string[] p)
        {
            try
            {
                StringCollection files = FileAndDirectoryFilter.Get(p, FileAndDirectoryFilter.FileAndDirectoryMode.Directories | FileAndDirectoryFilter.FileAndDirectoryMode.Files);
                foreach (var file in files)
                    if (force)
                        FileOperationAPIWrapper.DeleteCompletelySilent(file);
                    else
                        FileOperationAPIWrapper.Recylce(file);
            }
            catch (Exception ex)
            {
                WriteError($"Error {ex.Message}");
            }

        }


        static void _Paste(string[] p, bool move)
        {
            if (ClipboardHelper.ContainsFileDropList())
            {
                foreach (string source in Clipboard.GetFileDropList())
                {
                    var destination = Path.Combine(Environment.CurrentDirectory, Path.GetFileName(source));
                    if (p.Length > 0)
                    {
                        destination = p[0];

                        if (!Directory.Exists(p[0]) && force)
                            Directory.CreateDirectory(p[0]);
                    }
 
                    WriteLine($"\"{source}\" -> \"{destination}\"");

                    if (source == destination)
                    {
                        WriteError("Same files? Fuckup?");
                        return;
                    }
                    if (!File.Exists(source))
                    {
                        WriteError("Source is gone :(");
                        return;
                    }
                    if (!Directory.Exists(destination))
                    {
                        WriteError("Destination not found.");
                        return;
                    }
                    try
                    {
                        

                        File.Copy(source, destination, force);
                        if (move)
                            File.Delete(source);
                    }
                    catch(IOException iox)
                    {
                        WriteError("File already exists. Use the force!");
                        //WriteError(iox.Message);
                    }
                    catch (Exception ex)
                    {
                        WriteError(ex.Message);
                    }
                }
            }
            else
            {
                WriteError("No pastable content!");
            }
        }

        static void _Raw(string[] p)
        {
            WriteError("???", 255);
        }
        static void _String(string[] p)
        {
            string str = p[0];
            Clipboard.SetText(str);
        }

        static void _Path(string[] p)
        {
            if(p.Length == 0)
                Clipboard.SetText(Environment.CurrentDirectory);
            else
            {
                string path = Path.GetFullPath(p[0]);
                Clipboard.SetText(path);
            }
        }

        static void _Show(string[] p, bool headless)
        {
            if (ClipboardHelper.ContainsText(TextDataFormat.Rtf))
            {
                if (!headless) WriteHeader("RTF:");
                _Text(null);
            }
            else if (ClipboardHelper.ContainsText(TextDataFormat.Html))
            {
                if (!headless) WriteHeader("HTML:");
                _Text(null);
            }
            else if (ClipboardHelper.ContainsText())
            {
                if (!headless) WriteHeader("Text:");
                Write(Clipboard.GetText(TextDataFormat.UnicodeText));
            }
            else if (ClipboardHelper.ContainsImage())
            {
                Image i = Clipboard.GetImage();

                WriteHeader($"Image ({i.Width}x{i.Height} {i.HorizontalResolution}dpi)");

                //Image r = ASCIIConverter.ResizeImageKeepAspect(i, Console.WindowWidth, 1000);
                //Console.WriteLine(ASCIIConverter.GrayscaleImageToASCII(r));
            }
            else if (ClipboardHelper.ContainsFileDropList())
            {
                WriteHeader("Files/Directories:");
                foreach (string source in Clipboard.GetFileDropList())
                {
                    bool isFile = File.Exists(source);
                    bool isDir = Directory.Exists(source);

                    string info = "";

                    if (isFile)
                        info = "f";
                    else if (isDir)
                        info = "d";


                    Write($"{info} ");
                    WriteLine($"{source}", isFile || isDir ? ConsoleColor.Green : ConsoleColor.Red);
                }
            }
            else
            {
                WriteHeader("Unknown clipboard content!");
            }
        }

        #endregion

        #region Helper
        // https://stackoverflow.com/a/68299877
        public static string ExtractBetweenTwoStrings(string FullText, string StartString, string EndString, bool IncludeStartString, bool IncludeEndString)
        {
            try
            {
                int Pos1 = FullText.IndexOf(StartString) + StartString.Length; int Pos2 = FullText.IndexOf(EndString, Pos1); return ((IncludeStartString) ? StartString : "")
                  + FullText.Substring(Pos1, Pos2 - Pos1) + ((IncludeEndString) ? EndString : "");
            }
            catch (Exception ex) { return ""; }
        }

        static void WriteDebug(string msg)
        {
            Console.WriteLine($"DEBUG:{msg}", ConsoleColor.Magenta);
        }

        static void Write(string Message)
        {
            if (silent) return;
            Console.ResetColor();
            Console.WriteLine(Message);
        }
        static void Write(string Message, ConsoleColor color)
        {
            if (silent) return;
            Console.ForegroundColor = color;
            Console.WriteLine(Message);
            Console.ResetColor();
        }

        static void WriteLine(string Message)
        {
            Write(Message + Environment.NewLine);
        }

        static void WriteLine(string Message, ConsoleColor color)
        {
            Write(Message + Environment.NewLine, color);
        }

        static void WriteError(string Message, int ErrorLevel = 1)
        {
            WriteLine(Message, ConsoleColor.Red);
            _ErrorLevel = ErrorLevel;
        }

        static void Die(string Message, int ErrorLevel)
        {
            WriteError(Message, ErrorLevel);
            Environment.Exit(ErrorLevel);
        }
        static void WriteHeader(string Message)
        {
            if (!Console.IsOutputRedirected)
                WriteLine(Message, ConsoleColor.Cyan);
            
        }

        static byte[] GetBytesFromClipboardRaw()
        {
            DataObject retrievedData = Clipboard.GetDataObject() as DataObject;
            if (retrievedData == null || !retrievedData.GetDataPresent("rawbinary", false))
                return null;
            MemoryStream byteStream = retrievedData.GetData("rawbinary", false) as MemoryStream;
            if (byteStream == null)
                return null;
            return byteStream.ToArray();
        }

        #endregion
    }
}
