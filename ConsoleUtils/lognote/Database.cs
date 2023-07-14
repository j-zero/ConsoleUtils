using System;
using System.Drawing;
using System.IO;
using System.Linq;
using Pastel;

namespace lognote
{
    public class DB
    {
        private bool debug = false;
        public string folder = PathHelper.GetSpecialFolder(Environment.SpecialFolder.MyDocuments, "LogNote");
        
        string fileExtension = ".log";
        //string thema = "default";
        //bool exit = false;
        public string dateTimeFormat = "yyyy'-'MM'-'dd' 'HH':'mm':'ss";
        string fileDateTimeFormat = "yyyy''MM''dd''HH''mm''ss";
        string linePrefix = " ";

        public string thema { get; set; }

        string GetDateTime(DateTime? dt = null)
        {
            if (dt == null)
                return DateTime.Now.ToString(dateTimeFormat).Pastel(ColorTheme.OffsetColor);
            else
                return ((DateTime)dt).ToString(dateTimeFormat).Pastel(ColorTheme.OffsetColor);
        }
        public void SaveScreenshot(string thema)
        {
            string imgeFileExtension = ".png";
            string dateTime = DateTime.Now.ToString(fileDateTimeFormat);
            if (System.Windows.Forms.Clipboard.ContainsImage())
            {
                Image screenshot = System.Windows.Forms.Clipboard.GetImage();
                if (screenshot != null)
                {
                    string themaFolder = CreateThemeFolder(thema);


                    string filename = PathHelper.CleanFileNameFromString(thema + "-" + PathHelper.CleanFileNameFromString(dateTime) + imgeFileExtension);
                    string fullFileName = Path.Combine(themaFolder, filename);

                    int i = 0;
                    while (File.Exists(fullFileName))
                    {
                        filename = PathHelper.CleanFileNameFromString(thema + "-" + PathHelper.CleanFileNameFromString(dateTime) + "-" + (i++).ToString() + imgeFileExtension);
                        fullFileName = Path.Combine(themaFolder, filename);
                    }

                    CreateThemeFolder(thema);
                    string msg = $"Image from clipboard saved to: {filename}";
                    screenshot.Save(fullFileName, System.Drawing.Imaging.ImageFormat.Png);
                    
                    SaveData(thema, msg);
                    PrintMessage(msg);

                    if (debug)
                        Console.WriteLine($"DEBUG: {fullFileName.Pastel(ColorTheme.OffsetColorHighlight)}");



                }
            }
            else
            {
                PrintMessage("No iamge in clipboard.", true);
            }

        }



        public void SaveData(string thema, string text)
        {
            string msg = linePrefix + text.Replace("\r\n", "\n").Replace("\n", linePrefix + "\n");

            string safeFileName = PathHelper.CleanFileNameFromString(thema + fileExtension);

            string dateTime = DateTime.Now.ToString(dateTimeFormat);

            string themaFolder = CreateThemeFolder(thema);
            string finalFileName = Path.Combine(themaFolder, safeFileName);

            CreateThemeFolder(thema);

            File.AppendAllText(finalFileName, $"{dateTime}\n{msg}\n"); // todo error handling

            if (debug)
                Console.WriteLine($"DEBUG: {finalFileName.Pastel(ColorTheme.OffsetColorHighlight)}:\n{dateTime.Pastel(ColorTheme.OffsetColor)}\n{msg.Pastel("#ffffff")}");



        }

        public void PrintMessage(string msg, bool error = false)
        {
            if (error)
                Console.WriteLine($"{DateTime.Now.ToString(this.dateTimeFormat).Pastel(ColorTheme.OffsetColor)} {("(!) " + msg).Pastel(ColorTheme.Error1)}");
            else
                Console.WriteLine($"{DateTime.Now.ToString(this.dateTimeFormat).Pastel(ColorTheme.OffsetColor)} {msg}");
        }


        public void PrintData(string thema)
        {
            string filename = Path.Combine(this.folder, thema, PathHelper.CleanFileNameFromString(thema + fileExtension));
            if (File.Exists(filename))
            {
                string[] lines = File.ReadAllLines(filename);
                foreach (string line in lines)
                {
                    if (IsDateTimeLine(line))
                    {
                        Console.WriteLine(line.Pastel(ColorTheme.Default1));
                    }
                    else
                    {
                        if (line.StartsWith(linePrefix))
                        {
                            Console.WriteLine(line.Pastel(ColorTheme.Text));
                        }
                        else
                        {
                            ;
                        }

                    }
                }
            }
            else
            {
                PrintMessage("empty", true);
            }

        }

        public string GetFolder(string thema)
        {
            return Path.Combine(this.folder, thema);
        }

        public string[] GetThema(string thema)
        {
            string filename = Path.Combine(this.folder, thema, PathHelper.CleanFileNameFromString(thema + fileExtension));
            return File.ReadAllLines(filename);
        }

        public string[] GetAllThemas()
        {
            if (Directory.Exists(this.folder))
            {
                string[] dirs = Directory.GetDirectories(this.folder).Select(Path.GetFileName).ToArray();
                return dirs;
            }
            else
            {
                return new string[0];
            }

        }

        public bool IsDateTimeLine(string line)
        {
            DateTime dt;
            bool isValidDateTime = DateTime.TryParse(line, out dt);
            return isValidDateTime && !line.StartsWith(linePrefix);
        }

        string CreateThemeFolder(string thema)
        {
            return Directory.CreateDirectory(Path.Combine(folder, thema)).FullName; // todo error handling
        }
    }
}
