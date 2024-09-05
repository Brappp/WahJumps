using System;
using System.IO;

namespace WahJumps.Logging
{
    public static class CustomLogger
    {
        private static readonly string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncher", "pluginConfigs", "WahJumps", "plugin.log");

        public static bool IsLoggingEnabled { get; set; } = false; // Default to false

        public static void Log(string message)
        {
            if (!IsLoggingEnabled) return; // Skip logging if disabled

            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logging failed: {ex.Message}");
            }
        }

        public static void ClearLog()
        {
            try
            {
                if (File.Exists(logFilePath))
                {
                    File.Delete(logFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to clear log: {ex.Message}");
            }
        }
    }
}
