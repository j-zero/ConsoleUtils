using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace consoleutils.update
{
    internal class update
    {
        static string URL = "https://github.com/j-zero/ConsoleUtils/releases/latest/download/ConsoleUtils.zip";
        static string VersionURL = "https://github.com/j-zero/ConsoleUtils/releases/latest/download/VERSION";

        static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        static bool _debug = false;
        static bool _result = false;
        static string _filename = "";
        static string _asm_filename = "consoleutils.update.exe";
        static string color1 = "#fbb539";
        static string color2 = "#e85d04";

        static DateTime _startedAt;
        static string tmpFile = "";
        static string tmpDir = "";

        public static void Update()
        {
            
            Console.WriteLine($"[{"*".Pastel(color1)}] downloading \"{URL}\"");
            Console.WriteLine($"[{"*".Pastel(color1)}] to \"{tmpFile}\"...");
            if (StartDownloadFile(new Uri(URL), tmpFile, 5000))
            {
              
                string newAsm = "";
                if (ExtractSingleFileFromZIP(tmpFile, _asm_filename, tmpDir)) // get fresh uploader from zip
                {
                    Console.WriteLine($"[{"*".Pastel(color1)}] extracting updater to \"{tmpDir}\"");
                    newAsm = Path.Combine(tmpDir, _asm_filename);
                    
                }
                else // copy myself to update
                {
                    Console.WriteLine($"[{"*".Pastel(color1)}] copying updater to \"{tmpDir}\"");
                    newAsm = Path.Combine(tmpDir, _asm_filename);
                    File.Copy(Assembly.GetExecutingAssembly().Location, newAsm, true);

                }

                
                Console.Write($"[{"*".Pastel(color1)}] running updater \"{newAsm}\" ... ");

                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = newAsm;
                proc.StartInfo.Arguments = $"extract \"{tmpFile}\" \"{AssemblyDirectory}\""; // ugly as fuck?
                proc.StartInfo.UseShellExecute = true;

                if (proc.Start())
                    Console.WriteLine($"{"success!".Pastel(color1)}");
                else
                    Console.WriteLine($"{"failed!".Pastel(color2)}");

                //File.Delete(tmpFile);
                //Directory.Delete(tmpDir, true);
                ;
            }

        }

        static void ShowVersion()
        {
            string version_string = ("v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + "").PadRight(20);

            Console.WriteLine(@"▄█▄    ████▄    ▄      ▄▄▄▄▄   ████▄ █     ▄███▄     ▄     ▄▄▄▄▀ ▄█ █      ▄▄▄▄▄   ".Pastel("#fbb539"));
            Console.WriteLine(@"█▀ ▀▄  █   █     █    █     ▀▄ █   █ █     █▀   ▀     █ ▀▀▀ █    ██ █     █     ▀▄ ".Pastel("#faa307"));
            Console.WriteLine(@"█   ▀  █   █ ██   █ ▄  ▀▀▀▀▄   █   █ █     ██▄▄    █   █    █    ██ █   ▄  ▀▀▀▀▄   ".Pastel("#f48c06"));
            Console.WriteLine(@"█▄  ▄▀ ▀████ █ █  █  ▀▄▄▄▄▀    ▀████ ███▄  █▄   ▄▀ █   █   █     ▐█ ███▄ ▀▄▄▄▄▀    ".Pastel("#e85d04"));
            Console.WriteLine(@"▀███▀        █  █ █                      ▀ ▀███▀   █▄ ▄█  ▀       ▐     ▀          ".Pastel("#dc2f02"));
            Console.WriteLine((@"             █   ██   Updater " + version_string.Pastel(color1) + @" ▀▀▀ ").Pastel("#d00000") + "https://github.com/j-zero/ConsoleUtils".Pastel(color2));
        }

        static bool CheckForNewVersion(out string LocalVersion, out string RemoteVersion) 
        {
            RemoteVersion = new WebClient().DownloadString(VersionURL).Replace("v", "").Trim();
            string versionFile = Path.Combine(AssemblyDirectory, "VERSION");


            if (File.Exists(versionFile)) {
                LocalVersion = File.ReadAllText(versionFile).Replace("v", "").Trim();
            }
            else
                LocalVersion = "0.0.0.0";

            var version1 = new Version(LocalVersion);
            var version2 = new Version(RemoteVersion);

            var result = version1.CompareTo(version2);
            if (result > 0)
                return false; // Console.WriteLine("version1 is greater");
            else if (result < 0)
                return true;//Console.WriteLine("version2 is greater");
            else
                return false; // Console.WriteLine("versions are equal");

        }

        static void Main(string[] args)
        {
            string version_string = ("ConsoleUtilsUpdater v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()).Pastel(color2) + " (" + "https://github.com/j-zero/ConsoleUtils".Pastel(color1) + ")";
            Console.WriteLine($"[{"*".Pastel(color1)}] {version_string}");

            if (args.Length == 0)
            {
                string localVersion;
                string remoteVersion;
                if(CheckForNewVersion(out localVersion,out remoteVersion))
                {
                    Console.WriteLine($"[{"*".Pastel(color1)}] new release available: {remoteVersion.Pastel(color1)}");
                    Console.WriteLine($"[{"*".Pastel(color1)}] run {"consoleutils.update".Pastel(color1)} {"upgrade".Pastel(color2)} for upgrading");
                }
                else
                {
                    Console.WriteLine($"[{"*".Pastel(color1)}] you run the latest release");
                    //Console.WriteLine($"Your version: {localVersion} Remote version: {remoteVersion}");
                }
            }
            else if (args.Length == 1 && args[0] == "upgrade")
            {

                tmpFile = Path.GetTempFileName();
                tmpDir = Path.Combine(Path.GetTempPath());
                Update();
            }
            else if (args.Length == 3)
            {

                if (args[0] == "extract")
                {
                    //Console.WriteLine(args[1]);
                    //Console.WriteLine(args[2]);

                    try
                    {
                        Console.WriteLine($"[{"*".Pastel(color1)}] extracting \"{args[1]}\"");
                        Console.WriteLine($"[{"*".Pastel(color1)}] to \"{args[2]}\"...");
                        ExtractAllFilesFromZIP(args[1], args[2], true);
                        Console.WriteLine($"\r[{"*".Pastel(color1)}] deleting temporary files ...");
                        File.Delete(args[1]);
                        Console.WriteLine($"[{"*".Pastel(color1)}] done!\n");

                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine($"[{"E".Pastel(color2)}] {e.Message}");

                    }

                }
                ShowVersion();
                Console.Error.WriteLine($"[{"*".Pastel(color1)}] Press any key to exit...");
                Console.ReadKey();
                Environment.Exit(0);
            }

        }

        static bool ExtractSingleFileFromZIP(string zipPath, string fileToExtract, string extractPath)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries.Where(e => e.FullName.ToLower() == fileToExtract))
                {
                    string tmpPath = Path.Combine(extractPath, entry.FullName);
                    entry.ExtractToFile(tmpPath,true);
                    return File.Exists(tmpPath);
                }
            }
            return false;

        }

        static bool ExtractAllFilesFromZIP(string zipPath, string extractPath, bool verbose)
        {
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        Console.WriteLine($"[{"*".Pastel(color1)}] extracting \"{entry.FullName}\" to \"{extractPath}\"");
                        string tmpPath = Path.Combine(extractPath, entry.FullName);

                        string fullPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));

                        if (Path.GetFileName(fullPath).Length != 0)
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                            // The boolean parameter determines whether an existing file that has the same name as the destination file should be overwritten
                            entry.ExtractToFile(fullPath, true);
                        }

                       
                    }
                }
                return true;
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return false;
            }

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
        public static string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
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

            Console.Write($"\r[{"*".Pastel(color1)}] progress: {readableBytes}/{readableBytesTotal} ({e.ProgressPercentage}%, {readableBps}/s)".PadRight(Console.BufferWidth));
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
                Console.WriteLine(Environment.NewLine + $"[{"*".Pastel(color1)}] download finished!");
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
