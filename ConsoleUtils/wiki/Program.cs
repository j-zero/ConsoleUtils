using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace wiki
{
    internal class Program
    {
        static void Main(string[] args)
        {
            
                var f = Environment.CommandLine.Replace("\"" + Environment.GetCommandLineArgs()[0] + "\"", "").TrimStart();
            if (args.Length > 0)
                Console.WriteLine(wiki_query(string.Join("+", args)));
            else if (Console.IsInputRedirected)
                using (Stream s = Console.OpenStandardInput())
                using (StreamReader sr = new StreamReader(s))
                    Console.WriteLine(wiki_query(sr.ReadToEnd()));
            else
                Console.WriteLine("Usage: wiki [search term]");
            
        }

        static string wiki_query(string needle)
        {
            string final_result = "";
            WebClient client = new WebClient();

            using (Stream stream = client.OpenRead("http://de.wikipedia.org/w/api.php?format=json&action=query&prop=extracts&explaintext=1&redirects=1&exintro&titles=" + needle))
            using (StreamReader reader = new StreamReader(stream))
            {
                JsonSerializer ser = new JsonSerializer();
                Result result = ser.Deserialize<Result>(new JsonTextReader(reader));

                foreach (Page page in result.query.pages.Values)
                    final_result += page.extract;
            }
            return final_result;
        }
    }
    public class Result
    {
        public Query query { get; set; }
    }

    public class Query
    {
        public Dictionary<string, Page> pages { get; set; }
    }

    public class Page
    {
        public string extract { get; set; }
    }
}
