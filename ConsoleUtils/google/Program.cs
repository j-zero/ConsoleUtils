using System;
using System.IO;

namespace google
{
    internal class Program
    {
    // raw command line without the program name (.NET: GetCommandLineArgs()[0] is the dll, not the exe)
    static string GetRawArguments()
    {
        string cl = Environment.CommandLine;
        string rest;
        if (cl.StartsWith("\""))
        {
            int end = cl.IndexOf('"', 1);
            rest = end >= 0 ? cl.Substring(end + 1) : "";
        }
        else
        {
            int sp = cl.IndexOf(' ');
            rest = sp >= 0 ? cl.Substring(sp + 1) : "";
        }
        return rest.TrimStart();
    }

        static void Main(string[] args)
        {
            var f = GetRawArguments();
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
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo($"https://www.google.com/search?q={query}") { UseShellExecute = true });
        }
    }
}
