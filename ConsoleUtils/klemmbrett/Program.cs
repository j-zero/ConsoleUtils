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
                    _Show(null, false);
                }
            }
            else
            {
                string command = args[0];
                string[] parameters = args.Skip(1).ToArray();

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
                    _Paste(parameters);
                }
                else if (command == "show")
                {
                    _Show(parameters, false);
                }
                else if (command == "raw")
                {
                    _Raw(parameters);
                }
                else
                {
                    WriteError($"{command}? Wat?", 255);
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
                ext = p[0];
            }
            else
            {
                WriteError("Wat?", 255);
                return;
            }

            string data = null;

            if (ClipboardHelper.ContainsText())
            {
                switch (ext)
                {
                    case "html":
                    case "htm":
                        data = Clipboard.GetText(TextDataFormat.Html);
                        File.WriteAllText(path, ExtractBetweenTwoStrings(data, "<!--StartFragment-->", "<!--EndFragment-->", false, false),Encoding.Unicode);
                        Console.WriteLine($"Saved HTML to \"{path}\"");
                        break;
                    case "rtf":
                        data = Clipboard.GetText(TextDataFormat.UnicodeText);
                        File.WriteAllText(path, data, Encoding.Unicode);
                        Console.WriteLine($"Saved {data.Length} bytes to \"{path}\"");
                        break;
                    case "csv":
                        data = Clipboard.GetText(TextDataFormat.CommaSeparatedValue);
                        File.WriteAllText(path, data, Encoding.Unicode);
                        Console.WriteLine($"Saved {data.Length} bytes to \"{path}\"");
                        break;
                    default:
                        data = Clipboard.GetText(TextDataFormat.UnicodeText);
                        File.WriteAllText(path, data, Encoding.Unicode);
                        Console.WriteLine($"Saved {data.Length} bytes to \"{path}\"");
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
                        Console.WriteLine($"Saved PNG to \"{path}\"");
                        break;
                    case "jpg":
                    case "jpeg":
                        i.Save(path, ImageFormat.Jpeg);
                        Console.WriteLine($"Saved JPEG to \"{path}\"");
                        break;
                    case "gif":
                        i.Save(path, ImageFormat.Gif);
                        Console.WriteLine($"Saved GIF to \"{path}\"");
                        break;
                    case "bmp":
                        i.Save(path, ImageFormat.Bmp);
                        Console.WriteLine($"Saved Bitmap to \"{path}\"");
                        break;
                    case "tif":
                    case "tiff":
                        i.Save(path, ImageFormat.Tiff);
                        Console.WriteLine($"Saved TIFF to \"{path}\"");
                        break;
                    default:
                        i.Save(path, ImageFormat.Png);
                        Console.WriteLine($"Saved PNG to \"{path}\"");
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
                    Console.Write(content);
                }
                else if (format == TextDataFormat.Html && ClipboardHelper.ContainsText(TextDataFormat.Html))
                {
                    string content = Clipboard.GetText(format);
                    content = ExtractBetweenTwoStrings(content, "<!--StartFragment-->", "<!--EndFragment-->", false, false);
                    Console.Write(content);
                }
                else
                {
                    string content = Clipboard.GetText();
                    Console.Write(content);
                }
            }
            else if (p.Length == 1)
            {
                string path = Path.GetFullPath(p[0]);
                if (File.Exists(path))
                {
                    string fileStr = File.ReadAllText(path);
                    Clipboard.SetText(fileStr, format);
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
            string path = Path.GetFullPath(p[0]);
            if (File.Exists(path))
            {
                Clipboard.SetFileDropList(new StringCollection() { path });
            }
            else if (Directory.Exists(path))
            {
                Clipboard.SetFileDropList(new StringCollection() { path });
            }
            else
            {
                WriteError($"File \"{path}\" not found!");
            }
        }
        static void _Paste(string[] p)
        {
            bool overwrite = false;
            if (p.Length > 0 && (p[0] == "force" || p[0] == "f"))
                overwrite = true;
            if (ClipboardHelper.ContainsFileDropList())
            {
                foreach (string source in Clipboard.GetFileDropList())
                {
                    var destination = Path.Combine(Environment.CurrentDirectory, Path.GetFileName(source));
                    if(source == destination)
                    {
                        WriteError("Same files? Fuckup?");
                        return;
                    }
                    try
                    {
                        Console.WriteLine($"\"{source}\" -> \"{destination}\"");
                        File.Copy(source, destination, overwrite);
                    }
                    catch(IOException iox)
                    {
                        WriteError("File already exists. Use the force!");
                        WriteError(iox.Message);
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
                _Text(null, TextDataFormat.Rtf);
            }
            else if (ClipboardHelper.ContainsText(TextDataFormat.Html))
            {
                if (!headless) WriteHeader("HTML:");
                _Text(null, TextDataFormat.Html);
            }
            else if (ClipboardHelper.ContainsText())
            {
                if (!headless) WriteHeader("Text:");
                Console.Write(Clipboard.GetText(TextDataFormat.UnicodeText));
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
                WriteHeader("Files:");
                foreach (string source in Clipboard.GetFileDropList())
                {
                    Console.WriteLine(source);
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

        static void WriteError(string Message, int ErrorLevel = 1)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(Message);
            Console.ResetColor();
            _ErrorLevel = ErrorLevel;
        }

        static void WriteHeader(string Text)
        {
            if (!Console.IsOutputRedirected)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(Text);
                Console.ResetColor();
            }
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
