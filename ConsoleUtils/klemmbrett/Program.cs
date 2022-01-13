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
                    return 0;
                }
                else
                {
                    Console.WriteLine("No command given!");
                    return 1;
                }
            }
            else
            {
                string command = args[0];

                if (args.Length == 2) // command + parameter
                {
                    if (command == "string" || command == "s")
                    {
                        string str = args[1];
                        Clipboard.SetText(str);
                    }
                    if (command == "path" || command == "P")
                    {
                        string path = Path.GetFullPath(args[1]);
                        Clipboard.SetText(path);
                    }
                    else if (command == "copy" || command == "c")
                    {
                        // copy files to clipyboard
                        string path = Path.GetFullPath(args[1]);
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
                            Console.WriteLine($"File \"{path}\" not found!");
                            return 1;
                        }

                    }
                    else if (command == "text" || command == "t")
                    {
                        string path = Path.GetFullPath(args[1]);
                        if (File.Exists(path))
                        {
                            string fileStr = File.ReadAllText(path);
                            Clipboard.SetText(fileStr);
                        }
                    }
                    else if (command == "image" || command == "i")
                    {
                        string path = Path.GetFullPath(args[1]);
                        if (File.Exists(path))
                        {
                            Image i = new Bitmap(path);
                            Clipboard.SetImage(i);
                        }
                    }
                    else if (command == "save" || command == "S")
                    {
                        string path = Path.GetFullPath(args[1]);


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
                }
                else if (args.Length == 1) // command only
                {
                    if (command == "paste" || command == "p")
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
                                    Console.WriteLine(ex.Message);
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("No pastable content!");
                            return 1;
                        }
                    }
                    if (command == "path" || command == "P")
                    {
                        Clipboard.SetText(Environment.CurrentDirectory);
                    }
                    else if (command == "show" || command == "S")
                    {
                        if (ClipboardHelper.ContainsText())
                        {
                            WriteHeader("String:");
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
                }
                else
                {
                    Console.WriteLine("Wat?");
                    return 255;
                }
            }
            return 2;
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
