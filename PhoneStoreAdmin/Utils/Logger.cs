using System;
using System.IO;

namespace PhoneStoreAdmin.Utils
{
    /// <summary>
    /// Simple logging utility for the PhoneStore Admin application
    /// </summary>
    public static class Logger
    {
        private static readonly string LogFilePath;
        private static readonly object LockObject = new object();

        static Logger()
        {
            // Create log file in user's Documents folder
            LogFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                "PhoneStoreAdmin-debug.log"
            );
        }

        /// <summary>
        /// Log an information message
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void Info(string message)
        {
            WriteLog("INFO", message);
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void Error(string message)
        {
            WriteLog("ERROR", message);
        }

        /// <summary>
        /// Log an error with exception details
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="ex">The exception to log</param>
        public static void Error(string message, Exception ex)
        {
            WriteLog("ERROR", $"{message} - Exception: {ex.Message}\nStackTrace: {ex.StackTrace}");
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void Warning(string message)
        {
            WriteLog("WARNING", message);
        }

        /// <summary>
        /// Log a debug message (only in debug builds)
        /// </summary>
        /// <param name="message">The message to log</param>
        [System.Diagnostics.Conditional("DEBUG")]
        public static void Debug(string message)
        {
            WriteLog("DEBUG", message);
        }

        /// <summary>
        /// Log authentication attempts
        /// </summary>
        /// <param name="username">Username attempting to login</param>
        /// <param name="success">Whether the attempt was successful</param>
        public static void LogAuth(string username, bool success)
        {
            string status = success ? "SUCCESS" : "FAILED";
            WriteLog("AUTH", $"Login attempt for user '{username}' - {status}");
        }

        /// <summary>
        /// Clear the log file
        /// </summary>
        public static void ClearLog()
        {
            lock (LockObject)
            {
                try
                {
                    if (File.Exists(LogFilePath))
                    {
                        File.Delete(LogFilePath);
                    }
                }
                catch (Exception ex)
                {
                    // Can't log this error as it would cause recursion
                    System.Diagnostics.Debug.WriteLine($"Failed to clear log file: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get the full path to the log file
        /// </summary>
        /// <returns>Full path to the log file</returns>
        public static string GetLogFilePath()
        {
            return LogFilePath;
        }

        /// <summary>
        /// Open the log file in the default text editor
        /// </summary>
        public static void OpenLogFile()
        {
            try
            {
                if (File.Exists(LogFilePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = LogFilePath,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                Error("Failed to open log file", ex);
            }
        }

        /// <summary>
        /// Write a log entry with timestamp and level
        /// </summary>
        /// <param name="level">Log level (INFO, ERROR, WARNING, DEBUG, AUTH)</param>
        /// <param name="message">The message to log</param>
        private static void WriteLog(string level, string message)
        {
            lock (LockObject)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string logEntry = $"[{timestamp}] [{level}] {message}{Environment.NewLine}";
                    
                    File.AppendAllText(LogFilePath, logEntry);
                }
                catch (Exception ex)
                {
                    // Fallback to Debug.WriteLine if file logging fails
                    System.Diagnostics.Debug.WriteLine($"Logging failed: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Original message: [{level}] {message}");
                }
            }
        }

        /// <summary>
        /// Log application startup
        /// </summary>
        public static void LogAppStart()
        {
            WriteLog("SYSTEM", "=== PhoneStore Admin Application Started ===");
            WriteLog("SYSTEM", $"Log file location: {LogFilePath}");
            WriteLog("SYSTEM", $"Application version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
            WriteLog("SYSTEM", $"Operating System: {Environment.OSVersion}");
            WriteLog("SYSTEM", $"User: {Environment.UserName}");
            WriteLog("SYSTEM", "===========================================");
        }

        /// <summary>
        /// Log application shutdown
        /// </summary>
        public static void LogAppEnd()
        {
            WriteLog("SYSTEM", "=== PhoneStore Admin Application Ended ===");
        }
    }
}