using System;
using System.Collections.Generic;
using System.IO;
using Pastel;

namespace ed
{
    internal class Program
    { 
        static void lineChanger(string newText, string fileName, int line_to_edit)
        {
            string[] arrLine = File.ReadAllLines(fileName);
            arrLine[line_to_edit - 1] = newText;
            File.WriteAllLines(fileName, arrLine);
        }

        static void Main(string[] args)
        {
            var file_name = @"C:\TEMP\foobar.txt";
            string[] lines = File.ReadAllLines(file_name); // ugly


            PrintLines(lines, 0, Console.WindowHeight - 2);
            ParseKey();
            ;


        }

        static void ParseKey()
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


        }

        static void PrintLines(string[] lines, int from, int to) 
        {
            Console.Clear();
            int line_number_max_length = lines.Length.ToString().Length; // even more ugly
            string prefix = "".PadLeft(line_number_max_length + 1) + "│ ".Pastel(ColorTheme.DarkText);

            for (int i = from; i < to; i++)
            {
                var arr = GetSplittedText(lines[i], line_number_max_length + 2, 1);

                for (int j = 0; j < arr.Length; j++)
                {
                    string line_number = i.ToString().PadLeft(line_number_max_length + 1, ' ').Pastel(ColorTheme.Default1);
                    string blank = "".ToString().PadLeft(line_number_max_length + 1, ' ');
                    Console.WriteLine((j == 0 ? line_number : blank) + "│ ".Pastel(ColorTheme.DarkText) + arr[j].Pastel(ColorTheme.Text));
                }

            }
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
