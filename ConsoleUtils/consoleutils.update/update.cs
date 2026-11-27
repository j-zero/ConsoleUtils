using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Pastel;

namespace consoleutils.update
{
    /// <summary>An error that is shown to the user without a stack trace.</summary>
    internal class UpdateError : Exception
    {
        public UpdateError(string message) : base(message) { }
    }

    /// <summary>A release on GitHub.</summary>
    internal record Release(string Tag, string Url, string Notes, string ZipUrl, long ZipSize, string ChecksumUrl);

    /// <summary>A version like "v0.10.2" or "v0.10.2-7-g7fe9a37" (what "git describe --tags" prints).</summary>
    internal record AppVersion(int[] Parts, int CommitsAhead, string Text) : IComparable<AppVersion>
    {
        private static readonly Regex Pattern = new(@"^v?(?<nums>\d+(?:\.\d+){0,3})(?:-(?<ahead>\d+)-g[0-9a-f]+)?(?<pre>-[0-9A-Za-z.\-]+)?$", RegexOptions.Compiled);

        public static AppVersion Parse(string text)
        {
            var m = Pattern.Match(text.Trim());
            if (!m.Success)
                throw new UpdateError($"Cannot read the version \"{text.Trim()}\".");
            var parts = m.Groups["nums"].Value.Split('.').Select(int.Parse).ToArray();
            int ahead = m.Groups["ahead"].Success ? int.Parse(m.Groups["ahead"].Value) : 0;
            return new AppVersion(parts, ahead, text.Trim());
        }

        public int CompareTo(AppVersion other)
        {
            for (int i = 0; i < Math.Max(Parts.Length, other.Parts.Length); i++)
            {
                int a = i < Parts.Length ? Parts[i] : 0, b = i < other.Parts.Length ? other.Parts[i] : 0;
                if (a != b)
                    return a.CompareTo(b);
            }
            return CommitsAhead.CompareTo(other.CommitsAhead);
        }

        public string Display => "v" + string.Join(".", Parts) + (CommitsAhead > 0 ? $" (+{CommitsAhead} commits)" : "");
    }

    internal class Program
    {
        const string DefaultRepo = "j-zero/ConsoleUtils";
        const string DefaultApi = "https://api.github.com";
        const string ZipName = "ConsoleUtils.zip";
        const string ChecksumName = "ConsoleUtils.zip.sha256";
        const string VersionFile = "VERSION";
        const string OldMarker = ".old-";

        const int ExitOk = 0, ExitError = 1, ExitUpdateAvailable = 10;

        static readonly string color1 = "#fbb539";
        static readonly string color2 = "#e85d04";

        static string repo = DefaultRepo;
        static string api = DefaultApi;
        static bool yes, force, quiet;

        static string AppDirectory => AppContext.BaseDirectory.TrimEnd('\\', '/');

        static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            try
            {
                var rest = ParseOptions(args);
                CleanUpOldFiles();
                return Run(rest).GetAwaiter().GetResult();
            }
            catch (UpdateError e)
            {
                Error(e.Message);
                return ExitError;
            }
            catch (HttpRequestException e)
            {
                Error($"Cannot reach GitHub: {e.Message}");
                return ExitError;
            }
            catch (TaskCanceledException)
            {
                Error("The request to GitHub timed out.");
                return ExitError;
            }
            catch (UnauthorizedAccessException e)
            {
                Error($"No permission to write to \"{AppDirectory}\": {e.Message} Start the updater from an elevated shell.");
                return ExitError;
            }
            catch (IOException e)
            {
                Error(e.Message);
                return ExitError;
            }
        }

        static List<string> ParseOptions(string[] args)
        {
            var rest = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-y": case "--yes": yes = true; break;
                    case "-f": case "--force": force = true; break;
                    case "-q": case "--quiet": quiet = true; break;
                    case "--repo":
                        repo = i + 1 < args.Length ? args[++i] : throw new UpdateError("--repo needs a value like owner/name.");
                        break;
                    case "--api": // for tests: a different server that answers like api.github.com
                        api = i + 1 < args.Length ? args[++i].TrimEnd('/') : throw new UpdateError("--api needs a URL.");
                        break;
                    default: rest.Add(args[i]); break;
                }
            }
            return rest;
        }

        static async Task<int> Run(List<string> args)
        {
            string command = args.Count > 0 ? args[0].ToLowerInvariant() : "";
            switch (command)
            {
                case "":
                    return await Interactive();
                case "check":
                    return await Check();
                case "upgrade":
                case "update":
                    return await Upgrade();
                case "version":
                    Console.WriteLine(LocalVersion()?.Text ?? "unknown");
                    return ExitOk;
                case "help":
                case "-h":
                case "--help":
                    Help();
                    return ExitOk;
                default:
                    throw new UpdateError($"Unknown command \"{args[0]}\". Try: consoleutils.update help");
            }
        }

        // ---- commands ------------------------------------------------------------------------

        static async Task<int> Interactive()
        {
            Banner();
            var local = LocalVersion();
            var release = await GetLatestRelease();
            var remote = AppVersion.Parse(release.Tag);

            if (local != null && local.CompareTo(remote) >= 0)
            {
                Info($"You are running the latest release: {local.Display.Pastel(color1)}");
                return ExitOk;
            }

            ShowRelease(local, release);
            if (Console.IsInputRedirected)
            {
                Info($"Run {"consoleutils.update upgrade".Pastel(color1)} to install it.");
                return ExitUpdateAvailable;
            }
            if (!Confirm("Do you want to upgrade now?"))
            {
                Info($"You can upgrade later with {"consoleutils.update upgrade".Pastel(color1)}.");
                return ExitUpdateAvailable;
            }
            return await Install(release);
        }

        /// <summary>For scripts: exit code 0 = up to date, 10 = update available, 1 = error.</summary>
        static async Task<int> Check()
        {
            var local = LocalVersion();
            var release = await GetLatestRelease();
            var remote = AppVersion.Parse(release.Tag);

            if (local != null && local.CompareTo(remote) >= 0)
            {
                Info($"Up to date: {local.Display}");
                return ExitOk;
            }
            Info($"Update available: {(local?.Display ?? "unknown")} -> {remote.Display} ({release.Url})");
            return ExitUpdateAvailable;
        }

        static async Task<int> Upgrade()
        {
            var local = LocalVersion();
            var release = await GetLatestRelease();
            var remote = AppVersion.Parse(release.Tag);

            if (!force && local != null && local.CompareTo(remote) >= 0)
            {
                Info($"You are running the latest release: {local.Display.Pastel(color1)} (use --force to reinstall)");
                return ExitOk;
            }

            ShowRelease(local, release);
            if (!yes)
            {
                if (Console.IsInputRedirected)
                    throw new UpdateError("Not interactive, use --yes to install without asking.");
                if (!Confirm("Install this release?"))
                    return ExitOk;
            }
            return await Install(release);
        }

        // ---- install -------------------------------------------------------------------------

        static async Task<int> Install(Release release)
        {
            EnsureWritable(AppDirectory);

            string work = Path.Combine(Path.GetTempPath(), "consoleutils-update-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(work);
            try
            {
                string zip = Path.Combine(work, ZipName);
                Info($"downloading {release.ZipUrl.Pastel(color1)}");
                await Download(release.ZipUrl, zip, release.ZipSize);

                await VerifyChecksum(release, zip);
                CheckZip(zip);

                Info($"installing into {AppDirectory.Pastel(color1)}");
                int count = ApplyZip(zip, AppDirectory);

                File.WriteAllText(Path.Combine(AppDirectory, VersionFile), release.Tag + Environment.NewLine);
                Info($"{count} files updated to {release.Tag.Pastel(color1)}.");
                Info("Programs that are running keep the old version until they are started again.");
                return ExitOk;
            }
            finally
            {
                try { Directory.Delete(work, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }

        static void EnsureWritable(string dir)
        {
            string probe = Path.Combine(dir, ".write-test-" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(probe, "");
            File.Delete(probe);
        }

        static async Task VerifyChecksum(Release release, string zip)
        {
            if (release.ChecksumUrl == null)
            {
                Warn($"The release has no {ChecksumName}, the download cannot be verified.");
                return;
            }

            string line = await GetString(release.ChecksumUrl);
            var m = Regex.Match(line, @"\b[0-9a-fA-F]{64}\b");
            if (!m.Success)
                throw new UpdateError($"{ChecksumName} of the release is not readable.");

            string actual;
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(zip))
                actual = Convert.ToHexString(sha.ComputeHash(stream));

            if (!actual.Equals(m.Value, StringComparison.OrdinalIgnoreCase))
                throw new UpdateError($"The checksum of the download does not match (expected {m.Value.ToLowerInvariant()}, got {actual.ToLowerInvariant()}). Nothing was installed.");
            Info("checksum ok");
        }

        static void CheckZip(string zip)
        {
            using var archive = ZipFile.OpenRead(zip);
            string root = AppDirectory + Path.DirectorySeparatorChar;
            foreach (var entry in archive.Entries.Where(e => e.Name.Length > 0))
            {
                // zip slip: an entry must stay inside the program directory
                string target = Path.GetFullPath(Path.Combine(AppDirectory, entry.FullName));
                if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    throw new UpdateError($"The archive contains an unsafe path (\"{entry.FullName}\"). Nothing was installed.");
            }
            if (!archive.Entries.Any(e => e.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)))
                throw new UpdateError("The archive does not contain any program. Nothing was installed.");
        }

        /// <summary>
        /// Replaces the files. A file that is in use (this updater, a running tool) cannot be overwritten, but it can be renamed:
        /// the old file becomes "name.old-N" (deleted at the next start) and the new one takes its place.
        /// If anything fails, all files that were already replaced are restored.
        /// </summary>
        static int ApplyZip(string zip, string targetDir)
        {
            var done = new List<(string Target, string Backup)>();
            int count = 0;
            string fresh = null;
            try
            {
                using var archive = ZipFile.OpenRead(zip);
                // the VERSION file is written last, so an aborted update still reports the old version
                foreach (var entry in archive.Entries.Where(e => e.Name.Length > 0 && !e.Name.Equals(VersionFile, StringComparison.OrdinalIgnoreCase)))
                {
                    string target = Path.GetFullPath(Path.Combine(targetDir, entry.FullName));
                    Directory.CreateDirectory(Path.GetDirectoryName(target));

                    fresh = target + ".new";
                    entry.ExtractToFile(fresh, true);

                    string backup = null;
                    if (File.Exists(target))
                    {
                        backup = target + OldMarker + Environment.ProcessId;
                        if (File.Exists(backup))
                            File.Delete(backup);
                        File.Move(target, backup);
                    }
                    File.Move(fresh, target);
                    fresh = null;
                    done.Add((target, backup));
                    count++;
                }
                return count;
            }
            catch
            {
                Warn("The update failed, restoring the previous files ...");
                if (fresh != null)
                {
                    try { File.Delete(fresh); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                }
                foreach (var (target, backup) in Enumerable.Reverse(done))
                {
                    try
                    {
                        if (File.Exists(target))
                            File.Delete(target);
                        if (backup != null)
                            File.Move(backup, target);
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
                throw;
            }
        }

        /// <summary>Deletes the "*.old-N" files of earlier updates (the ones still in use stay until the next time).</summary>
        static void CleanUpOldFiles()
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(AppDirectory, "*" + OldMarker + "*", SearchOption.AllDirectories))
                {
                    try { File.Delete(file); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                }
                foreach (var file in Directory.EnumerateFiles(AppDirectory, "*.new", SearchOption.AllDirectories))
                {
                    try { File.Delete(file); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // cleaning up is optional
            }
        }

        // ---- GitHub --------------------------------------------------------------------------

        static HttpClient CreateClient(string url)
        {
            var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ConsoleUtils-Updater", "2"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            // a token raises the rate limit of the API, but it must never be sent to other servers
            string token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (!string.IsNullOrEmpty(token) && new Uri(url).Host.Equals("api.github.com", StringComparison.OrdinalIgnoreCase))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        static async Task<string> GetString(string url)
        {
            using var client = CreateClient(url);
            using var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new UpdateError($"GitHub answered {(int)response.StatusCode} {response.ReasonPhrase} for {url}");
            return await response.Content.ReadAsStringAsync();
        }

        static async Task<Release> GetLatestRelease()
        {
            string url = $"{api}/repos/{repo}/releases/latest";
            string json;
            try
            {
                json = await GetString(url);
            }
            catch (UpdateError e) when (e.Message.Contains("404"))
            {
                throw new UpdateError($"The repository {repo} has no release.");
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            string tag = root.GetProperty("tag_name").GetString();
            string notes = root.TryGetProperty("body", out var body) && body.ValueKind == JsonValueKind.String ? body.GetString() : "";
            string page = root.TryGetProperty("html_url", out var html) ? html.GetString() : $"https://github.com/{repo}/releases";

            string zipUrl = null, checksumUrl = null;
            long zipSize = 0;
            foreach (var asset in root.GetProperty("assets").EnumerateArray())
            {
                string name = asset.GetProperty("name").GetString();
                string download = asset.GetProperty("browser_download_url").GetString();
                if (name.Equals(ZipName, StringComparison.OrdinalIgnoreCase))
                {
                    zipUrl = download;
                    zipSize = asset.TryGetProperty("size", out var size) ? size.GetInt64() : 0;
                }
                else if (name.Equals(ChecksumName, StringComparison.OrdinalIgnoreCase))
                {
                    checksumUrl = download;
                }
            }

            if (zipUrl == null)
                throw new UpdateError($"The release {tag} has no {ZipName}.");
            return new Release(tag, page, notes, zipUrl, zipSize, checksumUrl);
        }

        static async Task Download(string url, string path, long expectedSize)
        {
            using var client = CreateClient(url);
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
                throw new UpdateError($"Download failed: {(int)response.StatusCode} {response.ReasonPhrase}");

            long total = response.Content.Headers.ContentLength ?? expectedSize;
            var started = Stopwatch.StartNew();
            long received = 0, lastShown = 0;

            using (var source = await response.Content.ReadAsStreamAsync())
            using (var target = File.Create(path))
            {
                var buffer = new byte[81920];
                int read;
                while ((read = await source.ReadAsync(buffer)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, read));
                    received += read;
                    if (!quiet && !Console.IsOutputRedirected && started.ElapsedMilliseconds - lastShown > 100)
                    {
                        lastShown = started.ElapsedMilliseconds;
                        ShowProgress(received, total, started.Elapsed);
                    }
                }
            }
            if (!quiet && !Console.IsOutputRedirected)
            {
                ShowProgress(received, total, started.Elapsed);
                Console.WriteLine();
            }
            if (total > 0 && received != total)
                throw new UpdateError($"The download is incomplete ({received} of {total} bytes).");
        }

        static void ShowProgress(long received, long total, TimeSpan elapsed)
        {
            double speed = elapsed.TotalSeconds > 0 ? received / elapsed.TotalSeconds : 0;
            string text = total > 0
                ? $"[*] progress: {Size(received)}/{Size(total)} ({received * 100 / total}%, {Size((long)speed)}/s)"
                : $"[*] progress: {Size(received)} ({Size((long)speed)}/s)";
            int width;
            try { width = ConsoleHelper.Width - 1; } catch (IOException) { width = 80; }
            Console.Write("\r" + text.PadRight(Math.Max(width, text.Length)).Replace("[*]", $"[{"*".Pastel(color1)}]"));
        }

        static string Size(long bytes)
        {
            string[] units = { "B", "kB", "MB", "GB" };
            double v = bytes;
            int i = 0;
            while (v >= 1024 && i < units.Length - 1) { v /= 1024; i++; }
            return $"{v:0.#} {units[i]}";
        }

        // ---- version -------------------------------------------------------------------------

        static AppVersion LocalVersion()
        {
            string file = Path.Combine(AppDirectory, VersionFile);
            if (!File.Exists(file))
                return null;
            try
            {
                return AppVersion.Parse(File.ReadAllText(file));
            }
            catch (UpdateError)
            {
                return null; // unreadable, handled like "unknown version"
            }
        }

        // ---- output --------------------------------------------------------------------------

        static void ShowRelease(AppVersion local, Release release)
        {
            Info($"A new {"ConsoleUtils".Pastel(color1)} release is available: {(local?.Display ?? "unknown").Pastel(color2)} -> {release.Tag.Pastel(color1)}");
            Info(release.Url.Pastel(color2));

            var lines = (release.Notes ?? "").Replace("\r", "").Split('\n').Select(l => l.TrimEnd()).Where(l => l.Length > 0).Take(15).ToList();
            foreach (var line in lines)
                Console.WriteLine("    " + line);
            if (lines.Count > 0)
                Console.WriteLine();
        }

        static bool Confirm(string question)
        {
            Console.Write($"[{"*".Pastel(color1)}] {question} [y/N] ");
            var key = Console.ReadKey(true);
            Console.WriteLine(key.KeyChar);
            return key.Key == ConsoleKey.Y;
        }

        static void Info(string text)
        {
            if (!quiet)
                Console.WriteLine($"[{"*".Pastel(color1)}] {text}");
        }

        static void Warn(string text) => Console.Error.WriteLine($"[{"!".Pastel(color1)}] {text}");

        static void Error(string text) => Console.Error.WriteLine($"[{"E".Pastel(color2)}] {text}");

        static void Banner()
        {
            if (quiet)
                return;
            string version = (LocalVersion()?.Text ?? "unknown").PadRight(20);
            Console.WriteLine(@"▄█▄    ████▄    ▄      ▄▄▄▄▄   ████▄ █     ▄███▄     ▄     ▄▄▄▄▀ ▄█ █      ▄▄▄▄▄   ".Pastel("#fbb539"));
            Console.WriteLine(@"█▀ ▀▄  █   █     █    █     ▀▄ █   █ █     █▀   ▀     █ ▀▀▀ █    ██ █     █     ▀▄ ".Pastel("#faa307"));
            Console.WriteLine(@"█   ▀  █   █ ██   █ ▄  ▀▀▀▀▄   █   █ █     ██▄▄    █   █    █    ██ █   ▄  ▀▀▀▀▄   ".Pastel("#f48c06"));
            Console.WriteLine(@"█▄  ▄▀ ▀████ █ █  █  ▀▄▄▄▄▀    ▀████ ███▄  █▄   ▄▀ █   █   █     ▐█ ███▄ ▀▄▄▄▄▀    ".Pastel("#e85d04"));
            Console.WriteLine(@"▀███▀        █  █ █                      ▀ ▀███▀   █▄ ▄█  ▀       ▐     ▀          ".Pastel("#dc2f02"));
            Console.WriteLine((@"             █   ██   Updater " + version.Pastel(color1) + @" ▀▀▀ ").Pastel("#d00000") + $"https://github.com/{repo}".Pastel(color2));
            Console.WriteLine();
        }

        static void Help()
        {
            Console.WriteLine($"Usage: consoleutils.update [{"command".Pastel(color1)}] [{"options".Pastel(color1)}]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine($"  {"(none)".PadRight(10).Pastel(color1)} check for a new release and ask to install it");
            Console.WriteLine($"  {"check".PadRight(10).Pastel(color1)} only check. Exit code 0 = up to date, 10 = update available, 1 = error");
            Console.WriteLine($"  {"upgrade".PadRight(10).Pastel(color1)} download and install the latest release");
            Console.WriteLine($"  {"version".PadRight(10).Pastel(color1)} print the installed version");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine($"  {"-y, --yes".PadRight(12).Pastel(color1)} do not ask before installing");
            Console.WriteLine($"  {"-f, --force".PadRight(12).Pastel(color1)} install even if the version is the same");
            Console.WriteLine($"  {"-q, --quiet".PadRight(12).Pastel(color1)} print only errors");
            Console.WriteLine($"  {"--repo".PadRight(12).Pastel(color1)} other repository (default {DefaultRepo})");
            Console.WriteLine($"  {"--api".PadRight(12).Pastel(color1)} other API server, for tests (default {DefaultApi})");
            Console.WriteLine();
            Console.WriteLine("The release is verified with its SHA-256 checksum. Files that are in use are renamed and replaced; the old copies");
            Console.WriteLine("(name.old-N) are deleted the next time the updater runs. Set GITHUB_TOKEN to raise the API rate limit.");
        }
    }
}
