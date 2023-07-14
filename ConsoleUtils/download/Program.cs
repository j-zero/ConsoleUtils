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

    // TODO Output to STDOUT instead File Download: client.DownloadDataAsync

    internal class Program
    {
        static CmdParser cmd;
        static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        static bool _result = false;
        static bool _debug = false;
        static string filename = "";

        //static long lastUpdate;
        //static long lastBytes = 0;
        static DateTime _startedAt;

        static void Main(string[] args)
        {

            cmd = new CmdParser(args)
            {
                { "help", "", CmdCommandTypes.FLAG, "Show this help." },
                { "headers", "", CmdCommandTypes.FLAG, "Show headers" },
                { "url", "u", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Download URL" },
                { "outfile", "O", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Output file" },
                { "timeout", "t", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.INT, 0 }
                    }, "Timeout" },
                { "no-proxy", "P", CmdCommandTypes.FLAG, new CmdParameters() {
                        { CmdParameterTypes.BOOL, false }
                    }, "Disable proxy" },
                { "proxy", "p", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Overwrite Proxy" },
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
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [Options] {{[--url|-u] URL}}");
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
                    

                    WebProxy proxy = new WebProxy();

                    string cmdProxy = cmd["proxy"].Strings[0];

                    if (cmdProxy != null)
                    {
                        proxy.Address = new Uri(cmdProxy);
                        //proxy.Credentials = new NetworkCredential("usernameHere", "pa****rdHere");  //These can be replaced by user input
                        proxy.UseDefaultCredentials = true;
                        proxy.BypassProxyOnLocal = false;  //still use the proxy for local addresses
                        client.Proxy = proxy;
                    }

                    if (cmd.HasFlag("no-proxy"))
                        client.Proxy = null;

                    

                    if(client.Proxy != null)
                        Console.WriteLine($"using proxy \"{client.Proxy.GetProxy(uri)}\"");


                    client.Credentials = CredentialCache.DefaultCredentials;
                    //client.Credentials = new NetworkCredential("username", "password");
                    client.DownloadProgressChanged += WebClientDownloadProgressChanged;
                    client.DownloadFileCompleted += WebClientDownloadCompleted;
                   
                   

                    client.DownloadFileAsync(uri, _fullPathWhereToSave);
                    if (timeout == 0)
                        _semaphore.Wait();
                    else
                    {
                        _semaphore.Wait(timeout);
                    }
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
            long bytesPerSecond = 0;
            if (_startedAt == default(DateTime))
            {
                _startedAt = DateTime.Now;
            }
            else
            {
                var timeSpan = DateTime.Now - _startedAt;
                if (timeSpan.TotalSeconds > 0)
                {
                    bytesPerSecond = (long)(e.BytesReceived / timeSpan.TotalSeconds);
                }
            }

            string readableBps = UnitHelper.CalculateHumanReadableSize((ulong)bytesPerSecond);
            string readableBytes = UnitHelper.CalculateHumanReadableSize((ulong)e.BytesReceived);
            string readableBytesTotal = UnitHelper.CalculateHumanReadableSize((ulong)e.TotalBytesToReceive,1024,1,true);

            Console.Write($"\r{filename} -> {readableBytes}/{readableBytesTotal} ({e.ProgressPercentage}%, {readableBps}/s)".PadRight(Console.BufferWidth));
        }

        private static void WebClientDownloadCompleted(object sender, AsyncCompletedEventArgs args)
        {


            if (args.Cancelled || args.Error != null)
            {

                dynamic re = args.Error as dynamic;
                try
                {
                    HttpWebResponse response = re.Response;

                    if (response == null)
                    {
                        if (re.GetType() == typeof(WebException))
                        {
                            ConsoleHelper.WriteError(re.Message);
                            Console.Write(re.InnerException.Message);
                        }
                        else
                        {
                            ConsoleHelper.WriteErrorDog($"Error: Response has type {re.GetType()}...¯\\_(ツ)_/¯ ");
                        }
                        _semaphore.Release();
                        return;
                    }

                    if (cmd.HasFlag("headers"))
                    {
                        var headers = response.Headers;
                        for (int i = 0; i < headers.Count; ++i)
                        {
                            string header = headers.GetKey(i);
                            foreach (string value in headers.GetValues(i))
                            {
                                Console.WriteLine("{0}: {1}", header, value);
                            }
                        }
                    }
                    Console.WriteLine($"HTTP Status Code: {(int)response.StatusCode}");

                }
                catch(Exception ex)
                {
                    ConsoleHelper.WriteError(ex);
                }

            }
            else
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
