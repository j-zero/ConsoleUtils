using Pastel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace download
{
    internal class Program
    {
        static CmdParser cmd;
        static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        static bool _result = false;
        static bool _debug = false;
        static string filename = "";

        static void Main(string[] args)
        {

            cmd = new CmdParser(args)
            {
                { "help", "", CmdCommandTypes.FLAG, "Show this help." },
                { "url", "u", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Download URL" },
                { "outfile", "O", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Output file" },
                { "timeout", "O", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.INT, 2000 }
                    }, "Output file" },
            };

            cmd.DefaultParameter = "url";
            cmd.Parse();

            string url = cmd["url"].String;
            if (cmd.HasFlag("help") || url == null)
                ShowHelp();

            filename = cmd["outfile"].String;

            if (url != null)
            {
                Uri uri = new Uri(url);
                
                filename = GetFilenameFromWebServer(url); // get filename by HEAD
                if (filename == String.Empty)
                    filename = GetFileNameFromUrl(url);     // extract filename from url

                if (filename == String.Empty)
                    filename = "index.html";        // set filename to "index.html"

                if (filename != string.Empty)
                    StartDownloadFile(uri, filename, (int)cmd["timeout"].Int);
                else
                    ConsoleHelper.WriteError("No filename given!"); // this should never happen!
            }
            else
            {
                // this should never happen!
            }
        }
        static void ShowHelp()
        {
            Console.WriteLine($"download, {ConsoleHelper.GetVersionString()}");
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [Options] {{[--file|-f] file}}");
            Console.WriteLine($"Options:");
            foreach (CmdOption c in cmd.OrderBy(x => x.Name))
            {
                string l = $"  --{c.Name}".Pastel("9CDCFE") + (!string.IsNullOrEmpty(c.ShortName) ? $", {("-" + c.ShortName).Pastel("9CDCFE")}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel("569CD6")).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
            //WriteError("Usage: subnet [ip/cidr|ip/mask|ip number_of_hosts]");
            Exit(0);
        }


        static void Exit(int exitCode)
        {
            string parrentProcess = ConsoleUtilsCore.ParentProcessUtilities.GetParentProcess().ProcessName;
            //Console.WriteLine(parrentProcess);

            if (System.Diagnostics.Debugger.IsAttached || parrentProcess.ToLower().Contains("explorer")) // is debugger attached or started by double-click/file-drag
            {
                Console.WriteLine("\nPress any key to exit.");
                Console.ReadKey();
            }

            Environment.Exit(exitCode);
        }

        public static bool StartDownloadFile(Uri uri, string path, int timeout)
        {
           
            string _fullPathWhereToSave = Path.GetFullPath(path);
            try
            {
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(_fullPathWhereToSave));

                if (File.Exists(_fullPathWhereToSave))
                {
                    File.Delete(_fullPathWhereToSave);
                }
                using (WebClient client = new WebClient())
                {
                    // client.Credentials = new NetworkCredential("username", "password");
                    client.DownloadProgressChanged += WebClientDownloadProgressChanged;
                    client.DownloadFileCompleted += WebClientDownloadCompleted;
                   
                    client.DownloadFileAsync(uri, _fullPathWhereToSave);
                    _semaphore.Wait(timeout);
                    return _result && File.Exists(_fullPathWhereToSave);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Was not able to download file!");
                Console.Write(e);
                return false;
            }
            finally
            {
                _semaphore.Dispose();
            }
        }

        private static void WebClientDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Console.Write($"\r{filename} -> {e.ProgressPercentage}%");
        }

        private static void WebClientDownloadCompleted(object sender, AsyncCompletedEventArgs args)
        {
            _result = !args.Cancelled;
            if (!_result)
            {
                Console.Write(args.Error.ToString());
            }
            Console.WriteLine(Environment.NewLine + "Download finished!");
            _semaphore.Release();
        }

        static string GetFileNameFromUrl(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                uri = new Uri(url);

            return Path.GetFileName(uri.LocalPath);
        }

        public static string GetFilenameFromWebServer(string url)
        {
            string result = String.Empty;

            var req = System.Net.WebRequest.Create(url);
            req.Method = "HEAD";
            try
            {
                using (System.Net.WebResponse resp = req.GetResponse())
                {
                    // Try to extract the filename from the Content-Disposition header
                    if (!string.IsNullOrEmpty(resp.Headers["Content-Disposition"]))
                    {
                        result = resp.Headers["Content-Disposition"].Substring(resp.Headers["Content-Disposition"].IndexOf("filename=") + 9).Replace("\"", "");
                    }
                }
            }
            catch (Exception ex)
            {
                if (_debug)
                    ConsoleHelper.WriteError(ex);
            }

            return result;
        }
    }
}
