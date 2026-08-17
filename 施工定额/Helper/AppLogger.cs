namespace 施工定额.Helper
{
    /// <summary>
    /// 轻量文件日志，写入 %AppData%\施工定额\logs\app-yyyyMMdd.log
    /// </summary>
    public static class AppLogger
    {
        private static readonly object _lock = new();
        private static string? _logDir;

        public static string LogDirectory
        {
            get
            {
                if (_logDir != null) return _logDir;
                _logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "施工定额", "logs");
                Directory.CreateDirectory(_logDir);
                return _logDir;
            }
        }

        public static void Info(string message) => Write("INFO", message, null);

        public static void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

        public static void Warn(string message) => Write("WARN", message, null);

        private static void Write(string level, string message, Exception? ex)
        {
            try
            {
                var path = Path.Combine(LogDirectory, $"app-{DateTime.Now:yyyyMMdd}.log");
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
                if (ex != null)
                    line += Environment.NewLine + ex;

                lock (_lock)
                {
                    File.AppendAllText(path, line + Environment.NewLine);
                }
            }
            catch
            {
            }
        }
    }
}
