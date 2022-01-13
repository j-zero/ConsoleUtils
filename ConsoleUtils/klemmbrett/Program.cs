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
                    _Show(null, true);
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
                else if (command == "copy" || command == "c")
                {
                    _Copy(parameters);
                }
                else if (command == "text" || command == "t")
                {
                    _Text(parameters);
                }
                else if (command == "image" || command == "i")
                {
                    _Image(parameters);
                }
                else if (command == "save" || command == "S")
                {
                    _Save(parameters);
                }
                else if (command == "paste" || command == "p")
                {
                    _Paste(parameters);
                }
                else if (command == "show")
                {
                    _Show(parameters, false);
                }
                else if (command == "show")
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

        static void _Save(string[] p)
        {
            string path = Path.GetFullPath(p[0]);

            if (ClipboardHelper.ContainsText())
            {
                string data = Clipboard.GetText();
                File.WriteAllText(path, data);
                Console.WriteLine($"Saved {data.Length} bytes to \"{path}\"");
            }
            else if (ClipboardHelper.ContainsImage())
            {
                string ext = Path.GetExtension(path);
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
                byte[] data = GetBytesFromClipboardRaw();
                if (data != null)
                {
                    File.WriteAllBytes(path, data);
                    Console.WriteLine($"Saved {data.Length} bytes to \"{path}\"");
                }
                else
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

        static void _Text(string[] p)
        {
            string path = Path.GetFullPath(p[0]);
            if (File.Exists(path))
            {
                string fileStr = File.ReadAllText(path);
                Clipboard.SetText(fileStr);
            }
            else
            {
                WriteError($"File \"{path}\" not found!");
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
            if (ClipboardHelper.ContainsFileDropList())
            {
                foreach (string source in Clipboard.GetFileDropList())
                {
                    var destination = Path.Combine(Environment.CurrentDirectory, Path.GetFileName(source));
                    try
                    {
                        // TODO: Ask for overrite
                        Console.WriteLine($"Copying \"{source}\" to \"{destination}\"");
                        File.Copy(source, destination, true);
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
            if (ClipboardHelper.ContainsText())
            {
                if(!headless) WriteHeader("String:");
                Console.Write(Clipboard.GetText());
            }
            else if (ClipboardHelper.ContainsImage())
            {
                Image i = Clipboard.GetImage();

                WriteHeader($"Image ({i.Width}x{i.Height}@{i.HorizontalResolution}dpi)");

                //Image r = ASCIIConverter.ResizeImageKeepAspect(i, Console.WindowWidth, 1000);
                //Console.WriteLine(ASCIIConverter.GrayscaleImageToASCII(r));
            }
            else if (ClipboardHelper.ContainsFileDropList())
            {
                if (!headless) WriteHeader("Files:");
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


        // Helper

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

    }
}
