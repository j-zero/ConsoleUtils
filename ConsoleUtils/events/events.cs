using System;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Cryptography;
using Pastel;

namespace events
{
    public class events
    {
        static bool ask_to_close = false;
        static int lineCounter = 0;
        static string color1 = "f25c54";
        static string color2 = "f4845f";
        static bool DEBUG = false;
        static CmdParser cmd;
        static string fullLogPath = null;
        static long currentLogLevel = 5;

        public static void MainCore(string[] args)
        {
            //Console.Error.WriteLine("Hi!");
            Console.CancelKeyPress += Console_CancelKeyPress;

            cmd = new CmdParser(args)
            {
                { "help", "h", CmdCommandTypes.FLAG, "Show this help." },
                { "xml", "x", CmdCommandTypes.FLAG, "Save XML." },
                { "append", "a", CmdCommandTypes.FLAG, "Append to Logfile" },
                { "overwrite", "o", CmdCommandTypes.FLAG, "Do not ask for overwriting files." },
                { "colored-log", "C", CmdCommandTypes.FLAG, "Colored log file (experimental)" },

                { "query", "q", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, $"Custom query (e.g. \"{"*[System[(EventID='15')]]".Pastel(color2)}\"" },
                { "level", "l", CmdCommandTypes.PARAMETER,
                    new CmdParameters() {
                        { CmdParameterTypes.INT, 5},
                    },
                    "LogLevel 0-5 (Default: 5), 0 = LogAlways, 1 = Critical, 2 = Error, 3 = Warning, 4 = Info, 5 = Verbose"
                },
                { "channel", "c", CmdCommandTypes.MULTIPE_PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, $"Channel(s) to open (Default: All; E.g: \"{"Applilcation".Pastel(color2)}\", \"{"System".Pastel(color2)}\", \"{"Security".Pastel(color2)}\")" },
                { "file", "f", CmdCommandTypes.MULTIPE_PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "File to write logs)" },

            };

            cmd.DefaultParameter = "start";
            cmd.Parse();

            if (cmd.HasFlag("help"))
                ShowHelp();

            currentLogLevel = cmd["level"].Int;

            if (args is null) throw new ArgumentNullException(nameof(args));

            if (cmd.HasFlag("file") && cmd["file"].StringIsNotNull)
            {
                var filePath = cmd["file"].String;
                fullLogPath = Path.GetFullPath(filePath);
                if(!File.Exists(fullLogPath))
                    File.Create(fullLogPath);
                else
                {
                    if (!cmd.HasFlag("overwrite") && !cmd.HasFlag("append"))
                    {
                        if (Confirm($"File \"{fullLogPath}\" already exists, would you like to overwrite it?", ConfirmDefault.No))
                            TruncateLogFile();
                        else
                            Environment.Exit(0);
                    }
                    else if (cmd.HasFlag("overwrite"))
                    {
                        TruncateLogFile();
                    }
                    

                    
                }
            }

            string parrentProcess = windows.core.ParentProcessUtilities.GetParentProcess().ProcessName;
            //Console.WriteLine(parrentProcess);

            if (System.Diagnostics.Debugger.IsAttached || parrentProcess.ToLower().Contains("explorer")) // is debugger attached or started by double-click/file-drag
            {
                //Console.WriteLine("\nPress any key to exit.");
                //Console.ReadKey();
            }

            //Console.Error.Write("Getting logs  ... ");
            LoadEventLogs(cmd["channel"].Strings, cmd["query"].String);
            //Console.Error.WriteLine("let's go!");

            while (!ask_to_close)
            {
                //System.Threading.Thread.Sleep(50);
                KeyPressEvent(Console.ReadKey(true));
                ;
            }
        }

        static void KeyPressEvent(ConsoleKeyInfo keyInfo)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.D0:
                    currentLogLevel = 0;
                    WriteErrorLine("(" + "!".Pastel(ColorTheme.purple) + ")" + " Log level set to 0 (" + "LogAlways".Pastel(GetLevelColor(currentLogLevel)) + ")");
                    break;
                case ConsoleKey.D1:
                    currentLogLevel = 1;
                    WriteErrorLine("(" + "!".Pastel(ColorTheme.purple) + ")" + " Log level set to 1 (" + "Critical".Pastel(GetLevelColor(currentLogLevel)) + ")");
                    break;
                case ConsoleKey.D2:
                    currentLogLevel = 2;
                    WriteErrorLine("(" + "!".Pastel(ColorTheme.purple) + ")" + " Log level set to 2 (" + "Error".Pastel(GetLevelColor(currentLogLevel)) + ")");
                    break;
                case ConsoleKey.D3:
                    currentLogLevel = 3;
                    WriteErrorLine("(" + "!".Pastel(ColorTheme.purple) + ")" + " Log level set to 3 (" + "Warning".Pastel(GetLevelColor(currentLogLevel)) + ")");
                    break;
                case ConsoleKey.D4:
                    currentLogLevel = 4;
                    WriteErrorLine("(" + "!".Pastel(ColorTheme.purple) + ")" + " Log level set to 4 (" + "Info".Pastel(GetLevelColor(currentLogLevel)) + ")");
                    break;
                case ConsoleKey.D5:
                    currentLogLevel = 5;
                    WriteErrorLine("(" + "!".Pastel(ColorTheme.purple) + ")" + " Log level set to 5 (" + "Verbose".Pastel(GetLevelColor(currentLogLevel)) + ")");
                    break;
                default:
                    break;
            }
        }

        static bool TruncateLogFile()
        {
            try
            {
                FileStream fileStream = new FileStream(fullLogPath, FileMode.Truncate);
                //fileStream.SetLength(0);
                fileStream.Close();
            }
            catch (Exception ex)
            {
                WriteErrorLine(ex.Message);
                return false;
            }
            return true;
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            //throw new NotImplementedException();
            ask_to_close = true;
            //WriteErrorLine("Goodbye!");
            Environment.Exit(0);
        }

        private static void LoadEventLogs(string[] logSources, string query = null)
        {
            EventLogSession session = new EventLogSession();
            var allLogs = session.GetLogNames().ToArray();
            /*
            foreach (string name in session.GetLogNames())
            {
                Console.WriteLine(name);
            }
            */

            if (logSources == null || cmd["channel"].String == null)
                logSources = allLogs;
            // logSources = new string[] { "Application", "System", "Security"};

            foreach (var logSource in logSources)
            {
                if (!allLogs.Contains(logSource))
                {
                    WriteErrorLine($"Error: Can't find channel \"{logSource}\"");
                    Environment.Exit(1);
                }
                EventLogWatcher logWatcher;

                if (query == null)
                    logWatcher = new EventLogWatcher(new EventLogQuery(logSource, PathType.LogName) { TolerateQueryErrors = true, Session = session });
                else
                    logWatcher = new EventLogWatcher(new EventLogQuery(logSource, PathType.LogName, query) { TolerateQueryErrors = true, Session = session });

                logWatcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(LogWatcher_EventRecordWritten);

                try
                {
                    logWatcher.Enabled = true;
                }
                catch (System.Diagnostics.Eventing.Reader.EventLogException ele)
                {
                    if (DEBUG)
                        WriteErrorLine($"Error: Cannot listen to {logSource}".Pastel(ColorTheme.red));
                }
                catch (Exception ex)
                {
                    WriteErrorLine(ex.Message + Environment.NewLine + ex.InnerException + Environment.NewLine + ex.StackTrace);
                }
            }
        }

        static string GetLevelColor(byte? level)
        {
            if (level.HasValue)
                return GetLevelColor(level.Value);
            else
                return GetLevelColor(-1);
        }

        static string GetLevelColor(long level)
        {
            switch (level)
            {
                case 0: // LogAlways
                    return ColorTheme.fg;
                case 1: // Critical
                    return ColorTheme.dark_red;
                case 2: // Error
                    return ColorTheme.red;
                case 3: // Warning
                    return ColorTheme.yellow;
                case 4: // Informational
                    return ColorTheme.blue;
                case 5: // Verbose
                    return ColorTheme.cyan;
                default:
                    return ColorTheme.coral;
            }
        }

        private static void LogWatcher_EventRecordWritten(object sender, EventRecordWrittenEventArgs e)
        {
            try
            {
                if (e.EventRecord.Level > currentLogLevel)
                    return;



                var time = e.EventRecord.TimeCreated.Value.ToString("yyyy-MM-dd HH\\:mm\\:ss.fff", CultureInfo.InvariantCulture);
                var source = e.EventRecord.ProviderName;

                var user = e.EventRecord.UserId != null ? e.EventRecord.UserId.Translate(typeof(System.Security.Principal.NTAccount)).ToString() : "<null>";

                //string account = new System.Security.Principal.SecurityIdentifier(sid)
                var pid = e.EventRecord.ProcessId.ToString();
                //var processName = e.EventRecord.ProcessId.HasValue ? System.Diagnostics.Process.GetProcessById(e.EventRecord.ProcessId.Value).ProcessName : "<null>";

                var id = e.EventRecord.Id.ToString();
                var logname = e.EventRecord.LogName;
                //var level = e.EventRecord.LevelDisplayName;
                var task = e.EventRecord.TaskDisplayName ?? "<null>";
                var opCode = e.EventRecord.OpcodeDisplayName;
                var mname = e.EventRecord.MachineName;
                var description = e.EventRecord.FormatDescription();
                var xml = e.EventRecord.ToXml();


                var levelColor = GetLevelColor(e.EventRecord.Level);
                var levelText = e.EventRecord.LevelDisplayName;
                switch (e.EventRecord.Level)
                {
                    case 0: // LogAlways
                        levelText = "Log";
                        break;
                    case 1: // Critical
                        levelText = "Critical";
                        break;
                    case 2: // Error
                        levelText = "Error";
                        break;
                    case 3: // Warning
                        levelText = "Warning";
                        break;
                    case 4: // Informational
                        levelText = "Info";
                        break;
                    case 5: // Verbose
                        levelText = "Verbose";
                        break;
                    default:
                        levelText = "Unknown";
                        break;
                }

                WriteLine($"{time.Pastel(ColorTheme.green)}, {levelText.Pastel(levelColor)}, {logname.Pastel(ColorTheme.fg)}:{source.Pastel(ColorTheme.fg)} (Pid: {pid}), Event-ID: {id.Pastel(ColorTheme.fg)}, User: {user.Pastel(ColorTheme.fg)}".Pastel(ColorTheme.light_grey));
                WriteLine($"{description.Pastel(ColorTheme.light_grey)}\n");
                if (cmd.HasFlag("file") && cmd["file"].StringIsNotNull)
                {
                    if (cmd.HasFlag("xml"))
                    {
                        WriteLogLine(xml, fullLogPath);
                    }
                    else
                    {
                        if (!cmd.HasFlag("colored-log"))
                            ConsoleExtensions.Disable();

                        WriteLogLine($"{time.Pastel(ColorTheme.green)}, {levelText.Pastel(levelColor)}, {logname.Pastel(ColorTheme.fg)}:{source.Pastel(ColorTheme.fg)} (Pid: {pid}), Event-ID: {id.Pastel(ColorTheme.fg)}, User: {user.Pastel(ColorTheme.fg)}\n{description}\n---".Pastel(ColorTheme.light_grey), fullLogPath);
                        
                        if (!cmd.HasFlag("colored-log"))
                            ConsoleExtensions.Enable();
                    }
                }
            }
            catch (Exception ex)
            {
                var time = DateTime.Now.ToString("yyyy-MM-dd HH\\:mm\\:ss.fff", CultureInfo.InvariantCulture);
                WriteErrorLine($"{time.Pastel(ColorTheme.green)}, {"Exception".Pastel(ColorTheme.purple)}".Pastel(ColorTheme.light_grey));
                WriteErrorLine($"{ex.Message.Pastel(ColorTheme.light_grey)}");
                WriteErrorLine($"{ex.StackTrace.Pastel(ColorTheme.grey)}");
            }

        }

        static void Write(string text)
        {
            Console.Write(text);
        }

        static void WriteLine(string text)
        {
            Write(text + Environment.NewLine);
        }

        static void WriteError(string text)
        {
            Console.Error.Write(text);
        }

        static void WriteErrorLine(string text) { WriteError(text + Environment.NewLine); }

        static void WriteLogLine(string text, string path)
        {
            try
            {
                System.IO.File.AppendAllText(path, text + Environment.NewLine);
            }
            catch (Exception ex)
            {
                WriteError(ex.Message + Environment.NewLine);
                Environment.Exit(1);
            }
        }
        public enum ConfirmDefault
        {
            None, Yes, No
        }
        public static bool Confirm(string title, ConfirmDefault confirmDefault = ConfirmDefault.None)
        {
            ConsoleKey response;

            switch (confirmDefault)
            {
                case ConfirmDefault.Yes:
                    do
                    {
                        Console.Write($"{title} [Y/n] ");
                        response = Console.ReadKey(false).Key;
                        if (response == ConsoleKey.Enter)
                        {
                            Console.WriteLine();
                            return true;
                        }
                    } while (response != ConsoleKey.Y && response != ConsoleKey.N);
                    break;
                case ConfirmDefault.No:
                    do
                    {
                        Console.Write($"{title} [y/N] ");
                        response = Console.ReadKey(false).Key;
                        if (response == ConsoleKey.Enter)
                        {
                            Console.WriteLine();
                            return false;
                        }
                    } while (response != ConsoleKey.Y && response != ConsoleKey.N);
                    break;
                default:
                    do
                    {
                        Console.Write($"{title} [y/n] ");
                        response = Console.ReadKey(false).Key;
                        Console.WriteLine();
                    } while (response != ConsoleKey.Y && response != ConsoleKey.N);
                    break;

            }
            return (response == ConsoleKey.Y);
        }

        static void ShowHelp()
        {
            ShowVersion();
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName.Pastel(color1)} [{"Options".Pastel(color2)}]");
            Console.WriteLine($"{"Options".Pastel(color2)}:");
            foreach (CmdOption c in cmd.OrderBy(x => x.Name))
            {
                string l = $"  --{c.Name}".Pastel(color1) + (!string.IsNullOrEmpty(c.ShortName) ? $", {("-" + c.ShortName).Pastel(color1)}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel(color2)).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
            //WriteError("Usage: subnet [ip/cidr|ip/mask|ip number_of_hosts]");
            System.Environment.Exit(0);
        }

        static void ShowVersion()
        {
            var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string version_string = ("" + ver.Major + "." + ver.Minor + "." + ver.Revision + "").PadLeft(7);

            Console.WriteLine(@"███████ ██    ██ ███████ ███    ██ ████████ ███████".Pastel("#f25c54"));
            Console.WriteLine(@"██      ██    ██ ██      ████   ██    ██    ██      ".Pastel("#f27059"));
            Console.WriteLine(@"█████   ██    ██ █████   ██ ██  ██    ██    ███████ ".Pastel("#f4845f"));
            Console.WriteLine(@"██       ██  ██  ██      ██  ██ ██    ██         ██ ".Pastel("#f79d65"));
            Console.WriteLine((@"███████   ████   ███████ ██   ████    ██    " + (version_string).PastelBg("#f7b267").Pastel("#f25c54")).Pastel("#f7b267")); //███████ " + version_string).PastelBg("#f7b267"));

        }

        private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = new AssemblyName(args.Name);

            string path = assemblyName.Name + ".dll";
            if (assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture) == false)
            {
                path = String.Format(@"{0}\{1}", assemblyName.CultureInfo, path);
            }

            using (Stream stream = executingAssembly.GetManifestResourceStream(path))
            {
                if (stream == null)
                    return null;

                byte[] assemblyRawBytes = new byte[stream.Length];
                stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
                return Assembly.Load(assemblyRawBytes);
            }
        }
    }
}
