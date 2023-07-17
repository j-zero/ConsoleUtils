using System;
using System.Collections.Generic;
using System.IO;
using Pastel;

namespace ed
{
    internal class Program
    {
        static int from = 0;
        static int current_last_line = 0;
        static int backup_from = 0;

        static string[] lines = new string[0];

        static void lineChanger(string newText, string fileName, int line_to_edit)
        {
            string[] arrLine = File.ReadAllLines(fileName);
            arrLine[line_to_edit - 1] = newText;
            File.WriteAllLines(fileName, arrLine);
        }

        static void Main(string[] args)
        {
            Console.CursorVisible = false;

            var file_name = args[0];
            lines = File.ReadAllLines(file_name); // ugly

            PrintLines();
            while (ParseKey())
            {

            }
            ;


        }

        static bool ParseKey()
        {

            ConsoleKeyInfo cki;
            cki = Console.ReadKey(true);
            if (cki.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                Console.Write("CTRL ");
            }

            if (cki.Modifiers.HasFlag(ConsoleModifiers.Alt))
            {
                Console.Write("ALT ");
            }

            if (cki.Modifiers.HasFlag(ConsoleModifiers.Shift))
            {
                Console.Write("SHIFT ");
            }
            if (cki.Key == ConsoleKey.DownArrow)
            {
                backup_from = from;
                from++;
                if (from >= lines.Length - Console.WindowHeight - 1)
                    from = lines.Length - Console.WindowHeight - 1;
                PrintLines();
            }
            if (cki.Key == ConsoleKey.UpArrow)
            {
                backup_from = from;
                from--;
                if (from < 0)
                    from = 0;
                PrintLines();
            }
            if (cki.Key == ConsoleKey.PageDown || cki.Key == ConsoleKey.Spacebar)
            {
                backup_from = from;
                from = current_last_line - 1;
                if (from >= lines.Length - Console.WindowHeight - 1)
                    from = lines.Length - Console.WindowHeight - 1;
                PrintLines();
            }
            if (cki.Key == ConsoleKey.PageUp)
            {

                from -= Console.WindowHeight / 2;
                //backup_from = from;
                if (from < 0)
                    from = 0;
                PrintLines();
            }
            if (cki.Key == ConsoleKey.Spacebar)
            {
                PrintLines();
            }
            if (cki.Key == ConsoleKey.Q)
            {
                return false;
            }
            return true;
        }

        static int PrintLines() 
        {
            //Console.Clear();
            Console.SetCursorPosition(0, 0);
            
            int to = from + Console.WindowHeight-1;

            int line_number_max_length = lines.Length.ToString().Length; // even more ugly
            string prefix = "".PadLeft(line_number_max_length + 1) + "│ ".Pastel(ColorTheme.DarkText);
            int i = from;
            int current_line_number = i;
            int counter_displayed_lines = 0;

            while (i <= (to <= lines.Length ? to : lines.Length))
            {
                var arr = GetSplittedText(lines[i], line_number_max_length + 2, 1);
                
                for (int j = 0; j < arr.Length; j++)
                {

                    counter_displayed_lines++;
                    if (counter_displayed_lines > Console.WindowHeight - 1)
                        break;
                    string line_number = (current_line_number + 1).ToString().PadLeft(line_number_max_length + 1, ' ').Pastel(ColorTheme.Default1);
                    string blank = "".ToString().PadLeft(line_number_max_length + 1, ' ');
                    Console.Write((j == 0 ? (line_number) : blank) + "│ ".Pastel(ColorTheme.DarkText));
                    int pos = Console.CursorLeft;
                    Console.Write(arr[j].PadRight(Console.WindowWidth - pos,' ').Pastel(ColorTheme.Text));
                }
                current_line_number++;
                current_last_line = current_line_number;
                i += arr.Length;
            }

            Console.SetCursorPosition(0, Console.WindowHeight-1);
            Console.Write(":".PadRight(Console.WindowWidth, ' ').PastelBg(ColorTheme.Default1));
            Console.CursorVisible = false;
            return i;
        }

        static string[] GetSplittedText(string input, int padding_left = 4, int padding_right = 1)
        {
            int max_length = Console.WindowWidth - padding_left - padding_right;
            string tmp = input;
            if (input.Length < max_length)
                return new string[] { input };
            else
            {
                List<string> r = new List<string>();
                while(tmp.Length >= max_length)
                {
                    r.Add(tmp.Substring(0, max_length));
                    tmp = tmp.Substring(max_length);
                    
                }
                r.Add(tmp);
                return r.ToArray();
            }
        }

        void ramedit()
        {
            int line_to_edit = 2; // Warning: 1-based indexing!
            string sourceFile = "source.txt";
            string destinationFile = "target.txt";

            // Read the appropriate line from the file.
            string lineToWrite = null;
            using (StreamReader reader = new StreamReader(sourceFile))
            {
                for (int i = 1; i <= line_to_edit; ++i)
                    lineToWrite = reader.ReadLine();
            }

            if (lineToWrite == null)
                throw new InvalidDataException("Line does not exist in " + sourceFile);

            // Read the old file.
            string[] lines = File.ReadAllLines(destinationFile);

            // Write the new file over the old file.
            using (StreamWriter writer = new StreamWriter(destinationFile))
            {
                for (int currentLine = 1; currentLine <= lines.Length; ++currentLine)
                {
                    if (currentLine == line_to_edit)
                    {
                        writer.WriteLine(lineToWrite);
                    }
                    else
                    {
                        writer.WriteLine(lines[currentLine - 1]);
                    }
                }
            }
        }
        void streamEdit()
        {
            int line_to_edit = 2;
            string sourceFile = "source.txt";
            string destinationFile = "target.txt";
            string tempFile = "target2.txt";

            // Read the appropriate line from the file.
            string lineToWrite = null;
            using (StreamReader reader = new StreamReader(sourceFile))
            {
                for (int i = 1; i <= line_to_edit; ++i)
                    lineToWrite = reader.ReadLine();
            }

            if (lineToWrite == null)
                throw new InvalidDataException("Line does not exist in " + sourceFile);

            // Read from the target file and write to a new file.
            int line_number = 1;
            string line = null;
            using (StreamReader reader = new StreamReader(destinationFile))
            using (StreamWriter writer = new StreamWriter(tempFile))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    if (line_number == line_to_edit)
                    {
                        writer.WriteLine(lineToWrite);
                    }
                    else
                    {
                        writer.WriteLine(line);
                    }
                    line_number++;
                }
            }

            // TODO: Delete the old file and replace it with the new file here.
        }
    }
}
