using System.Diagnostics;

namespace windows.core
{
    /// <summary>The console window of a program that was started by double click or by dropping a file on it.</summary>
    public static class ConsoleWindow
    {
        /// <summary>True if the program was started from Explorer (the window closes at the end) or runs under a debugger.</summary>
        public static bool IsStartedFromExplorer()
        {
            if (Debugger.IsAttached)
                return true;
            try
            {
                using var parent = ParentProcessUtilities.GetParentProcess();
                return parent != null && parent.ProcessName.Contains("explorer", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false; // parent is gone or not accessible
            }
        }

        /// <summary>
        /// Waits for a key, but only if the window would disappear otherwise (started from Explorer) and a person can press a key.
        /// </summary>
        public static void PauseIfStartedFromExplorer(string message = "\nPress any key to exit.")
        {
            if (Console.IsInputRedirected || !IsStartedFromExplorer())
                return;
            Console.WriteLine(message);
            Console.ReadKey();
        }
    }
}
