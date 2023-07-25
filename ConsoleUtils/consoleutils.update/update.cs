using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace consoleutils.update
{
    internal class update
    {
        static string URL = "https://github.com/j-zero/ConsoleUtils/releases/latest/download/ConsoleUtils.zip";
        static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        static bool _debug = false;
        static bool _result = false;
        static string filename = "";

        static DateTime _startedAt;
        static string filePath = Path.GetTempFileName();

        public static void Update()
        {
            Console.WriteLine($"Downloading \"{URL}\" to \"{filePath}\"...");
            if (StartDownloadFile(new Uri(URL), filePath, 5000))
            {
                Console.WriteLine($"Extracting to \"{AssemblyDirectory}\"");
                ;
            }
        }

        static void Main(string[] args)
        {
            Update();
        }


        private static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        private static bool StartDownloadFile(Uri uri, string path, int timeout)
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


                    client.Credentials = CredentialCache.DefaultCredentials;
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

            string readableBps = CalculateHumanReadableSize((ulong)bytesPerSecond);
            string readableBytes = CalculateHumanReadableSize((ulong)e.BytesReceived);
            string readableBytesTotal = CalculateHumanReadableSize((ulong)e.TotalBytesToReceive, 1024, 1, true);

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
                            Console.Error.WriteLine(re.Message);
                            Console.Write(re.InnerException.Message);
                        }
                        else
                        {
                            Console.Error.WriteLine($"Error: Response has type {re.GetType()}...¯\\_(ツ)_/¯ ");
                        }
                        _semaphore.Release();
                        return;
                    }

                    Console.WriteLine($"HTTP Status Code: {(int)response.StatusCode}");

                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }

            }
            else
            {
                Console.WriteLine(Environment.NewLine + "Download finished!");
                _result = true;
            }

            _semaphore.Release();
        }

        public static string CalculateHumanReadableSize(UInt64 value, int factor = 1024, int decimalPlaces = 1, bool showByteSuffix = false)
        {
            string[] SizeSuffixes = { "B", "k", "M", "G", "T", "P", "E", "Z", "Y", "R", "Q" };
            string _humanReadbleSize, _humanReadbleSizeSuffix = string.Empty;

            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            //if (value < 0) { return "-" + CalculateHumanReadableSize(-value, decimalPlaces); }
            if (value == 0)
            {
                _humanReadbleSize = "0";
                _humanReadbleSizeSuffix = "";
                return "0";
            }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, factor);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1 << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= factor;
            }

            _humanReadbleSize = string.Format("{0:n" + decimalPlaces + "}", adjustedSize);
            _humanReadbleSizeSuffix = (mag == 0 && !showByteSuffix) ? SizeSuffixes[mag] : "";

            return String.Format(CultureInfo.InvariantCulture, "{0:n" + decimalPlaces + "}{1}", adjustedSize, SizeSuffixes[mag]);

        }
    }
}
