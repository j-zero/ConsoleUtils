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
        static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        static bool _result = false;
        static bool _debug = false;
        static string filename = "";

        static void Main(string[] args)
        {
            string url = args[0];

            Uri uri = new Uri(url);

           filename = GetFilenameFromWebServer(url);
            if (filename == String.Empty)
                filename = GetFileNameFromUrl(url);

            if (filename == String.Empty)
                filename = "index.html";

            if (filename != string.Empty)
                StartDownload(uri, filename, 2000);
            else
                Console.WriteLine("need filename!");
        }
        public static bool StartDownload(Uri uri, string path, int timeout)
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
                    Console.WriteLine(@"Downloading file:");
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
