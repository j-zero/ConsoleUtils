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
            /* Read STDIN
            if (Console.IsInputRedirected)
            {
                using (Stream s = Console.OpenStandardInput())
                {
                    using (StreamReader reader = new StreamReader(s))
                    {
                        Console.Write(reader.ReadToEnd());
                    }
                }
            }
            */
            if(args.Length == 0)
            {
                Console.WriteLine("No command given!");
                return 1;
            }
            string command = args[0];

            if (args.Length == 2) // command + parameter
            {
                if (command == "string" || command == "s")
                {
                    string str = args[1];
                    Clipboard.SetText(str);
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
                else if(command == "show")
                {
                    if (ClipboardHelper.ContainsText())
                    {
                        WriteHeader("String:");
                        Console.Write(Clipboard.GetText());
                    }
                    else if (ClipboardHelper.ContainsImage())
                    {
                         WriteHeader("Image");
                        Image i = Clipboard.GetImage();
                        Image r = ASCIIConverter.ResizeImageKeepAspect(i, Console.WindowWidth, 1000);
                        Console.WriteLine(ASCIIConverter.GrayscaleImageToASCII(r));
                    }
                    else if(ClipboardHelper.ContainsFileDropList()){
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


            return 2;
        }

        static void WriteHeader(string Text)
        {
            if (!Console.IsOutputRedirected)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(Text);
                Console.ResetColor();
            }
        }

        public static ImageFormat GetImageFormatFromImage(Image image)
        {
            if (image.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Jpeg))
                return System.Drawing.Imaging.ImageFormat.Jpeg;
            if (image.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Bmp))
                return System.Drawing.Imaging.ImageFormat.Bmp;
            if (image.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Png))
                return System.Drawing.Imaging.ImageFormat.Png;
            if (image.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Gif))
                return System.Drawing.Imaging.ImageFormat.Gif;
            if (image.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Emf))
                return System.Drawing.Imaging.ImageFormat.Emf;
            if (image.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Exif))
                return System.Drawing.Imaging.ImageFormat.Exif;
            if (image.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Icon))
                return System.Drawing.Imaging.ImageFormat.Icon;
            if (image.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.MemoryBmp))
                return System.Drawing.Imaging.ImageFormat.MemoryBmp;
            if (image.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Tiff))
                return System.Drawing.Imaging.ImageFormat.Tiff;
            if (image.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Wmf))
                return System.Drawing.Imaging.ImageFormat.Wmf;

            throw new Exception("Image format not supported");
        }
    }
}
