using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics
{
    public static class DebugHandler
    {
        public enum LogLevel
        {
            INFO,
            WARNING,
            ERROR,
            DEBUG
        }

        private static readonly string debugFilePath = PathManager.GetLogFilePath();
        private static readonly object fileLock = new object();

        static DebugHandler()
        {
            EnsureDebugFileExists();
        }

        public static void Log(string message, LogLevel level = LogLevel.INFO)
        {
            string logEntry = $"{GetEmoji(level)} {DateTime.Now} [{level}] - {message}";
            WriteLog(logEntry, level);
        }
        public static void LogError(string message, Exception ex=null)
        {
            // Convert Element to ElementData if provided, and get its string representation
            string exceptionDetails = "no exception";
            if (ex != null)
            {
                // Build detailed exception information
                exceptionDetails = $"Exception Type: {ex.GetType()}";
                exceptionDetails += $"\nMessage: {ex.Message}";
                exceptionDetails += $"\nSource: {ex.Source}";
                exceptionDetails += $"\nTarget Site: {ex.TargetSite}";
                exceptionDetails += $"\nStack Trace: {ex.StackTrace}";

                // Check for Inner Exception
                if (ex.InnerException != null)
                {
                    exceptionDetails += "\n--- Inner Exception ---";
                    exceptionDetails += $"\nType: {ex.InnerException.GetType()}";
                    exceptionDetails += $"\nMessage: {ex.InnerException.Message}";
                    exceptionDetails += $"\nSource: {ex.InnerException.Source}";
                    exceptionDetails += $"\nTarget Site: {ex.InnerException.TargetSite}";
                    exceptionDetails += $"\nStack Trace: {ex.InnerException.StackTrace}";
                }
            }
            // Compose the final log entry
            string logEntry = $"{GetEmoji(LogLevel.ERROR)} {DateTime.Now} [ERROR] - {message}\n{exceptionDetails}";

            WriteLog(logEntry, LogLevel.ERROR);
        }

        public static void LogWarning(string message)
        {
            // Convert Element to ElementData if provided, and get its string representation
            string logEntry = $"{GetEmoji(LogLevel.WARNING)} {DateTime.Now} [WARNING] - {message}";
            WriteLog(logEntry, LogLevel.WARNING);
        }

        public static void ClearLog()
        {
            try
            {
                lock (fileLock)
                {
                    using (StreamWriter writer = new StreamWriter(debugFilePath, false))
                    {
                        writer.WriteLine($"{DateTime.Now}: Debug log cleared.");
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        public static void OpenLogFile()
        {
            try
            {
                // Sort the log entries before opening
                SortLogEntries();

                System.Diagnostics.Process.Start("notepad.exe", debugFilePath);
            }
            catch (Exception ex)
            {
            }
        }

        private static void SortLogEntries()
        {
            try
            {
                List<string> errors = new List<string>();
                List<string> warnings = new List<string>();
                List<string> others = new List<string>();

                string[] lines = File.ReadAllLines(debugFilePath);

                foreach (string line in lines)
                {
                    if (line.Contains("[ERROR]")) errors.Add(line);
                    else if (line.Contains("[WARNING]")) warnings.Add(line);
                    else others.Add(line);
                }

                lock (fileLock)
                {
                    using (StreamWriter writer = new StreamWriter(debugFilePath, false))
                    {
                        writer.WriteLine($"{DateTime.Now}: Debug log sorted.");
                        writer.WriteLine("=========== ERRORS ===========");
                        errors.ForEach(writer.WriteLine);
                        writer.WriteLine("=========== WARNINGS ==========");
                        warnings.ForEach(writer.WriteLine);
                        writer.WriteLine("=========== OTHER LOGS ========");
                        others.ForEach(writer.WriteLine);
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private static void WriteLog(string logEntry, LogLevel level)
        {
            lock (fileLock)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(debugFilePath, true))
                    {
                        writer.WriteLine(logEntry);
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }

        private static void EnsureDebugFileExists()
        {
            try
            {
                if (!File.Exists(debugFilePath))
                {
                    using (StreamWriter writer = new StreamWriter(debugFilePath))
                    {
                        writer.WriteLine($"{DateTime.Now}: Debug log created.");
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private static string GetEmoji(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.INFO:
                    return "✅";
                case LogLevel.WARNING:
                    return "⚠️";
                case LogLevel.ERROR:
                    return "❌";
                case LogLevel.DEBUG:
                    return "🔍";
                default:
                    return "";
            }
        }
    }
}
