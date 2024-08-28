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
        static string color1 = "f25c54";
        static string color2 = "f4845f";
        static bool DEBUG = false;
        static CmdParser cmd;
        static string full_log_path = null;
        static long current_log_level = 5;
        static bool log_enabled = true;
        static bool running = false;

        static int all_logs_counter = 0;
        static int attached_logs_counter = 0;

        static void ShowVersion()
        {
            var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string version_string = ("" + ver.Major + "." + ver.Minor + "").PadLeft(7);

            Console.WriteLine(@"███████ ██    ██ ███████ ███    ██ ████████ ███████".Pastel("#f25c54"));
            Console.WriteLine(@"██      ██    ██ ██      ████   ██    ██    ██      ".Pastel("#f27059"));
            Console.WriteLine(@"█████   ██    ██ █████   ██ ██  ██    ██    ███████ ".Pastel("#f4845f"));
            Console.WriteLine(@"██       ██  ██  ██      ██  ██ ██    ██         ██ ".Pastel("#f79d65"));
            Console.WriteLine((@"███████   ████   ███████ ██   ████    ██    " + (version_string).PastelBg("#f7b267").Pastel("#f25c54")).Pastel("#f7b267")); //███████ " + version_string).PastelBg("#f7b267"));
            Console.WriteLine("Part of " + GetURL());


        }

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
                        { CmdParameterTypes.STRING, GetTempLogFile() }
                    }, "File to write logs)" },

                { "disable-logging", "L", CmdCommandTypes.FLAG, "Disable logging into file" }
            };

            cmd.DefaultParameter = "start";
            cmd.Parse();

            if (cmd.HasFlag("help"))
                ShowHelp();

            log_enabled = !cmd.HasFlag("disable-logging");

            current_log_level = cmd["level"].Int;

            if (args is null) throw new ArgumentNullException(nameof(args));

            var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string version_string = ("" + ver.Major + "." + ver.Minor + "." + ver.Revision).PadLeft(7);

            WriteLine(("E".Pastel("#f25c54") +"V".Pastel("#f27059") + "E".Pastel("#f4845f") + "N".Pastel("#f79d65") + "T".Pastel("#f7b267") + "S".Pastel("#f7b267") + " " + version_string.Pastel("#f7b267") + " is part of " + GetURL()));
            

            WriteLine(($"Keys: " 
                + $"{(" 0..5".Pastel(color1) + " log level ".Pastel(Color.Black)).PastelBg(ColorTheme.fg)} "
                + $"{(" P".Pastel(color1) + " pause ".Pastel(Color.Black)).PastelBg(ColorTheme.fg)} "
                + $"{(" F".Pastel(color1) + " toggle > file ".Pastel(Color.Black)).PastelBg(ColorTheme.fg)} "
                + $"{(" SPC".Pastel(color1) + " print line ".Pastel(Color.Black)).PastelBg(ColorTheme.fg)} "));


            if (!cmd.HasFlag("disable-logging") && cmd["file"].StringIsNotNull)
            {
                var filePath = cmd["file"].String;
                full_log_path = Path.GetFullPath(filePath);
                if(!File.Exists(full_log_path))
                    File.Create(full_log_path);

                else
                {
                    if (!cmd.HasFlag("overwrite") && !cmd.HasFlag("append"))
                    {
                        if (Confirm($"File \"{full_log_path}\" already exists, would you like to overwrite it?", ConfirmDefault.No))
                            TruncateLogFile();
                        else
                            Environment.Exit(0);
                    }
                    else if (cmd.HasFlag("overwrite"))
                    {
                        TruncateLogFile();
                    }
                    

                    
                }
                WriteErrorLine("[" + "!".Pastel(ColorTheme.purple) + "] " + $"Logging to \"{filePath}\"");
            }
            
            string parrentProcess = windows.core.ParentProcessUtilities.GetParentProcess().ProcessName;
            //Console.WriteLine(parrentProcess);

            if (System.Diagnostics.Debugger.IsAttached || parrentProcess.ToLower().Contains("explorer")) // is debugger attached or started by double-click/file-drag
            {
                //Console.WriteLine("\nPress any key to exit.");
                //Console.ReadKey();
            }

            WriteError("[" + "!".Pastel(ColorTheme.purple) + "] " + $"Attaching to eventlog channels ... ");
            //Console.Error.Write("Getting logs  ... ");
            LoadEventLogs(cmd["channel"].Strings, cmd["query"].String);
            WriteErrorLine($"{attached_logs_counter}/{all_logs_counter} attached.");
            WriteErrorLine("[" + "!".Pastel(ColorTheme.purple) + "] " + $"Logging started: {DateTime.Now.ToString("yyyy-MM-dd HH\\:mm\\:ss.fff", CultureInfo.InvariantCulture).Pastel(ColorTheme.cyan)}\n");

            running = true;

            while (!ask_to_close)
            {
                try
                {
                    //System.Threading.Thread.Sleep(50);
                    KeyPressEvent(Console.ReadKey(true));
                    ;
                }
                catch
                {

                }
            }

        }

        static void KeyPressEvent(ConsoleKeyInfo keyInfo)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.D0:
                    current_log_level = 0;
                    WriteErrorLine("[" + "!".Pastel(ColorTheme.purple) + "] " + "Log level set to 0 (" + "LogAlways".Pastel(GetLevelColor(current_log_level)) + ")");
                    break;
                case ConsoleKey.D1:
                    current_log_level = 1;
                    WriteErrorLine("[" + "!".Pastel(ColorTheme.purple) + "] " + "Log level set to 1 (" + "Critical".Pastel(GetLevelColor(current_log_level)) + ")");
                    break;
                case ConsoleKey.D2:
                    current_log_level = 2;
                    WriteErrorLine("[" + "!".Pastel(ColorTheme.purple) + "] " + "Log level set to 2 (" + "Error".Pastel(GetLevelColor(current_log_level)) + ")");
                    break;
                case ConsoleKey.D3:
                    current_log_level = 3;
                    WriteErrorLine("[" + "!".Pastel(ColorTheme.purple) + "] " + "Log level set to 3 (" + "Warning".Pastel(GetLevelColor(current_log_level)) + ")");
                    break;
                case ConsoleKey.D4:
                    current_log_level = 4;
                    WriteErrorLine("[" + "!".Pastel(ColorTheme.purple) + "] " + "Log level set to 4 (" + "Info".Pastel(GetLevelColor(current_log_level)) + ")");
                    break;
                case ConsoleKey.D5:
                    current_log_level = 5;
                    WriteErrorLine("[" + "!".Pastel(ColorTheme.purple) + "] " + "Log level set to 5 (" + "Verbose".Pastel(GetLevelColor(current_log_level)) + ")");
                    break;
                case ConsoleKey.F:
                    log_enabled = !log_enabled;
                    WriteErrorLine("[" + "!".Pastel(ColorTheme.purple) + "] " + $"Logging to file {(log_enabled ? "enabled" : "disabled")}");
                    break;
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    break;
                case ConsoleKey.C:
                    Console.WriteLine("".PadLeft(Console.BufferWidth, '-'));
                    Console.Clear();
                    break;
                case ConsoleKey.Spacebar:
                    Console.WriteLine("".PadLeft(Console.BufferWidth, '-'));
                    break;
                case ConsoleKey.P:
                case ConsoleKey.Pause:
                    running = !running;
                    WriteErrorLine("[" + "!".Pastel(ColorTheme.purple) + "] " + $"{(running ? "Started." : "Stopped.")}");
                    break;
                default:
                    break;
            }
        }

        static string GetTempLogFile() {
            return Path.Combine(Path.GetTempPath(), "events." + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".log");
        }

        static bool TruncateLogFile()
        {
            try
            {
                FileStream fileStream = new FileStream(full_log_path, FileMode.Truncate);
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
            WriteError("[" + "!".Pastel(ColorTheme.purple) + "] " + $"User interrupt. Closing ...");

            Environment.Exit(0);
        }

        private static void LoadEventLogs(string[] logSources, string query = null)
        {
            EventLogSession session = new EventLogSession();
            var allLogs = session.GetLogNames().ToArray();
            all_logs_counter = allLogs.Length;

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
                    attached_logs_counter++;
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
            if (!running)
                return;

            try
            {
                if (e.EventRecord.Level > current_log_level)
                    return;

               
                var hostname = System.Net.Dns.GetHostName();
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
                
                WriteLine($"{time.Pastel(ColorTheme.cyan)}, {hostname}, {levelText.Pastel(levelColor)}, {logname.Pastel(ColorTheme.fg)}:{source.Pastel(ColorTheme.fg)} (Pid: {pid}), Event-ID: {id.Pastel(ColorTheme.fg)}, User: {user.Pastel(ColorTheme.fg)}:".Pastel(ColorTheme.lighter_grey));
                WriteLine($"{description.Pastel(ColorTheme.light_grey)}\n");
                if (cmd.HasFlag("file") && cmd["file"].StringIsNotNull)
                {
                    if (cmd.HasFlag("xml"))
                    {
                        WriteLogLine(xml, full_log_path);
                    }
                    else
                    {
                        if (!cmd.HasFlag("colored-log"))
                            ConsoleExtensions.Disable();

                        WriteLogLine($"{time.Pastel(ColorTheme.green)}, {hostname}, {levelText.Pastel(levelColor)}, {logname.Pastel(ColorTheme.fg)}:{source.Pastel(ColorTheme.fg)} (Pid: {pid}), Event-ID: {id.Pastel(ColorTheme.fg)}, User: {user.Pastel(ColorTheme.fg)}:\n{description}\n---".Pastel(ColorTheme.lighter_grey), full_log_path);
                        
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
            Console.Write(text.Pastel(ColorTheme.lighter_grey));
        }

        static void WriteLine(string text)
        {
            Write(text + Environment.NewLine);
        }

        static void WriteError(string text)
        {
            Console.Error.Write(text.Pastel(ColorTheme.lighter_grey));
        }

        static void WriteErrorLine(string text) { WriteError(text + Environment.NewLine); }

        static void WriteLogLine(string text, string path)
        {
            if (log_enabled)
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
            Console.WriteLine($"\nUsage: {AppDomain.CurrentDomain.FriendlyName.Pastel(color1)} [{"Options".Pastel(color2)}]");
            Console.WriteLine($"{"Options".Pastel(color2)}:");
            foreach (CmdOption c in cmd.OrderBy(x => x.Name))
            {
                if (c.CmdType == CmdCommandTypes.HIDDEN_FLAG) 
                    continue;

                string l = $"  --{c.Name}".Pastel(color1) + (!string.IsNullOrEmpty(c.ShortName) ? $", {("-" + c.ShortName).Pastel(color1)}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel(color2)).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
            //WriteError("Usage: subnet [ip/cidr|ip/mask|ip number_of_hosts]");
            System.Environment.Exit(0);
        }

        static string GetURL()
        {
            return ("ConsoleUtils".Pastel(color1) + " (" + "https://github.com/j-zero/ConsoleUtils".Pastel(color2) + ")");
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
