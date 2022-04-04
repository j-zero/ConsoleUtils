using System;
using System.IO;

namespace google
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
                google(string.Join("+", args));
            else
                if (Console.IsInputRedirected)
                using (Stream s = Console.OpenStandardInput())
                using (StreamReader sr = new StreamReader(s))
                    google(sr.ReadToEnd());
            else
                Console.WriteLine("Usage: google [search term]");

        }

        static void google(string query)
        {
            System.Diagnostics.Process.Start($"https://www.google.com/search?q={query}");
        }
    }
}
